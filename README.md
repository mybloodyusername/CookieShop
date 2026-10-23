# CookieShop API

.NET 10, ASP.NET Core Identity, PostgreSQL. This repository contains the backend API; owner media management is exposed through admin APIs, without a dashboard UI.

## Currency

All product prices, cart totals and order amounts are **whole Iranian toman**. Responses identify the application currency as `IRT` (an application identifier, not an ISO currency code). Prices use `decimal`; input validation and database checks reject fractions, negative amounts and invalid sale prices. `StoreCurrency.ToRials` converts an integer toman amount to rials using checked arithmetic. Payment integrations must explicitly confirm their expected unit and use this conversion only when they require rials.

## Configuration and startup

Supply secrets through environment variables, your hosting secret manager, or .NET user secrets. No administrator or JWT signing secret ships in configuration.

```sh
export ConnectionStrings__DefaultConnection='Host=localhost;Database=cookie_shop;Username=YOUR_USER;Password=YOUR_PASSWORD'
export JwtSettings__SecretKey="$(openssl rand -base64 48)"
export CorsSettings__ProductionOrigin='https://YOUR_STOREFRONT_DOMAIN'
export AllowedHosts='YOUR_API_DOMAIN'
```

The signing secret must remain stable across restarts and instances. Default session lifetime is 60 minutes (`JwtSettings__ExpirationMinutes`, allowed range 5–120). Changing the key invalidates all sessions.

Apply migrations as a deployment step, using the same connection environment variable:

```sh
dotnet ef database update --project CookieShop.Infra --startup-project CookieShop.Api
dotnet run --project CookieShop.Api
```

Automatic migration on startup is disabled. For local development only, you can set `Database__ApplyMigrationsOnStartup=true`.

To provision the first owner, supply all four values below for one startup, then disable bootstrap and remove the password from deployment configuration:

```sh
export BootstrapAdmin__Enabled=true
export BootstrapAdmin__PhoneNumber='YOUR_11_DIGIT_IRANIAN_MOBILE'
export BootstrapAdmin__Email='YOUR_EMAIL'
export BootstrapAdmin__Password='YOUR_UNIQUE_STRONG_PASSWORD'
```

Passwords need at least 12 characters, an uppercase letter, lowercase letter and digit. Bootstrap never elevates an existing customer, and does nothing once an administrator exists. Existing installations must change the old seeded administrator password before exposure to the internet. Removing the old seed does not remove existing accounts.

### Existing data

Back up the database before migration. The migration copies current delivery addresses into existing orders and adds constraints. Review existing prices before applying it: old sample prices such as `5.99` have no safe automatic conversion to toman. Correct prices deliberately, including historical order amounts if they used a different unit. No migration silently rounds or guesses an exchange rate. Invalid stock, cart quantities and sale prices must also be corrected before the constraints can apply. The email migration enforces unique normalized emails; resolve any existing duplicates (including differences only in letter case) first.

```sql
SELECT "NormalizedEmail", count(*) FROM "AspNetUsers"
WHERE "NormalizedEmail" IS NOT NULL GROUP BY "NormalizedEmail" HAVING count(*) > 1;
SELECT "Id", "Price", "SalePrice" FROM "Products"
WHERE "Price" <> trunc("Price") OR "Price" < 1 OR "Price" > 999999999999
   OR ("SalePrice" IS NOT NULL AND
       ("SalePrice" <> trunc("SalePrice") OR "SalePrice" < 1 OR "SalePrice" >= "Price"))
   OR ("IsOnSale" AND "SalePrice" IS NULL) OR "StockQuantity" < 0;
SELECT "Id", "TotalAmount" FROM "Orders"
WHERE "TotalAmount" < 0 OR "TotalAmount" <> trunc("TotalAmount");
SELECT "Id", "UnitPrice", "Quantity" FROM "OrderItems"
WHERE "UnitPrice" < 1 OR "UnitPrice" <> trunc("UnitPrice") OR "Quantity" NOT BETWEEN 1 AND 1000;
SELECT "Id", "Quantity" FROM "CartItems" WHERE "Quantity" NOT BETWEEN 1 AND 1000;
```

Older JWTs without the new security-stamp claim are invalid. Previously exposed secrets must be rotated. Remove old demo products/images if they are not real inventory.

## Persian sample setup

A dedicated Development-only maintenance command replaces all store data and non-admin accounts with a complete Persian sample store. It preserves every existing admin's ID, English/ASCII username, password and identity credentials. Empty admin display names become «مدیر فروشگاه». At least one existing administrator is required.

```sh
dotnet run --project CookieShop.Api --launch-profile http -- --reset-demo-data
```

This command is destructive and explicit: normal application startup never resets the samples. It removes all old addresses, carts, orders, products, categories, PostgreSQL media and non-admin users. It clears old data before applying pending migrations so legacy fractional prices do not block the new whole-toman constraints. Cleanup and schema migration occur before the transaction that inserts the new sample store; keep a database backup before running it.

The new setup contains:

- 6 Persian categories and 24 products with Persian descriptions, ingredient/allergen notes, stock levels and whole-toman prices.
- Sale items, an out-of-stock item and an unavailable item for realistic catalog states.
- 48 generated WebP sample illustrations, two per product, stored in PostgreSQL with Persian alt text and category-based albums. These are illustrative placeholders, not photographs of actual inventory.
- 3 clearly marked sample delivery addresses and 12 orders across all five order states, with item/delivery snapshots and consistent totals and stock deductions.
- One populated sample cart. All sample orders, addresses and the cart belong to the oldest preserved admin; no customer accounts are recreated.

The command does not listen for HTTP requests or change your configured upload provider. Sample media always uses the temporary PostgreSQL adapter. To prevent leaving orphaned objects in external buckets, it refuses a reset when the current library contains media stored outside PostgreSQL; move or remove those assets first.

## Browser authentication

The API places the JWT in the `CookieShop.Token` HttpOnly cookie. Production cookies require HTTPS and use `SameSite=Strict`. Deploy the storefront and API on the same site (for example `shop.example.com` and `api.example.com`), or reverse proxy `/api` through the storefront. A completely unrelated frontend domain will not work with these cookies.

For browser requests, use `credentials: 'include'` and the configured exact CORS origin:

1. `GET /api/Auth/Csrf` returns `{ "token": "..." }` and sets an HttpOnly antiforgery cookie.
2. Send `X-CSRF-TOKEN: <token>` on every POST, PUT, PATCH and DELETE, including login, registration and multipart uploads. Non-browser callers also fetch the token and preserve cookies.
3. Register with `POST /api/Auth/Register` and log in with `POST /api/Auth/Login`. Mobile numbers use ASCII `09xxxxxxxxx` format.
4. Fetch a fresh CSRF token after login because it is bound to the authenticated identity.
5. `POST /api/Auth/Logout` revokes **all sessions for the account**. `POST /api/User/ChangePassword` accepts `currentPassword` and `newPassword`, then revokes sessions and clears the cookie. Log in again afterward.

Five failed password checks lock an account for 15 minutes. Login and registration share a limit of 10 requests per minute per client IP per API instance. Unknown accounts and invalid passwords return the same login error. Sessions validate the current user, lockout and security stamp on each request; current database roles replace stale token roles. Profile changes invalidate existing sessions too. `/api/User/Me` returns the current session user.

Public access is explicit for catalog reads, image reads and authentication entry points. Other routes require authentication; catalog changes, image library actions, user administration and order fulfillment require the admin role. Customer address/cart/order actions are scoped to the current user. Conflicts return 409 and validation errors return 400.

## Image library and product galleries

With the default `Media__Provider=Postgres`, compressed bytes are stored in PostgreSQL `bytea`, separately from queryable metadata. `IImageStorage` separates byte storage from the library and product relationships. Each image keeps its storage provider so changing the upload provider does not break older PostgreSQL images.

Every uploaded asset has a stable public endpoint `/api/images/{id}.webp`. By default URLs are relative to the API origin. Set `Media__PublicBaseUrl=https://YOUR_API_DOMAIN` for absolute URLs in all media, product and cart responses; the base comes from trusted configuration rather than the request Host header. Alternatively resolve relative URLs against your API origin in the frontend. Files use `image/webp`, `nosniff`, ETags and immutable caching. Contents cannot be replaced at an existing URL: upload a new asset and change the attachment. Deleting an asset does not purge copies already cached by browsers or a CDN. The library is for public product photos, not private documents.

Uploads accept actual JPEG, PNG and WebP data regardless of filenames or declared content types. The processor validates dimensions before decoding, caps inputs at 10 MiB, 40 megapixels and 16384 pixels per side, normalizes orientation, removes metadata, resizes to fit 2048×2048 without enlargement, and writes WebP at quality 80. Animated inputs become a still image. Two conversions run concurrently per API process. The Linux native dependency is included for deployment; verify the target Linux image during deployment.

All library mutation/list endpoints below require an administrator and unsafe requests require CSRF:

| Method | Route | Input / purpose |
| --- | --- | --- |
| POST | `/api/images` | Multipart `File`, optional `Alt` (max 500), optional `Album` (max 100) |
| GET | `/api/images?album=Cookies&page=1&pageSize=30` | Paged metadata library; no blob bytes |
| PUT | `/api/images/{id}` | JSON `{ "alt": "...", "album": "Cookies" }` |
| DELETE | `/api/images/{id}` | Delete an unattached asset; attached images return 409 |
| PUT | `/api/images/products/{productId}/{imageId}` | Attach or update `{ "alt": null, "sortOrder": 0 }` |
| DELETE | `/api/images/products/{productId}/{imageId}` | Detach without deleting the asset |
| GET | `/api/images/{id}.webp` | Public WebP bytes |

Upload assets individually, then attach their IDs to products. There is no fixed limit on gallery size, and an asset can be reused on multiple products. Library albums are optional labels, editable through the metadata endpoint and filterable in the library list. Gallery entries sort by `sortOrder`, then image ID; the first entry becomes `imageUrl`. Product detail and catalog responses include an `images` array containing URLs, alt text and order. Existing `ImageUrl` fields remain a fallback for compatibility with earlier clients.

An attachment's `alt: null` inherits the library alt; `alt: ""` explicitly means decorative. Nonempty values override the library alt for that product. These fields contain plain text, which the frontend must bind as text/attributes rather than HTML.

### Liara / Arvan S3 adapter

Create a private bucket and server-side access credentials with only the bucket permissions this app needs. Set these values from your provider's panel:

```sh
export Media__Provider=S3
export Media__S3__Endpoint='https://YOUR_S3_ENDPOINT'
export Media__S3__Region='YOUR_SIGNING_REGION'
export Media__S3__Bucket='YOUR_BUCKET'
export Media__S3__AccessKey='YOUR_SERVER_ACCESS_KEY'
export Media__S3__SecretKey='YOUR_SERVER_SECRET_KEY'
```

The adapter uses path-style S3 operations with explicit endpoint/region, HTTPS and compatibility settings for optional checksums. Keys follow `images/{id}.webp`. The API proxies reads through the same stable public endpoint, so the bucket need not be public and credentials never reach the browser. Configure a CDN in front of image GETs as traffic grows.

Switching providers affects **new uploads only**. Existing images remain in their original storage and continue to resolve. Keep S3 configuration available if you later switch uploads back to PostgreSQL. Bulk movement of existing bytes needs a separate copy/verify/metadata-switch migration; never change the stored provider field without first copying and verifying the object. No live Liara or Arvan connection is tested without provider credentials.

## Checkout and operational boundaries

Checkout recalculates prices from the database, reserves stock and clears the cart in a serializable transaction. PostgreSQL `xmin` concurrency checks protect products, cart rows and order state. Competing operations return a retryable conflict. Cancellation of a pending order restores stock in the same transaction, and admin cancellation uses that same path. Allowed fulfillment transitions are pending → paid → shipped → delivered, or pending → cancelled. Delivery details and item names/prices are snapshotted when placing an order.

This is still a store backend, not a completed payment integration. Pending orders reserve stock until cancelled; add reservation expiry/background cleanup when implementing the payment lifecycle. A real payment gateway must verify callbacks server-side, match amount and currency against the saved order, enforce callback idempotency, and confirm payment before fulfillment. Admin `Paid` status is a manual workflow until that integration exists. Phone/email verification, account recovery and owner MFA also require implementation before a public launch.

Terminate HTTPS at a trusted ingress. Configure forwarded headers only for your actual trusted proxy/network; do not blindly trust client-supplied forwarded headers. The current IP limiter otherwise sees the proxy IP. Add a shared ingress rate limit when running multiple replicas. Set `DataProtection__KeyPath` to a persistent private mounted directory shared by replicas; protect/encrypt that key store according to your host. Back up PostgreSQL (including image blobs) and object storage, and test restoration. Protect API secrets and disable bootstrap after provisioning.

## Build

```sh
dotnet restore CookieShop.sln
dotnet build CookieShop.sln -c Release
```

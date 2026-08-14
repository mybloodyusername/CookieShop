namespace CookieShop.App.DTOs.Common;

public record PagedResult<T>(IReadOnlyCollection<T> Items, int TotalCount, int Page, int PageSize);

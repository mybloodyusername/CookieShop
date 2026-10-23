using CookieShop.Domain.Constants;
using CookieShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace CookieShop.Infra.Data;

public static class SeedData
{
    public static async Task Initialize(ILogger<CookieShopDbContext> logger, IServiceProvider services,
        UserManager<ApplicationUser> users, RoleManager<IdentityRole<Guid>> roles)
    {
        foreach (var role in UserRole.All)
            if (!await roles.RoleExistsAsync(role)) Ensure(await roles.CreateAsync(new IdentityRole<Guid>(role)));
        var config = services.GetRequiredService<IConfiguration>();
        // Bootstrap is explicit and one-time. Never elevate an existing customer account.
        if (!config.GetValue<bool>("BootstrapAdmin:Enabled")) return;
        if ((await users.GetUsersInRoleAsync(UserRole.Admin)).Count > 0) return;
        var phone = config["BootstrapAdmin:PhoneNumber"] ?? throw new InvalidOperationException("Bootstrap admin phone required.");
        var email = config["BootstrapAdmin:Email"] ?? throw new InvalidOperationException("Bootstrap admin email required.");
        var password = config["BootstrapAdmin:Password"] ?? throw new InvalidOperationException("Bootstrap admin password required.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(phone, "^09[0-9]{9}$"))
            throw new InvalidOperationException("Bootstrap phone must be an Iranian mobile number.");
        if (await users.FindByNameAsync(phone) != null || await users.FindByEmailAsync(email) != null)
            throw new InvalidOperationException("Bootstrap identity already exists; provision admin manually.");
        var user = new ApplicationUser { UserName = phone, PhoneNumber = phone, Email = email };
        Ensure(await users.CreateAsync(user, password));
        var assigned = await users.AddToRoleAsync(user, UserRole.Admin);
        if (!assigned.Succeeded) { Ensure(await users.DeleteAsync(user)); Ensure(assigned); }
        logger.LogInformation("Owner account created. Disable BootstrapAdmin after provisioning.");
    }
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}

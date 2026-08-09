using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CookieShop.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    [MaxLength(64)] public string FirstName { get; set; } = string.Empty;
    [MaxLength(64)] public string LastName { get; set; } = string.Empty;
    
    [NotMapped] public string FullName => $"{FirstName} {LastName}".Trim();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
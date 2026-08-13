using System.ComponentModel.DataAnnotations;

namespace CookieShop.Domain.Entities;

public class Address
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)] public required string Title { get; set; }

    [Required, MaxLength(256)] public required string AddressLine1 { get; set; }

    [MaxLength(256)] public string AddressLine2 { get; set; } = string.Empty;

    [Required, MaxLength(16)] public required string PhoneNumber { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    
    public Guid ProvinceId { get; set; }
    public Province Province { get; set; } = null!;

    public Guid CityId { get; set; }
    public City City { get; set; } = null!;
    
}
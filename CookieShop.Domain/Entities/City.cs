using System.ComponentModel.DataAnnotations;

namespace CookieShop.Domain.Entities;

public class City
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)] public required string Name { get; set; }

    public Guid ProvinceId { get; set; }
    public Province Province { get; set; } = null!;
}
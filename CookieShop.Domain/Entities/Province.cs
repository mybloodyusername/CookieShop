using System.ComponentModel.DataAnnotations;

namespace CookieShop.Domain.Entities;

public class Province
{
    public Guid Id { get; set; }
    
    [Required, MaxLength(64)] public required string Name { get; set; }
    
    public ICollection<City> Cities { get; set; } = new List<City>();
}
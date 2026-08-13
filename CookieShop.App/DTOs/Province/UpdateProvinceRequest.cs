using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Province;

public record UpdateProvinceRequest(
    [Required] Guid Id,
    [Required] string Name
);
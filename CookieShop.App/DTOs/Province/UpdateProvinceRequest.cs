using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Province;

public record UpdateProvinceRequest(
    [Required] string Id,
    [Required] string Name
);
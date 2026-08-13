using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Province;

public record CreateProvinceRequest(
    [Required] string Name
);
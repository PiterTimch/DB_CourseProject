using System.ComponentModel.DataAnnotations;

namespace VehicleServiceStation.Models.Forms;

public sealed class ServiceForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Введіть назву послуги.")]
    [StringLength(160, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "Ціна має бути невід’ємною.")]
    public decimal Price { get; set; }
}

public sealed class PartForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Введіть назву запчастини.")]
    [StringLength(160, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть артикул.")]
    [StringLength(80, MinimumLength = 2)]
    public string Article { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "Ціна має бути невід’ємною.")]
    public decimal Price { get; set; }
}

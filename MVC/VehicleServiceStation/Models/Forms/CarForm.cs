using System.ComponentModel.DataAnnotations;

namespace VehicleServiceStation.Models.Forms;

public sealed class CarForm
{
    public int CarId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть клієнта.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Введіть VIN-код.")]
    [RegularExpression("^[A-HJ-NPR-Z0-9]{17}$", ErrorMessage = "VIN має складатися з 17 літер і цифр без I, O та Q.")]
    public string Vin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть державний номер.")]
    [StringLength(20, MinimumLength = 4)]
    public string LicensePlate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть марку та модель.")]
    [StringLength(120, MinimumLength = 2)]
    public string Model { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace VehicleServiceStation.Models.Forms;

public sealed class CustomerForm
{
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Оберіть тип клієнта.")]
    [RegularExpression("^(individual|business)$", ErrorMessage = "Невідомий тип клієнта.")]
    public string CustomerType { get; set; } = "individual";

    [Required(ErrorMessage = "Введіть ім’я або назву компанії.")]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть прізвище.")]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть номер телефону.")]
    [Phone(ErrorMessage = "Перевірте формат номера телефону.")]
    [StringLength(30, MinimumLength = 7)]
    public string Phone { get; set; } = string.Empty;
}

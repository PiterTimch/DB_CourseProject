using System.ComponentModel.DataAnnotations;

namespace VehicleServiceStation.Models.Forms;

public sealed class EmployeeCreateForm
{
    [Required(ErrorMessage = "Введіть ім’я.")]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть прізвище.")]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(admin|manager|director|guest)$", ErrorMessage = "Оберіть допустиму роль.")]
    public string Role { get; set; } = "manager";

    [Required(ErrorMessage = "Введіть логін.")]
    [StringLength(100, MinimumLength = 3)]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть пароль.")]
    [StringLength(200, MinimumLength = 8, ErrorMessage = "Пароль має містити щонайменше 8 символів.")]
    public string Password { get; set; } = string.Empty;
}

public sealed class EmployeeEditForm
{
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Введіть ім’я.")]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть прізвище.")]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(admin|manager|director|guest)$", ErrorMessage = "Оберіть допустиму роль.")]
    public string Role { get; set; } = "manager";

    [Required(ErrorMessage = "Введіть логін.")]
    [StringLength(100, MinimumLength = 3)]
    public string Login { get; set; } = string.Empty;

    [StringLength(200, MinimumLength = 8, ErrorMessage = "Новий пароль має містити щонайменше 8 символів.")]
    public string? Password { get; set; }
}

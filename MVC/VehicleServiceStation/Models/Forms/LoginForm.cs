using System.ComponentModel.DataAnnotations;

namespace VehicleServiceStation.Models.Forms;

public sealed class LoginForm
{
    [Required(ErrorMessage = "Введіть логін.")]
    [StringLength(100)]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть пароль.")]
    [StringLength(200)]
    public string Password { get; set; } = string.Empty;
}

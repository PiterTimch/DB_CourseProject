namespace VehicleServiceStation.Models;

public sealed class Employee
{
    public int EmployeeId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

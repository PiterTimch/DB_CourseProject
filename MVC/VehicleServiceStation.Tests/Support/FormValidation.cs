using System.ComponentModel.DataAnnotations;

namespace VehicleServiceStation.Tests.Support;

internal static class FormValidation
{
    public static IReadOnlyList<ValidationResult> Validate<T>(T form)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(form!, new ValidationContext(form!), results, validateAllProperties: true);
        return results;
    }
}

using VehicleServiceStation.Models.Forms;
using VehicleServiceStation.Tests.Support;

namespace VehicleServiceStation.Tests.Validation;

public sealed class CatalogFormsTests
{
    [Fact] public void Negative_service_price_is_rejected()
    {
        Assert.Contains(FormValidation.Validate(new ServiceForm { Name = "Діагностика", Price = -0.01m }), e => e.MemberNames.Contains(nameof(ServiceForm.Price)));
    }
    [Fact] public void Zero_service_price_is_accepted()
    {
        Assert.Empty(FormValidation.Validate(new ServiceForm { Name = "Діагностика", Price = 0m }));
    }
    [Theory]
    [MemberData(nameof(ServiceNameLengthCases))]
    public void Service_name_length_boundaries_are_enforced(string name, bool valid)
    {
        var errors = FormValidation.Validate(new ServiceForm { Name = name, Price = 1m });
        Assert.Equal(valid, errors.All(e => !e.MemberNames.Contains(nameof(ServiceForm.Name))));
    }

    public static IEnumerable<object[]> ServiceNameLengthCases =>
    [
        ["A", false],
        ["AB", true],
        [new string('X', 160), true],
        [new string('X', 161), false]
    ];
}

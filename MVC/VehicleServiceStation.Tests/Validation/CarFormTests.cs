using VehicleServiceStation.Models.Forms;
using VehicleServiceStation.Tests.Support;

namespace VehicleServiceStation.Tests.Validation;

public sealed class CarFormTests
{
    [Fact] public void Valid_car_is_accepted() => Assert.Empty(FormValidation.Validate(Valid()));
    [Theory]
    [InlineData("1HGCM82633A004352", true)]
    [InlineData("1HGCM82633I004352", false)]
    [InlineData("1HGCM82633O004352", false)]
    [InlineData("1HGCM82633Q004352", false)]
    [InlineData("SHORT", false)]
    public void Vin_requires_seventeen_valid_characters(string vin, bool valid)
    {
        var form = Valid(); form.Vin = vin;
        Assert.Equal(valid, FormValidation.Validate(form).All(e => !e.MemberNames.Contains(nameof(form.Vin))));
    }
    [Theory]
    [MemberData(nameof(ModelLengthCases))]
    public void Model_length_boundaries_are_enforced(string model, bool valid)
    {
        var form = Valid(); form.Model = model;
        Assert.Equal(valid, FormValidation.Validate(form).All(e => !e.MemberNames.Contains(nameof(form.Model))));
    }
    public static IEnumerable<object[]> ModelLengthCases =>
    [
        ["AB", true],
        ["A", false],
        [new string('A', 120), true],
        [new string('A', 121), false]
    ];

    private static CarForm Valid() => new() { CustomerId = 1, Vin = "1HGCM82633A004352", LicensePlate = "AA1234BB", Model = "Honda" };
}

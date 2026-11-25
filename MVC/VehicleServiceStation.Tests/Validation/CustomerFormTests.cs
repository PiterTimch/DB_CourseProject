using VehicleServiceStation.Models.Forms;
using VehicleServiceStation.Tests.Support;

namespace VehicleServiceStation.Tests.Validation;

public sealed class CustomerFormTests
{
    [Fact] public void Valid_customer_is_accepted() => Assert.Empty(FormValidation.Validate(Valid()));
    [Theory]
    [InlineData("A", "FirstName")]
    [InlineData("B", "LastName")]
    public void Name_shorter_than_two_characters_is_rejected(string name, string field)
    {
        var form = Valid();
        if (field == "FirstName") form.FirstName = name; else form.LastName = name;
        Assert.Contains(FormValidation.Validate(form), error => error.MemberNames.Contains(field));
    }
    [Theory]
    [InlineData("abc", false)]
    [InlineData("+380501234567", true)]
    public void Phone_format_is_checked(string phone, bool valid)
    {
        var form = Valid(); form.Phone = phone;
        Assert.Equal(valid, FormValidation.Validate(form).All(e => !e.MemberNames.Contains(nameof(form.Phone))));
    }
    private static CustomerForm Valid() => new() { FirstName = "Іван", LastName = "Петренко", Phone = "+380501234567" };
}

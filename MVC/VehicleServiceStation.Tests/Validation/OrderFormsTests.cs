using VehicleServiceStation.Models.Forms;
using VehicleServiceStation.Tests.Support;

namespace VehicleServiceStation.Tests.Validation;

public sealed class OrderFormsTests
{
    [Theory] [InlineData(1, true)] [InlineData(10000, true)] [InlineData(0, false)] [InlineData(10001, false)]
    public void Service_quantity_must_be_between_one_and_ten_thousand(int quantity, bool valid)
    {
        var form = new AddServiceForm { ServiceId = 1, EmployeeId = 1, Quantity = quantity };
        Assert.Equal(valid, FormValidation.Validate(form).All(e => !e.MemberNames.Contains(nameof(form.Quantity))));
    }
    [Theory] [InlineData(1, true)] [InlineData(10000, true)] [InlineData(-1, false)] [InlineData(10001, false)]
    public void Part_quantity_must_be_between_one_and_ten_thousand(int quantity, bool valid)
    {
        var form = new AddPartForm { PartId = 1, Quantity = quantity };
        Assert.Equal(valid, FormValidation.Validate(form).All(e => !e.MemberNames.Contains(nameof(form.Quantity))));
    }
    [Fact] public void Deadline_before_order_date_is_rejected()
    {
        var form = new OrderCreateForm { CarId = 1, OrderDate = new DateOnly(2026, 1, 2), Deadline = new DateOnly(2026, 1, 1) };
        Assert.Contains(FormValidation.Validate(form), e => e.MemberNames.Contains(nameof(form.Deadline)));
    }
    [Fact] public void Deadline_equal_to_order_date_is_accepted()
    {
        var date = new DateOnly(2026, 1, 2);
        var form = new OrderCreateForm { CarId = 1, OrderDate = date, Deadline = date };
        Assert.Empty(FormValidation.Validate(form));
    }
}

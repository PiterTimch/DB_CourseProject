using System.ComponentModel.DataAnnotations;

namespace VehicleServiceStation.Models.Forms;

public sealed class OrderCreateForm : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Оберіть автомобіль.")]
    public int CarId { get; set; }

    [Required]
    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly? Deadline { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Пробіг не може бути від’ємним.")]
    public int Mileage { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Deadline.HasValue && Deadline.Value < OrderDate)
        {
            yield return new ValidationResult("Термін не може бути раніше дати замовлення.", [nameof(Deadline)]);
        }
    }
}

public sealed class OrderEditForm : IValidatableObject
{
    public int OrderId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть автомобіль.")]
    public int CarId { get; set; }

    [Required]
    public DateOnly OrderDate { get; set; }

    public DateOnly? Deadline { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Пробіг не може бути від’ємним.")]
    public int Mileage { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Deadline.HasValue && Deadline.Value < OrderDate)
        {
            yield return new ValidationResult("Термін не може бути раніше дати замовлення.", [nameof(Deadline)]);
        }
    }
}

public sealed class AddServiceForm
{
    [Range(1, int.MaxValue, ErrorMessage = "Оберіть послугу.")]
    public int ServiceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть виконавця.")]
    public int EmployeeId { get; set; }

    [Range(1, 10000, ErrorMessage = "Кількість має бути від 1 до 10 000.")]
    public int Quantity { get; set; } = 1;
}

public sealed class AddPartForm
{
    [Range(1, int.MaxValue, ErrorMessage = "Оберіть запчастину.")]
    public int PartId { get; set; }

    [Range(1, 10000, ErrorMessage = "Кількість має бути від 1 до 10 000.")]
    public int Quantity { get; set; } = 1;
}

public sealed class DiscountForm
{
    [Range(typeof(decimal), "0", "100", ErrorMessage = "Знижка має бути від 0 до 100 відсотків.")]
    public decimal Discount { get; set; }
}

public sealed class HistoryLookupForm
{
    [Range(1, int.MaxValue, ErrorMessage = "Введіть системний ідентифікатор авто.")]
    public int CarId { get; set; }
}

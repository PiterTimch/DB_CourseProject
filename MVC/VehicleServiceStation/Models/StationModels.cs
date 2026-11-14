namespace VehicleServiceStation.Models;

public sealed class Customer
{
    public int CustomerId { get; init; }
    public string CustomerType { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public int CarCount { get; init; }
}

public sealed class Car
{
    public int CarId { get; init; }
    public int CustomerId { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string LicensePlate { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
}

public sealed class CatalogItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Article { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public bool IsPart { get; init; }
}

public sealed class OrderSummary
{
    public int OrderId { get; init; }
    public int CarId { get; init; }
    public string Car { get; init; } = string.Empty;
    public string Customer { get; init; } = string.Empty;
    public string Employee { get; init; } = string.Empty;
    public DateOnly OrderDate { get; init; }
    public DateOnly? Deadline { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalCost { get; init; }
}

public sealed class OrderLine
{
    public int ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Article { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public string Employee { get; init; } = string.Empty;
    public bool IsPart { get; init; }
    public decimal LineTotal => Quantity * Price;
}

public sealed class OrderDetails
{
    public int OrderId { get; init; }
    public int CarId { get; init; }
    public string Car { get; init; } = string.Empty;
    public string Customer { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Employee { get; init; } = string.Empty;
    public DateOnly OrderDate { get; init; }
    public DateOnly? Deadline { get; init; }
    public string Status { get; init; } = string.Empty;
    public int Mileage { get; init; }
    public decimal Discount { get; init; }
    public decimal TotalCost { get; init; }
    public List<OrderLine> Services { get; init; } = [];
    public List<OrderLine> Parts { get; init; } = [];
}

public sealed class HistoryResult
{
    public Car Car { get; init; } = new();
    public List<OrderSummary> Orders { get; init; } = [];
}

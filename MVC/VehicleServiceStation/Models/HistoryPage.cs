namespace VehicleServiceStation.Models;

public sealed class HistoryPage
{
    public int? CarId { get; init; }
    public Car? Car { get; init; }
    public IReadOnlyList<OrderSummary> Orders { get; init; } = [];
}

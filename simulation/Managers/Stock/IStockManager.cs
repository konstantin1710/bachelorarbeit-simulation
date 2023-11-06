using simulation.Models;

namespace simulation.Managers.Stock;

public interface IStockManager
{
    Task CreateStock(DateTime date);
    Task RearrangeToExistingStoragePlaces();
    Task<RearrangementsResult> SumUpLowStock();
    Task RearrangeStockBooking(StockBooking stock, int destination);
    Task<MultipleRearrangementsResult> ClearUpGroundzone();
    Task<RearrangementsResult> CondenseGroundzone();
}
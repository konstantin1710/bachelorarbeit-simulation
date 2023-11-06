using simulation.Models;

namespace simulation.Repository.Storage;

public interface IStorageRepository
{
    Task StoreArticles(StockBooking booking);
    Task<int> RemoveArticles(StockBooking booking);
    StoragePlace? GetPickplatzForArticle(StockBooking booking);
    StockBooking? GetHochzoneplatzAndAmountForArticle(StockBooking booking);
}
using simulation.Models;

namespace simulation.Repository.Stock;

public interface IStockRepository
{
    Task DeleteStock();
    List<StockBooking> GetZugaengeBeforeDate(int platzId, DateTime date);
    List<StockBooking> GetAbgaengeBeforeDate(int platzId, DateTime date);
    bool HasEnoughStock(PicklistEntry picklistEntry);
    List<IncomingGoods> GetZugaengeAtSpecificDate(DateTime date);
    List<StockBooking> GetStock(int platzId);
    List<StoragePlace> GetStoragePlacesInGroundzoneToArticle(Article article);
    double GetTakenStorageplaceRateInGroundzone();
    List<IncomingGoods> GetZugaengeForNextMonthOrderedByRank(DateTime date);
    int GetStockInGroundZone(Article article);
}
using simulation.Models;

namespace simulation.Repository.Warehouse;

public interface IWarehouseRepository
{
    List<int> GetAllPlatzIds();
    List<int> GetPlatzIdsWithoutStock();
    List<StoragePlace> GetAllNotExistingStoragePlaces();
    List<int> GetStoragePlaceIdsWithoutStock(string storagePlace, bool groundZone);
    List<StoragePlace> GetStoragePlacesWithLowStock();
    int GetNewExistingStoragePlace(StoragePlace storagePlace);
    List<string> GetStoragePlacesWithoutStock(bool groundZone);
    List<int> GetPlatzIdsWithoutStockInGroundZone();
    List<int> GetPlatzIdsWithoutStockOrderedByDistance();
    int GetStoragePlacesWithSameArticleInSameAisle(StoragePlace storagePlace, StockBooking stock);
    int GetNextMixedArticleStoragePlace();
    List<int> GetAllMixedArticleStoragePlaces();
    List<int> GetPlatzIdsWithLowFillratio();
    bool IsBodenzone(int platzId);
    int GetStoragePlaceCount();
    List<int> GetStoragePlacesToClass(int limit, int offset);
    List<int> GetStoragePlacesToClass(int articleClass);
    Task SetClassForStoragePlace(int platzId, int classToSet);
    int GetMaximumClass();
    List<StoragePlace> GetStoragePlacesByDistance();
    Task DeleteStoragePlaceClasses();
    int GetNextPlatzIdInHigherClass(int articleClass);
    int GetNextPlatzIdInLowerClass(int articleClass);
    List<int> GetStoragePlacesToClass(int articleClass, bool groundZone);
    int GetNextPlatzIdInLowerClass(int articleClass, bool groundZone);
    int GetNextPlatzIdInHigherClass(int articleClass, bool groundZone);
}
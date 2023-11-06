using simulation.Helpers;
using simulation.Models;
using simulation.Repository.Pickpool;
using simulation.Repository.Warehouse;

namespace simulation.Managers.Path;

public class PathManager : IPathManager
{
    private readonly IPickpoolRepository _pickpoolRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    public PathManager(IPickpoolRepository pickpoolRepository, IWarehouseRepository warehouseRepository)
    {
        _pickpoolRepository = pickpoolRepository;
        _warehouseRepository = warehouseRepository;
    }
    public string? GetNextStoragePlaceByDistance(int platzId, bool groundZone)
    {
        var storagePlaceIndexesByDistance =
            TspRouteCalculator.GetStoragePlacesOrderedByDistance(
                _pickpoolRepository.GetKurzbezeichnungByPlatzId(platzId));

        var distanceList = new List<string>();

        foreach (var storagePlace in storagePlaceIndexesByDistance
                     .Select(entry => TspRouteCalculator.ReverseHashmap[entry].ToList()))
        {
            distanceList.AddRange(storagePlace);
        }

        var storagePlaces = _warehouseRepository.GetStoragePlacesWithoutStock(groundZone);
        return distanceList.FirstOrDefault(x => storagePlaces.Contains(x));
    }

    public List<string> GetAllPlatzIdsByDistance(int platzId)
    {
        var storagePlaceIndexesByDistance =
            TspRouteCalculator.GetStoragePlacesOrderedByDistance(
                _pickpoolRepository.GetKurzbezeichnungByPlatzId(platzId));

        var distanceList = new List<string>();

        foreach (var storagePlace in storagePlaceIndexesByDistance
                     .Select(entry => TspRouteCalculator.ReverseHashmap[entry].ToList()))
        {
            distanceList.AddRange(storagePlace);
        }

        return distanceList;
    }

    public double GetDistanceBetweenTwoStoragePlaces(int origin, int destination)
    {
        return TspRouteCalculator.GetDistanceBetweenTwoStoragePlaces(
            _pickpoolRepository.GetKurzbezeichnungByPlatzId(origin),
            _pickpoolRepository.GetKurzbezeichnungByPlatzId(destination)) / (double)100;
    }

    public double GetDistanceBetweenTwoStoragePlaces(string origin, string destination)
    {
        return TspRouteCalculator.GetDistanceBetweenTwoStoragePlaces(origin, destination) / (double)100;
    }

    public double GetDistanceBetweenTwoNodes(int origin, int destination)
    {
        return TspRouteCalculator.GetDistanceBetweenTwoNodes(origin, destination);
    }

    public int GetIndexToPicklistEntry(PicklistEntry picklistEntry)
    {
        return TspRouteCalculator.GetIndexToStoragePlace(picklistEntry.PlatzBezeichnung ?? _pickpoolRepository.GetKurzbezeichnungByPlatzId(picklistEntry.PlatzId));
    }
}
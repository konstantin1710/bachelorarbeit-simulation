using simulation.Models;

namespace simulation.Managers.Path;

public interface IPathManager
{
    double GetDistanceBetweenTwoStoragePlaces(int origin, int destination);
    string? GetNextStoragePlaceByDistance(int platzId, bool groundZone);
    List<string> GetAllPlatzIdsByDistance(int platzId);
    double GetDistanceBetweenTwoStoragePlaces(string origin, string destination);
    double GetDistanceBetweenTwoNodes(int origin, int destination);
    int GetIndexToPicklistEntry(PicklistEntry picklistEntry);
}
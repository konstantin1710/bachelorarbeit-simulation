using simulation.Models;

namespace simulation.Repository.Pickpool;

public interface IPickpoolRepository
{
    List<PickpoolEntry> SelectPickpool(DateTime date);
    string GetKurzbezeichnungByPlatzId(int platzId);
    int GetPlatzIdByKurzbezeichnung(string storagePlace);
    List<PicklistsWithDuration> GetPicklistsWithDuration();
    List<PicklistEntry> GetPlatzIdsToPicklist(int picklistId);
    List<PicklistWithTotalSeconds> GetPicklistsWithDurationOlRewe();
    List<PicklistEntry> GetPlatzIdsToPicklistOlRewe(int picklistId);
    List<Article> GetHistoricSaleFigures();
    List<Article> GetActualSaleFigures();
}
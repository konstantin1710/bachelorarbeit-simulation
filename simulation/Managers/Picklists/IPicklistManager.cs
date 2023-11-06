using simulation.Models;

namespace simulation.Managers.Picklists;

public interface IPicklistManager
{
    List<Picklist> GetPicklists(int picklistCount = 0);
    List<Picklist> GetLengthsForPicklists(List<Picklist> picklists);
    Task<MultipleRearrangementsResult> SetReservations(DateTime date);
    Task ProcessPicklists(List<Picklist> picklists);
    List<Picklist> GetOptimizedPicklists(int picklistCount = 0);
}
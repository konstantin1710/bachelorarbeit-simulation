namespace simulation.Models;

public class UpdatePickspotsResult
{
    public UpdatePickspotsResult(List<Picklist> picklists, MultipleRearrangementsResult rearrangements)
    {
        Picklists = picklists;
        Rearrangements = rearrangements;
    }

    public List<Picklist> Picklists { get; set; }
    public MultipleRearrangementsResult Rearrangements { get; set; }
}
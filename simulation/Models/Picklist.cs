namespace simulation.Models;

public class Picklist
{
    public List<PicklistEntry> PicklistEntries { get; set; } = new();
    public double Length { get; set; }
}
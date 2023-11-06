namespace simulation.Models;

public class SimulationResult
{
    public DateTime Date { get; set; }
    public double Length { get; set; }
    public double PicklistEntryCount { get; set; }
    public int PicklistCount { get; set; }
    public int RearrangementCountHighzoneGroundzone { get; set; }
    public double RearrangementLengthHighzoneGroundzone { get; set; }
    public int RearrangementCountInGroundzone { get; set; }
    public double RearrangementLengthInGroundzone { get; set; }
}
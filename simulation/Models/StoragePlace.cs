namespace simulation.Models;

public class StoragePlace
{
    public int PlatzId { get; set; }
    public string? PlatzBezeichnung { get; set; }
    public int IsBodenzone { get; set; }
    public string? Unit { get; set; }
    public string? Gang { get; set; }
    public string? Platz { get; set; }
    public double Distance { get; set; }
    public double Fillratio { get; set; }
    public int StoragePlaceClass { get; set; }
}
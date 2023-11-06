namespace simulation.Models;

public class DistanceResult
{
    public string Origin { get; set; } = null!;
    public string Destination { get; set; } = null!;
    public double Distance { get; set; }
    public int Time { get; set; }
    public double Speed { get; set; }
}
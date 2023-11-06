namespace simulation.Models;

public class StockTransfer
{
    public StockTransfer(int origin, int destination)
    {
        Origin = origin;
        Destination = destination;
    }
    public int Origin { get; set; }
    public int Destination { get; set; }
}
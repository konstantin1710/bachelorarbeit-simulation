namespace simulation.Models;

public class TspDataModel
{
    public TspDataModel(long[,] tourDistanceMatrix)
    {
        TourDistanceMatrix = tourDistanceMatrix;
    }

    public readonly long[,] TourDistanceMatrix;
    public int VehicleNumber = 1;
    public int Depot = 0;
}
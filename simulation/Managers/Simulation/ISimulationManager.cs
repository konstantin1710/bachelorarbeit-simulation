using simulation.Models;

namespace simulation.Managers.Simulation;

public interface ISimulationManager
{
    Task<List<SimulationResult>> SimulatePicking(Strategy strategy, DateTime date, int numberOfDays, bool betterPicklists,
        int numberOfClasses = 2, bool optimizedGroundzone = false, bool exactForecast = false);
    List<DistanceResult> GetDistances(List<DistanceResult> storagePlaces);
    double CalculatePickSpeed();
    double CalculatePickSpeedOlRewe();
    (int, float) GetSaleFigureStatistics();
}
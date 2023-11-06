using simulation.Models;

namespace simulation.Managers.Strategies;

public interface IStrategyManager
{
    Task Random(DateTime date);
    Task<MultipleRearrangementsResult> Current(DateTime date);
    Task RandomWithPreferredGroundZone(DateTime date);
    Task PreferredLowDistance(DateTime date);
    Task DistanceBySalesRank(DateTime date, bool optimizedGroundzone, bool exactForecast);
    Task Classes(DateTime date, int numberOfClasses);
}
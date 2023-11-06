using Microsoft.AspNetCore.Mvc;
using simulation.Managers.Simulation;
using simulation.Models;

namespace simulation.Controllers;

[ApiController]
[Route("/api/simulation")]
public class SimulationController : ControllerBase
{
    private readonly ISimulationManager _simulationManager;

    public SimulationController(ISimulationManager simulationManager)
    {
        _simulationManager = simulationManager;
    }

    /// <summary>
    /// Hauptmethode, die die Einlagerung, Umlagerung und Kommissionierung im Beispiellager simuliert
    /// </summary>
    /// <param name="strategy">Strategie der Einlagerung</param>
    /// <param name="date">Startdatum</param>
    /// <param name="numberOfDays">Anzahl an Tagen, die simuliert werden sollen</param>
    /// <param name="betterPicklists">Gibt an, ob bessere Picklisten mit Berücksichtigung der lokalen Zusammenhänge von Lagerplätzen erstellt werden sollen</param>
    /// <param name="numberOfClasses">Anzahl der Klassen für Classes-Strategy. Optional für alle anderen Strategien, default 2.</param>
    /// <param name="exactForecast">Gibt an, ob für Vergleichszwecke eine perfekte Verkaufsprognose verwendet werden soll</param>
    /// <param name="optimizedGroundzone"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("simulate-picking")]
    public async Task<ActionResult<List<SimulationResult>>> SimulatePicking(Strategy strategy, DateTime date, int numberOfDays,
        bool betterPicklists, int numberOfClasses = 2, bool optimizedGroundzone = false, bool exactForecast = false)
    {
        return Ok(await _simulationManager.SimulatePicking(strategy, date, numberOfDays, betterPicklists, numberOfClasses, optimizedGroundzone, exactForecast));
    }

    [HttpPost]
    [Route("get-distances")]
    public ActionResult<List<DistanceResult>> GetDistances(List<DistanceResult> storagePlaces)
    {
        return Ok(_simulationManager.GetDistances(storagePlaces));
    }

    [HttpPost]
    [Route("calculate-pick-speed")]
    public ActionResult<double> CalculatePickSpeed()
    {
        return Ok(_simulationManager.CalculatePickSpeed());
    }

    [HttpPost]
    [Route("calculate-pick-speed-olrewe")]
    public ActionResult<double> CalculatePickSpeedOlRewe()
    {
        return Ok(_simulationManager.CalculatePickSpeedOlRewe());
    }

    [HttpPost]
    [Route("get-sale-figure-statistics")]
    public ActionResult<(int, float)> GetSaleFigureStatistics()
    {
        return Ok(_simulationManager.GetSaleFigureStatistics());
    }
}
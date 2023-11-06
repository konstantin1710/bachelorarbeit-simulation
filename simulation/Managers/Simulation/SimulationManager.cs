using simulation.Helpers;
using simulation.Managers.Picklists;
using simulation.Managers.Stock;
using simulation.Managers.Strategies;
using simulation.Models;
using simulation.Repository.Pickpool;

namespace simulation.Managers.Simulation;

public class SimulationManager : ISimulationManager
{
    private readonly IPicklistManager _picklistManager;
    private readonly IStockManager _stockManager;
    private readonly IStrategyManager _strategyManager;
    private readonly IPickpoolRepository _pickpoolRepository;

    public SimulationManager(IPicklistManager picklistManager, IStockManager stockManager,
        IStrategyManager strategyManager, IPickpoolRepository pickpoolRepository)
    {
        _picklistManager = picklistManager;
        _stockManager = stockManager;
        _strategyManager = strategyManager;
        _pickpoolRepository = pickpoolRepository;
    }
    public async Task<List<SimulationResult>> SimulatePicking(Strategy strategy, DateTime date, int numberOfDays,
        bool betterPicklists, int numberOfClasses = 2, bool optimizedGroundzone = false, bool exactForecast = false)
    {
        var result = new List<SimulationResult>();
        List<Picklist> picklists;

        Console.WriteLine($"\nKreiere Bestand für Datum {date:dd.MM.yyyy}\n");
        await _stockManager.CreateStock(date);
        await _stockManager.RearrangeToExistingStoragePlaces();

        for (var i = 0; i < numberOfDays; i++)
        {
            Console.WriteLine($"\n\n{date:dd.MM.yyyy}:");
            var multipleRearrangements = new MultipleRearrangementsResult();
            if (i > 0)
            {
                Console.WriteLine("Aktualisiere Bestände");
                multipleRearrangements.Add(await StoreNewArticles(strategy, date, numberOfClasses, optimizedGroundzone, exactForecast));
            }

            Console.WriteLine("Setze Platzreservierungen");
            var reservationsResult = await _picklistManager.SetReservations(date);
            if (reservationsResult.GroundzoneGroundzone.Count > 0)
            {
                reservationsResult.Add(await _picklistManager.SetReservations(date));
            }
            multipleRearrangements.Add(reservationsResult);

            Console.WriteLine("Ermittle Picklisten");
            picklists = betterPicklists ? _picklistManager.GetOptimizedPicklists() : _picklistManager.GetPicklists();

            if (picklists.Count == 0)
            {
                Console.WriteLine("Heute keine Picklisten");
                date = date.AddDays(1);
                continue;
            }

            Console.WriteLine("Arbeite Picklisten ab");
            await _picklistManager.ProcessPicklists(picklists);

            Console.WriteLine("Berechne Weglänge");
            var aggregadedLength = picklists.Sum(TspRouteCalculator.GetRoute);

            Console.WriteLine("Lagere einzelne Artikel um");
            var rearrangementsInGroundZone = await _stockManager.SumUpLowStock();

            Console.WriteLine("Aggregiere Bodenzone");
            rearrangementsInGroundZone.Add(await _stockManager.CondenseGroundzone());

            result.Add(new SimulationResult
            {
                Date = date,
                Length = aggregadedLength,
                PicklistEntryCount = picklists.Sum(p => p.PicklistEntries.Count),
                PicklistCount = picklists.Count,
                RearrangementCountHighzoneGroundzone = multipleRearrangements.HighzoneGroundzone.Count,
                RearrangementLengthHighzoneGroundzone = multipleRearrangements.HighzoneGroundzone.Length,
                RearrangementCountInGroundzone = rearrangementsInGroundZone.Count + multipleRearrangements.GroundzoneGroundzone.Count,
                RearrangementLengthInGroundzone = rearrangementsInGroundZone.Length + multipleRearrangements.GroundzoneGroundzone.Length
            });

            Console.WriteLine("Tag beendet");
            date = date.AddDays(1);
        }

        Console.WriteLine($"\nBerechnung erfolgreich beendet um {DateTime.Now.TimeOfDay}");
        return result;
    }

    private async Task<MultipleRearrangementsResult> StoreNewArticles(Strategy strategy, DateTime date, int numberOfClasses = 2,
        bool optimizedGroundzone = false,
        bool exactForecast = false)
    {
        var result = new MultipleRearrangementsResult();
        switch (strategy)
        {
            case Strategy.Current:
                result = await _strategyManager.Current(date);
                break;
            case Strategy.Random:
                await _strategyManager.Random(date);
                break;
            case Strategy.RandomWithPreferredGroundZone:
                await _strategyManager.RandomWithPreferredGroundZone(date);
                break;
            case Strategy.PreferredLowDistance:
                await _strategyManager.PreferredLowDistance(date);
                break;
            case Strategy.DistanceBySalesRank:
                await _strategyManager.DistanceBySalesRank(date, optimizedGroundzone, exactForecast);
                break;
            case Strategy.Classes:
                await _strategyManager.Classes(date, numberOfClasses);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
        }

        await _stockManager.RearrangeToExistingStoragePlaces();
        return result;
    }


    public List<DistanceResult> GetDistances(List<DistanceResult> storagePlaces)
    {
        foreach (var distanceResult in storagePlaces)
        {
            distanceResult.Distance =
                TspRouteCalculator.GetDistanceBetweenTwoStoragePlaces(distanceResult.Origin, distanceResult.Destination) / (double) 100;
            distanceResult.Speed = distanceResult.Distance / distanceResult.Time;
        }
        return storagePlaces;
    }

    public double CalculatePickSpeed()
    {
        var picklists = _pickpoolRepository.GetPicklistsWithDuration();
        picklists.RemoveAll(x => x.Duration.TotalSeconds == 0);
        picklists.RemoveAll(x => x.Duration.TotalHours > 8);

        double totalLength = 0;
        var toDelete = new List<PicklistsWithDuration>();
        foreach (var picklist in picklists)
        {
            var entries = _pickpoolRepository.GetPlatzIdsToPicklist(picklist.PicklistenId);
            try
            {
                totalLength += TspRouteCalculator.GetRoute(new Picklist
                {
                    PicklistEntries = entries
                });
            }
            catch (Exception)
            {
                toDelete.Add(picklist);
            }
        }
        picklists = picklists.Except(toDelete).ToList();
        var totalSeconds = picklists.Sum(x => x.Duration.TotalSeconds);
        return totalLength / totalSeconds;
    }

    public double CalculatePickSpeedOlRewe()
    {
        var picklists = _pickpoolRepository.GetPicklistsWithDurationOlRewe();
        var picklistsDateTime = picklists.Select(picklist => new PicklistsWithDuration
            {
                PicklistenId = picklist.PicklistenId,
                Duration = TimeSpan.FromSeconds(picklist.Duration)
            })
            .ToList();
        picklistsDateTime.RemoveAll(x => x.Duration.TotalSeconds == 0);
        picklistsDateTime.RemoveAll(x => x.Duration.TotalHours > 8);

        double totalLength = 0;
        var toDelete = new List<PicklistsWithDuration>();
        foreach (var picklist in picklistsDateTime)
        {
            var entries = _pickpoolRepository.GetPlatzIdsToPicklistOlRewe(picklist.PicklistenId);
            try
            {
                totalLength += TspRouteCalculator.GetRoute(new Picklist
                {
                    PicklistEntries = entries
                });
            }
            catch (Exception)
            {
                toDelete.Add(picklist);
            }
        }
        picklistsDateTime = picklistsDateTime.Except(toDelete).ToList();
        var totalSeconds = picklistsDateTime.Sum(x => x.Duration.TotalSeconds);
        return totalLength / totalSeconds;
    }

    public (int, float) GetSaleFigureStatistics()
    {
        var history = _pickpoolRepository.GetHistoricSaleFigures();
        var actual = _pickpoolRepository.GetActualSaleFigures();

        var intersect = history.Where(x => actual.Any(y => x.Artikelnummer == y.Artikelnummer && x.Variante == y.Variante)).ToList();
        var result1 = intersect.Count;

        var counter = 0;
        var running = 0;
        foreach (var article in history)
        {
            var index = actual.FindIndex(x => x.Artikelnummer == article.Artikelnummer && x.Variante == article.Variante);
            if (index < 0)
            {
                running += 1001;
            }
            else
            {
                running += Math.Abs(index - counter) ;
            }
            counter++;
        }

        var result2 = running / (float)counter;
        return (result1, result2);
    }
}
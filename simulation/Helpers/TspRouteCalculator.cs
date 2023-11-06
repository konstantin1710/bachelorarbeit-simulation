using System.Text;
using Google.OrTools.ConstraintSolver;
using Newtonsoft.Json;
using simulation.Models;

namespace simulation.Helpers;

/// <summary>
/// Minimal TSP using distance matrix.
/// </summary>
public class TspRouteCalculator
{
    private static long[,] DistanceMatrix { get; } = JsonConvert.DeserializeObject<long[,]>(File.ReadAllText(@"..\..\simulator\GRO-distances.json", Encoding.UTF8))!;
    private static IReadOnlyDictionary<string, int> HashMap { get; } = JsonConvert.DeserializeObject<IReadOnlyDictionary<string, int>>(File.ReadAllText(@"..\..\simulator\GRO-hash_map.json", Encoding.UTF8))!;
    public static ILookup<int, string> ReverseHashmap { get; } = HashMap.ToLookup(x => x.Value, x => x.Key);

    public static double GetRoute(Picklist picklist)
    {
        var nodes = GetNodesFromPicklists(picklist);

        //Depot in GRO-48 mittig ganz unten
        nodes.Insert(0, 2082);

        var tourDistanceMatrix = new long[nodes.Count,nodes.Count];

        for (var i = 0; i < nodes.Count; i++)
        {
            for (var j = 0; j < nodes.Count; j++)
            {
                tourDistanceMatrix[i, j] = DistanceMatrix[nodes[i], nodes[j]];
            }
        }

        // Instantiate the data problem.
        var data = new TspDataModel(tourDistanceMatrix);

        // Create Routing Index Manager
        var manager =
            new RoutingIndexManager(data.TourDistanceMatrix.GetLength(0), data.VehicleNumber, data.Depot);

        // Create Routing Model.
        var routing = new RoutingModel(manager);

        var transitCallbackIndex = routing.RegisterTransitCallback((fromIndex, toIndex) =>
        {
            // Convert from routing variable Index to
            // distance matrix NodeIndex.
            var fromNode = manager.IndexToNode(fromIndex);
            var toNode = manager.IndexToNode(toIndex);
            return data.TourDistanceMatrix[fromNode, toNode];
        });

        // Define cost of each arc.
        routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

        // Setting first solution heuristic.
        var searchParameters =
            operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        //searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        //searchParameters.TimeLimit = new Duration { Seconds = 1 };

        // Solve the problem.
        var solution = routing.SolveWithParameters(searchParameters);

        return solution.ObjectiveValue() / (double)100;
    }

    private static List<int> GetNodesFromPicklists(Picklist picklist)
    {
        var result = new List<int>();
        foreach (var picklistEntry in picklist.PicklistEntries)
        {
            HashMap.TryGetValue(picklistEntry.PlatzBezeichnung!.Remove(picklistEntry.PlatzBezeichnung.Length - 2), out var index);
            result.Add(index);
            if (index == 0 && picklistEntry.PlatzId != 114384 && picklistEntry.PlatzId != 114378)
            {
                Console.WriteLine($"Lagerplatz {picklistEntry.PlatzBezeichnung} mit Platzid {picklistEntry.PlatzId} ist nicht in der Hashmap enthalten");
                throw new Exception();
            }
        }
        return result;
    }

    public static List<int> GetStoragePlacesOrderedByDistance(string storagePlace)
    {
        HashMap.TryGetValue(storagePlace.Remove(storagePlace.Length - 2), out var index);
        var distances = new Dictionary<int, long>();

        for (var i = 0; i < DistanceMatrix.GetLength(0); i++)
        {
            distances.Add(i, DistanceMatrix[index, i]);
        }

        return distances.OrderBy(x => x.Value).Select(distance => distance.Key).ToList();
    }

    public static long GetDistanceBetweenTwoStoragePlaces(string origin, string destination)
    {
        HashMap.TryGetValue(origin.Remove(origin.Length - 2), out var originIndex);
        HashMap.TryGetValue(destination.Remove(destination.Length - 2), out var destinationIndex);
        return DistanceMatrix[originIndex, destinationIndex];
    }

    public static long GetDistanceBetweenTwoNodes(int origin, int destination)
    {
        return DistanceMatrix[origin, destination];
    }

    public static int GetIndexToStoragePlace(string storagePlace)
    {
        HashMap.TryGetValue(storagePlace.Remove(storagePlace.Length - 2), out var index);
        return index;
    }
}
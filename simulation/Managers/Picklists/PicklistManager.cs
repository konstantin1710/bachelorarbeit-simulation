using System.Collections.Concurrent;
using System.Diagnostics;
using simulation.Helpers;
using simulation.Managers.Path;
using simulation.Managers.Stock;
using simulation.Models;
using simulation.Repository.Articles;
using simulation.Repository.Pickpool;
using simulation.Repository.Reservation;
using simulation.Repository.Stock;
using simulation.Repository.Storage;
using simulation.Repository.Warehouse;

namespace simulation.Managers.Picklists;

public class PicklistManager : IPicklistManager
{
    private readonly IConfiguration _configuration;
    private readonly IStockManager _stockManager;
    private readonly IPathManager _pathManager;
    private readonly IArticleRepository _articleRepository;
    private readonly IPickpoolRepository _pickpoolRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    public PicklistManager(IConfiguration configuration, IStockManager stockManager,
        IPathManager pathManager, IArticleRepository articleRepository,
        IPickpoolRepository pickpoolRepository, IReservationRepository reservationRepository,
        IStockRepository stockRepository, IStorageRepository storageRepository,
        IWarehouseRepository warehouseRepository)
    {
        _configuration = configuration;
        _stockManager = stockManager;
        _pathManager = pathManager;
        _articleRepository = articleRepository;
        _pickpoolRepository = pickpoolRepository;
        _reservationRepository = reservationRepository;
        _stockRepository = stockRepository;
        _storageRepository = storageRepository;
        _warehouseRepository = warehouseRepository;
    }

    private List<PickpoolEntry> _pickpool = null!;
    private Dictionary<int, List<PickpoolEntry>> _hashmap = null!;

    public List<Picklist> GetPicklists(int picklistCount = 0)
    {
        var result = new List<Picklist>();

        while (_pickpool.Count > 0)
        {
            var picklistEntries = GetNextPicklist();
            if (picklistEntries.Count == 0)
            {
                continue;
            }
            result.Add(new Picklist
            {
                PicklistEntries = picklistEntries
            });

            if (picklistCount != 0 && result.Count == picklistCount)
            {
                break;
            }
        }
        return result;
    }

    public List<Picklist> GetOptimizedPicklists(int picklistCount = 0)
    {
        var result = new List<Picklist>();
        _hashmap = CreateHashmap();

        while (_hashmap.Count > 0)
        {
            var picklistEntries = GetNextOptimizedPicklist();
            if (picklistEntries.Count == 0)
            {
                continue;
            }
            result.Add(new Picklist
            {
                PicklistEntries = picklistEntries
            });

            if (picklistCount != 0 && result.Count == picklistCount)
            {
                break;
            }
        }
        return result;
    }

    private Dictionary<int, List<PickpoolEntry>> CreateHashmap()
    {
        var result = new Dictionary<int, List<PickpoolEntry>>();
        var pickpoolChunks = _pickpool.Chunk(_configuration.GetValue<int>("ChunkSize"));

        foreach (var chunk in pickpoolChunks)
        {
            Parallel.ForEach(chunk, pickpoolEntry =>
            {
                pickpoolEntry.FlaecheMax = _articleRepository
                    .GetAllDimensionsForArticle(pickpoolEntry.Artikelnummer, pickpoolEntry.Variante).FlaecheMax;
            });
        }

        foreach (var pickpoolEntry in _pickpool)
        {
            var index = TspRouteCalculator.GetIndexToStoragePlace(pickpoolEntry.PlatzBezeichnung ?? _pickpoolRepository.GetKurzbezeichnungByPlatzId(pickpoolEntry.PlatzId));
            if (result.TryGetValue(index, out var list))
            {
                list.Add(pickpoolEntry);
            }
            else
            {
                result.Add(index, new List<PickpoolEntry>
                {
                    pickpoolEntry
                });
            }
        }

        return result;
    }

    /// <summary>
    /// returns next picklist and removes picklistentries from pickpool
    /// </summary>
    /// <returns>list of picklistentries of next picklist</returns>
    private List<PicklistEntry> GetNextPicklist()
    {
        var picklist = new List<PicklistEntry>();
        decimal currentArea = 0;
        var pickwagenArea = GetPickwagenArea();
        foreach (var pickPoolEntry in _pickpool.ToList())
        {
            var attributes = _articleRepository.GetAllDimensionsForArticle(pickPoolEntry.Artikelnummer, pickPoolEntry.Variante);
            if (currentArea + attributes.FlaecheMax * pickPoolEntry.Menge <= pickwagenArea)
            {
                currentArea += attributes.FlaecheMax * pickPoolEntry.Menge;
                var newPicklistEntry = MapPickpoolEntryToPicklistEntry(pickPoolEntry);
                picklist.Add(newPicklistEntry);
                _pickpool.Remove(pickPoolEntry);
            }
            else
            {
                if (currentArea == 0) _pickpool.Remove(pickPoolEntry);
                break;
            }
        }

        return picklist;
    }

    private List<PicklistEntry> GetNextOptimizedPicklist()
    {
        var random = new Random();
        var picklist = new List<PicklistEntry>();
        var pickwagenArea = GetPickwagenArea();

        var randomKey = random.Next(_hashmap.Count);
        var randomIndex = random.Next(_hashmap.ElementAt(randomKey).Value.Count);
        var firstEntry = _hashmap.ElementAt(randomKey).Value[randomIndex];

        RemoveEntryFromHashmap(_hashmap.ElementAt(randomKey).Key, randomIndex);
        var currentArea = firstEntry.FlaecheMax * firstEntry.Menge;
        if (currentArea + firstEntry.FlaecheMax * firstEntry.Menge > pickwagenArea)
        {
            return picklist;
        }
        picklist.Add(MapPickpoolEntryToPicklistEntry(firstEntry));

        while (true)
        {
            var nextNode = GetNextNode(picklist);
            if (nextNode == -1) break;

            for (var i = _hashmap[nextNode].Count - 1; i >= 0; i--)
            {
                var pickpoolEntry = _hashmap[nextNode][i];
                if (currentArea + pickpoolEntry.FlaecheMax * pickpoolEntry.Menge <= pickwagenArea)
                {
                    currentArea += pickpoolEntry.FlaecheMax * pickpoolEntry.Menge;
                    var newPicklistEntry = MapPickpoolEntryToPicklistEntry(pickpoolEntry);
                    picklist.Add(newPicklistEntry);
                    RemoveEntryFromHashmap(nextNode, i);
                }
                else
                {
                    if (currentArea == 0) RemoveEntryFromHashmap(nextNode, i);
                    return picklist;
                }
            }
        }

        return picklist;
    }

    private void RemoveEntryFromHashmap(int key, int index)
    {
        _hashmap[key].RemoveAt(index);
        if (_hashmap[key].Count == 0)
        {
            _hashmap.Remove(key);
        }
    }

    private int GetNextNode(List<PicklistEntry> picklistSoFar)
    {
        var minDistance = double.PositiveInfinity;
        var minNodeIndex = -1;
        foreach (var node in _hashmap)
        {
            foreach (var picklistEntry in picklistSoFar)
            {
                var distance = _pathManager.GetDistanceBetweenTwoNodes(node.Key, _pathManager.GetIndexToPicklistEntry(picklistEntry));
                if (distance == 0)
                {
                    return node.Key;
                }

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minNodeIndex = node.Key;
                }
            }
        }

        return minNodeIndex;
    }

    /// <summary>
    /// calculates pickwagen-area with formula clusterFactor * length * width * countOfAreasAtPickwagen
    /// </summary>
    /// <returns>area as decimal</returns>
    private decimal GetPickwagenArea()
    {
        var clusterFactor = _configuration.GetValue<int>("Pickwagen:ClusterFactor");
        var length = _configuration.GetValue<int>("Pickwagen:Length");
        var width = _configuration.GetValue<int>("Pickwagen:Width");
        var areas = _configuration.GetValue<int>("Pickwagen:Areas");
        return clusterFactor * length * width * areas;
    }

    private PicklistEntry MapPickpoolEntryToPicklistEntry(PickpoolEntry pickPoolEntry)
    {
        return new PicklistEntry
        {
            PlatzId = pickPoolEntry.PlatzId,
            PlatzBezeichnung = _pickpoolRepository.GetKurzbezeichnungByPlatzId(pickPoolEntry.PlatzId),
            Menge = pickPoolEntry.Menge,
            Artikelnummer = pickPoolEntry.Artikelnummer,
            Variante = pickPoolEntry.Variante,
            PicklistenId = pickPoolEntry.PicklistenId,
            Pickzeit = pickPoolEntry.Pickzeit
        };
    }

    /// <summary>
    /// modifies lengths of picklists
    /// </summary>
    /// <param name="picklists"></param>
    /// <returns></returns>
    public List<Picklist> GetLengthsForPicklists(List<Picklist> picklists)
    {
        foreach (var picklist in picklists)
        {
            picklist.Length = TspRouteCalculator.GetRoute(picklist);
        }

        return picklists;
    }

    public async Task<MultipleRearrangementsResult> SetReservations(DateTime date)
    {
        var result = new MultipleRearrangementsResult();
        var toDelete = new List<PickpoolEntry>();

        await _reservationRepository.DeleteReservations();
        _pickpool = _pickpoolRepository.SelectPickpool(date);

        foreach (var pickpoolEntry in _pickpool)
        {
            var tempResult = await GetAndReservePickspot(pickpoolEntry);
            if (tempResult == null)
            {
                toDelete.Add(pickpoolEntry);
            }
            else if (tempResult.GroundzoneGroundzone.Count > 0)
            {
                result.Add(tempResult);
                return result;
            }
            else
            {
                result.Add(tempResult);
            }
        }
        if (toDelete.Count > 0) _pickpool = _pickpool.Except(toDelete).ToList(); //Einträge entfernen, wenn kein Bestand da

        return result;
    }

    private async Task<MultipleRearrangementsResult?> GetAndReservePickspot(PickpoolEntry pickpoolEntry)
    {
        var result = new MultipleRearrangementsResult();
        var storagePlace = _storageRepository.GetPickplatzForArticle(pickpoolEntry.ToStockBooking());

        if (storagePlace == null || storagePlace.PlatzId == 0)
        {
            var rearrangementsResult = await GetNewPlatzIdByRearrangement(pickpoolEntry);

            if (rearrangementsResult is null)
            {
                return null;
            }

            result = rearrangementsResult;
            storagePlace = _storageRepository.GetPickplatzForArticle(pickpoolEntry.ToStockBooking());
        }
        pickpoolEntry.PlatzId = storagePlace!.PlatzId;
        pickpoolEntry.PlatzBezeichnung = storagePlace.PlatzBezeichnung;

        await _reservationRepository.ReserveArticle(pickpoolEntry.ToStockBooking());

        return result;
    }

    private async Task<MultipleRearrangementsResult?> GetNewPlatzIdByRearrangement(PickpoolEntry pickpoolEntry)
    {
        var hochzone = _storageRepository.GetHochzoneplatzAndAmountForArticle(pickpoolEntry.ToStockBooking());

        if (hochzone == null || hochzone.PlatzId == 0)
        {
            return null;
        }

        var booking = new StockBooking
        {
            PlatzId = hochzone.PlatzId,
            Artikelnummer = pickpoolEntry.Artikelnummer,
            Variante = pickpoolEntry.Variante,
            Menge = hochzone.Menge
        };

        var newStoragePlace = _pathManager.GetNextStoragePlaceByDistance(hochzone.PlatzId, true);

        if (string.IsNullOrEmpty(newStoragePlace))
        {
            var result = await _stockManager.ClearUpGroundzone();
            var rearrangement = await GetNewPlatzIdByRearrangement(pickpoolEntry);
            if (rearrangement != null)
            {
                result.Add(rearrangement);
            }
            return result;
        }

        await _storageRepository.RemoveArticles(booking);
        booking.PlatzId = _warehouseRepository.GetStoragePlaceIdsWithoutStock(newStoragePlace, true).First();
        await _storageRepository.StoreArticles(booking);

        return new MultipleRearrangementsResult
        {
            HighzoneGroundzone =
            {
                Count = 1,
                Length = _pathManager.GetDistanceBetweenTwoStoragePlaces(hochzone.PlatzId, booking.PlatzId)
            }
        };
    }

    public async Task ProcessPicklists(List<Picklist> picklists)
    {
        var picklistChunks = picklists.Chunk(_configuration.GetValue<int>("ChunkSize"));
        foreach (var chunk in picklistChunks)
        {
            var tasks = new ConcurrentBag<Task>();
            Parallel.ForEach(chunk, picklist =>
            {
                foreach (var entry in picklist.PicklistEntries)
                {
                    tasks.Add(ProcessSinglePicklistEntry(entry));
                }
            });
            await Task.WhenAll(tasks);
        }
    }

    private async Task ProcessSinglePicklistEntry(PicklistEntry picklistEntry)
    {
        var stock = _stockRepository.GetStock(picklistEntry.PlatzId).Where(x => x.Artikelnummer == picklistEntry.Artikelnummer && x.Variante == picklistEntry.Variante).ToList();
        if (stock.Count == 0 || stock.First().Menge < picklistEntry.Menge)
        {
            Console.WriteLine($"Artikel {picklistEntry.Artikelnummer}_{picklistEntry.Variante} hat auf Lagerplatz {picklistEntry.PlatzId} nicht genug Bestand zum Picken.");
            Debugger.Break();
        }
        await _storageRepository.RemoveArticles(picklistEntry.ToStockBooking());
        await _reservationRepository.RemoveReservation(picklistEntry.ToStockBooking());
    }
}
using System.Collections.Concurrent;
using simulation.Managers.Path;
using simulation.Models;
using simulation.Repository.Articles;
using simulation.Repository.Pickpool;
using simulation.Repository.Stock;
using simulation.Repository.Storage;
using simulation.Repository.Warehouse;

namespace simulation.Managers.Stock;

public class StockManager : IStockManager
{
    private readonly IConfiguration _configuration;
    private readonly IStockRepository _stockRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly IPickpoolRepository _pickpoolRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IPathManager _pathManager;

    public StockManager(IConfiguration configuration, IPathManager pathManager,
        IStockRepository stockRepository, IArticleRepository articleRepository,
        IStorageRepository storageRepository, IPickpoolRepository pickpoolRepository,
        IWarehouseRepository warehouseRepository)
    {
        _configuration = configuration;
        _stockRepository = stockRepository;
        _articleRepository = articleRepository;
        _storageRepository = storageRepository;
        _pickpoolRepository = pickpoolRepository;
        _warehouseRepository = warehouseRepository;
        _pathManager = pathManager;
    }

    public async Task CreateStock(DateTime date)
    {
        await _stockRepository.DeleteStock();
        var platzIds = _warehouseRepository.GetAllPlatzIds();
        var platzIdsChunk = platzIds.Chunk(_configuration.GetValue<int>("ChunkSize"));

        foreach (var platzIdChunk in platzIdsChunk)
        {
            var tasks = new ConcurrentBag<Task>();
            Parallel.ForEach(platzIdChunk, platzId =>
            {
                var zugaenge = _stockRepository.GetZugaengeBeforeDate(platzId, date);
                if (zugaenge.Count == 0) return;

                var abgaenge = _stockRepository.GetAbgaengeBeforeDate(platzId, date);

                tasks.Add(CalculateStockForSingleStoragePlace(zugaenge, abgaenge, platzId));
            });
            await Task.WhenAll(tasks);
        }
    }

    private async Task CalculateStockForSingleStoragePlace(List<StockBooking> zugaenge, List<StockBooking> abgaenge, int platzId)
    {
        foreach (var zugang in zugaenge)
        {
            var abgangAmount = abgaenge.Exists(a => a.Artikelnummer == zugang.Artikelnummer && a.Variante == zugang.Variante)
                ? abgaenge.Find(a => a.Artikelnummer == zugang.Artikelnummer && a.Variante == zugang.Variante)!.Menge
                : 0;
            zugang.Menge -= abgangAmount;
            if (zugang.Menge > 0)
            {
                await _storageRepository.StoreArticles(new StockBooking
                {
                    PlatzId = platzId,
                    Artikelnummer = zugang.Artikelnummer,
                    Variante = zugang.Variante,
                    Menge = zugang.Menge
                });
            }
        }
    }

    public async Task RearrangeToExistingStoragePlaces()
    {
        var notExistingStoragePlaces = _warehouseRepository.GetAllNotExistingStoragePlaces();
        var storagePlaceChunks = notExistingStoragePlaces.Chunk(_configuration.GetValue<int>("ChunkSize"));

        foreach (var storagePlaceChunk in storagePlaceChunks)
        {
            var tasks = new ConcurrentBag<Task>();
            Parallel.ForEach(storagePlaceChunk, storagePlace =>
            {
                var stock = _stockRepository.GetStock(storagePlace.PlatzId);
                foreach (var stockBooking in stock)
                {
                    tasks.Add(RearrangeFromNotExistingToExistingStoragePlace(stockBooking, storagePlace.IsBodenzone));
                }
            });
            await Task.WhenAll(tasks);
        }
    }

    private async Task RearrangeFromNotExistingToExistingStoragePlace(StockBooking stockBooking, int isBodenzone)
    {
        var unit = _pickpoolRepository.GetKurzbezeichnungByPlatzId(stockBooking.PlatzId).Substring(4, 2);
        var newPlatzId = _warehouseRepository.GetNewExistingStoragePlace(new StoragePlace
        {
            PlatzId = stockBooking.PlatzId,
            IsBodenzone = isBodenzone,
            Unit = unit,
            Gang = _pickpoolRepository.GetKurzbezeichnungByPlatzId(stockBooking.PlatzId).Split(';')[1],
            Platz = _pickpoolRepository.GetKurzbezeichnungByPlatzId(stockBooking.PlatzId).Split(';')[2]
        });

        await _storageRepository.RemoveArticles(stockBooking);
        stockBooking.PlatzId = newPlatzId;
        await _storageRepository.StoreArticles(stockBooking);
    }

    public async Task<RearrangementsResult> SumUpLowStock()
    {
        var result = new RearrangementsResult();

        var storagePlacesWithLowStock = _warehouseRepository.GetStoragePlacesWithLowStock();
        var mixedArticleStoragePlaces = _warehouseRepository.GetAllMixedArticleStoragePlaces();

        storagePlacesWithLowStock.RemoveAll(x => mixedArticleStoragePlaces.Contains(x.PlatzId));

        foreach (var storagePlace in storagePlacesWithLowStock)
        {
            var stock = _stockRepository.GetStock(storagePlace.PlatzId);
            foreach (var stockEntry in stock)
            {
                var sameAisleDestination = _warehouseRepository.GetStoragePlacesWithSameArticleInSameAisle(storagePlace, stockEntry);
                var destination = sameAisleDestination != 0
                    ? sameAisleDestination
                    : _warehouseRepository.GetNextMixedArticleStoragePlace();

                result.Length += _pathManager.GetDistanceBetweenTwoStoragePlaces(stockEntry.PlatzId, destination);

                await RearrangeStockBooking(stockEntry, destination);

                result.Count++;
            }
        }

        return result;
    }

    public async Task RearrangeStockBooking(StockBooking stock, int destination)
    {
        await _storageRepository.RemoveArticles(stock);
        stock.PlatzId = destination;
        await _storageRepository.StoreArticles(stock);
    }

    public async Task<MultipleRearrangementsResult> ClearUpGroundzone()
    {
        Console.WriteLine("Bodenzone voll! Räume Bodenzone auf");
        var result = new MultipleRearrangementsResult();
        result.GroundzoneGroundzone = await SumUpLowStock();
        result.GroundzoneGroundzone.Add(await CondenseGroundzone());
        result.HighzoneGroundzone = await RearrangeMostCommonArticlesToHighzone();
        return result;
    }

    public async Task<RearrangementsResult> CondenseGroundzone()
    {
        var result = new RearrangementsResult();
        var platzIdsToCondense = _warehouseRepository.GetPlatzIdsWithLowFillratio();

        var allStockEntries = new List<StockBooking>();
        foreach (var stock in platzIdsToCondense.Select(platzId => _stockRepository.GetStock(platzId)))
        {
            foreach (var stockEntry in stock) //TODO alle Positionen zusammenfassen und dann über die Artikel iterieren
            {
                allStockEntries.Add(stockEntry);
                var storagePlaces = _stockRepository.GetStoragePlacesInGroundzoneToArticle(new Article
                {
                    Artikelnummer = stockEntry.Artikelnummer,
                    Variante = stockEntry.Variante
                });
                if (storagePlaces.Count == 1) break;

                var subsets = GetSubsetsToRearrange(storagePlaces);
                var rearrangements = await RearrangeSubsets(subsets, stockEntry);
                result.Count += rearrangements.Count;
                result.Length += rearrangements.Length;
            }
        }

        var articlesWithLowStock = allStockEntries.DistinctBy(x => $"{x.Artikelnummer}_{x.Variante}").ToList();

        foreach (var article in articlesWithLowStock)
        {
            var storagePlaces = _stockRepository.GetStoragePlacesInGroundzoneToArticle(new Article
            {
                Artikelnummer = article.Artikelnummer,
                Variante = article.Variante
            });
            if (storagePlaces.Count == 1) break;

            var subsets = GetSubsetsToRearrange(storagePlaces);
            var rearrangements = await RearrangeSubsets(subsets, article);
            result.Count += rearrangements.Count;
            result.Length += rearrangements.Length;
        }

        return result;
    }

    private List<List<StoragePlace>> GetSubsetsToRearrange(List<StoragePlace> storagePlaces)
    {
        var result = new List<List<StoragePlace>>();
        var subset = new List<StoragePlace>();
        foreach (var storagePlace in storagePlaces)
        {
            if (subset.Sum(x => x.Fillratio) + storagePlace.Fillratio <= 1)
            {
                subset.Add(storagePlace);
            }
            else
            {
                result.Add(subset);
                subset = new List<StoragePlace>();
            }
        }

        if (subset.Count > 0)
        {
            result.Add(subset);
        }
        return result;
    }

    private async Task<RearrangementsResult> RearrangeSubsets(List<List<StoragePlace>> subsets, StockBooking stockBooking)
    {
        var result = new RearrangementsResult();
        foreach (var subset in subsets)
        {
            var destination = subset.MaxBy(x => x.Fillratio)!;
            subset.Remove(destination);
            foreach (var storagePlace in subset)
            {
                stockBooking.PlatzId = storagePlace.PlatzId;
                await _storageRepository.RemoveArticles(stockBooking);
                stockBooking.PlatzId = destination.PlatzId;
                await _storageRepository.StoreArticles(stockBooking);

                result.Count++;
                result.Length += _pathManager.GetDistanceBetweenTwoStoragePlaces(storagePlace.PlatzId, destination.PlatzId);
            }
        }

        return result;
    }

    private async Task<RearrangementsResult> RearrangeMostCommonArticlesToHighzone()
    {
        var result = new RearrangementsResult();
        var articles = _articleRepository.GetArticlesWithMultipleGroundzoneStorageplaces();
        var mixedArticleStoragePlaces = _warehouseRepository.GetAllMixedArticleStoragePlaces();

        foreach (var storagePlaces in articles.Select(article => _stockRepository.GetStoragePlacesInGroundzoneToArticle(article)))
        {
            storagePlaces.RemoveAll(x => mixedArticleStoragePlaces.Contains(x.PlatzId));
            if (storagePlaces.Count > 0) storagePlaces.RemoveAt(storagePlaces.Count - 1);

            foreach (var stock in storagePlaces.Select(storagePlace => _stockRepository.GetStock(storagePlace.PlatzId)))
            {
                foreach (var stockEntry in stock)
                {
                    var originPlatzId = stockEntry.PlatzId;
                    await _storageRepository.RemoveArticles(stockEntry);
                    var newStoragePlace = _pathManager.GetNextStoragePlaceByDistance(stockEntry.PlatzId, false);
                    stockEntry.PlatzId = _warehouseRepository
                        .GetStoragePlaceIdsWithoutStock(newStoragePlace ?? throw new InvalidOperationException(), false).First();
                    await _storageRepository.StoreArticles(stockEntry);

                    result.Count++;
                    result.Length += _pathManager.GetDistanceBetweenTwoStoragePlaces(originPlatzId, stockEntry.PlatzId);
                }

                if (_stockRepository.GetTakenStorageplaceRateInGroundzone() < _configuration.GetValue<float>("FillRatioGroundZone")) return result;
            }
        }
        return result;
    }
}
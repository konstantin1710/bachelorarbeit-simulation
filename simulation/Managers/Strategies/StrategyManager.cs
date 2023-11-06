using System.Collections.Concurrent;
using simulation.Managers.Path;
using simulation.Managers.Stock;
using simulation.Models;
using simulation.Repository.Articles;
using simulation.Repository.Stock;
using simulation.Repository.Storage;
using simulation.Repository.Warehouse;

namespace simulation.Managers.Strategies;

public class StrategyManager : IStrategyManager
{
    private readonly IConfiguration _configuration;
    private readonly IArticleRepository _articleRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockManager _stockManager;
    private readonly IPathManager _pathManager;

    public StrategyManager(IConfiguration configuration,  IArticleRepository articleRepository,
        IStockRepository stockRepository, IStorageRepository storageRepository,
        IWarehouseRepository warehouseRepository, IStockManager stockManager,
        IPathManager pathManager)
    {
        _configuration = configuration;
        _articleRepository = articleRepository;
        _stockRepository = stockRepository;
        _storageRepository = storageRepository;
        _warehouseRepository = warehouseRepository;
        _stockManager = stockManager;
        _pathManager = pathManager;
        _salesRankStatus = new SalesRankStatus();
    }

    private readonly SalesRankStatus _salesRankStatus;
    private bool _classesSet;

    public async Task<MultipleRearrangementsResult> Current(DateTime date)
    {
        var result = new MultipleRearrangementsResult();
        var zugaenge = _stockRepository.GetZugaengeAtSpecificDate(date);

        foreach (var zugang in zugaenge)
        {
            var stock = _stockRepository.GetStock(zugang.PlatzId);

            if (zugang.Menge <= _configuration.GetValue<int>("LowStockThreshold"))
            {
                zugang.PlatzId = _warehouseRepository.GetAllMixedArticleStoragePlaces().First();
            }
            else if (stock.Count > 0)
            {
                var groundZone = _warehouseRepository.IsBodenzone(zugang.PlatzId);
                var newStoragePlace = _pathManager.GetNextStoragePlaceByDistance(zugang.PlatzId, groundZone);
                if (newStoragePlace == null)
                {
                    result.Add(await _stockManager.ClearUpGroundzone());
                    newStoragePlace = _pathManager.GetNextStoragePlaceByDistance(zugang.PlatzId, groundZone);
                }
                zugang.PlatzId = _warehouseRepository.GetStoragePlaceIdsWithoutStock(newStoragePlace!, groundZone).First();
            }

            await _storageRepository.StoreArticles(zugang.ToStockBooking());
        }
        return result;
    }

    public async Task Random(DateTime date)
    {
        var random = new Random();
        var platzIds = _warehouseRepository.GetPlatzIdsWithoutStock();
        var zugaenge = _stockRepository.GetZugaengeAtSpecificDate(date);

        foreach(var zugang in zugaenge)
        {
            var index = random.Next(platzIds.Count);
            zugang.PlatzId = platzIds[index];

            if (zugang.Menge <= _configuration.GetValue<int>("LowStockThreshold"))
            {
                zugang.PlatzId = _warehouseRepository.GetAllMixedArticleStoragePlaces().First();
            }

            await _storageRepository.StoreArticles(zugang.ToStockBooking());
            platzIds.RemoveAt(index);
        }
    }

    public async Task RandomWithPreferredGroundZone(DateTime date)
    {
        var random = new Random();
        var platzIdsInGroundZone = _warehouseRepository.GetPlatzIdsWithoutStockInGroundZone();
        var zugaenge = _stockRepository.GetZugaengeAtSpecificDate(date);
        List<int> allPlatzIds;

        foreach (var zugang in zugaenge)
        {
            if (platzIdsInGroundZone.Count > 0)
            {
                var index = random.Next(platzIdsInGroundZone.Count);
                zugang.PlatzId = platzIdsInGroundZone[index];

                if (zugang.Menge <= _configuration.GetValue<int>("LowStockThreshold"))
                {
                    zugang.PlatzId = _warehouseRepository.GetAllMixedArticleStoragePlaces().First();
                }

                await _storageRepository.StoreArticles(zugang.ToStockBooking());
                platzIdsInGroundZone.RemoveAt(index);
            }
            else
            {
                allPlatzIds = _warehouseRepository.GetPlatzIdsWithoutStock();
                var index = random.Next(allPlatzIds.Count);

                zugang.PlatzId = allPlatzIds[index];

                if (zugang.Menge <= _configuration.GetValue<int>("LowStockThreshold"))
                {
                    zugang.PlatzId = _warehouseRepository.GetAllMixedArticleStoragePlaces().First();
                }

                await _storageRepository.StoreArticles(zugang.ToStockBooking());
                allPlatzIds.RemoveAt(index);
            }
        }
    }

    public async Task PreferredLowDistance(DateTime date)
    {
        var zugaenge = _stockRepository.GetZugaengeAtSpecificDate(date);
        var storagePlaces = _warehouseRepository.GetPlatzIdsWithoutStockOrderedByDistance();

        foreach (var zugang in zugaenge)
        {
            zugang.PlatzId = storagePlaces[0];

            if (zugang.Menge <= _configuration.GetValue<int>("LowStockThreshold"))
            {
                zugang.PlatzId = _warehouseRepository.GetAllMixedArticleStoragePlaces().First();
            }

            await _storageRepository.StoreArticles(zugang.ToStockBooking());
            storagePlaces.RemoveAt(0);
        }
    }

    public async Task DistanceBySalesRank(DateTime date, bool optimizedGroundzone, bool exactForecast)
    {
        if (date.Month != _salesRankStatus.Date.Month)
        {
            await SetSalesRanks(date, exactForecast);
            await SetStoragePlaceClassesForSalesrank();
        }

        var zugaenge = _stockRepository.GetZugaengeAtSpecificDate(date);
        foreach (var zugang in zugaenge)
        {
            if (zugang.Menge <= _configuration.GetValue<int>("LowStockThreshold"))
            {
                zugang.PlatzId = _warehouseRepository.GetAllMixedArticleStoragePlaces().First();
            }
            else
            {
                int articleClass;
                if (zugang.Rank == 0 || double.IsPositiveInfinity(zugang.Rank))
                {
                    articleClass = _warehouseRepository.GetMaximumClass();
                }
                else
                {
                    articleClass = (int)zugang.Rank;
                }
                zugang.PlatzId = optimizedGroundzone ? GetOptimizedPlatzIdInClass(articleClass, zugang.ToArticle(), date, exactForecast) : GetPlatzIdInClass(articleClass, true);
            }
            await _storageRepository.StoreArticles(zugang.ToStockBooking());
        }

        _salesRankStatus.Date = date;
    }

    private int GetPlatzIdInClass(int articleClass, bool salesrank = false)
    {
        var random = new Random();
        int result;
        var storagePlacesInClass = _warehouseRepository.GetStoragePlacesToClass(articleClass);
        if (storagePlacesInClass.Count == 0)
        {
            var platzId = _warehouseRepository.GetNextPlatzIdInLowerClass(articleClass);
            if (platzId == 0)
            {
                platzId = _warehouseRepository.GetNextPlatzIdInHigherClass(articleClass);
            }
            result = platzId;
        }
        else
        {
            result = salesrank? storagePlacesInClass.First() : storagePlacesInClass[random.Next(storagePlacesInClass.Count)];
        }

        return result;
    }

    private int GetOptimizedPlatzIdInClass(int articleClass, Article article, DateTime date, bool exactForecast)
    {
        int result;
        var groundZone = !HasEnoughStockInGroundzone(article, date, exactForecast);
        var storagePlacesInClass = _warehouseRepository.GetStoragePlacesToClass(articleClass, groundZone);
        if (storagePlacesInClass.Count == 0)
        {
            var platzId = _warehouseRepository.GetNextPlatzIdInLowerClass(articleClass, groundZone);
            if (platzId == 0)
            {
                platzId = _warehouseRepository.GetNextPlatzIdInHigherClass(articleClass, groundZone);
            }
            result = platzId;
        }
        else
        {
            result = storagePlacesInClass.First();
        }

        return result;
    }

    private bool HasEnoughStockInGroundzone(Article article, DateTime date, bool exactForecast)
    {
        var stockInGroundZone = _stockRepository.GetStockInGroundZone(article);
        var expectedSalesNumber = exactForecast ? _articleRepository.GetExactSalesNumber(article, date) : _articleRepository.GetExpectedSalesNumber(article, date);
        return stockInGroundZone > expectedSalesNumber;
    }

    private async Task SetStoragePlaceClassesForSalesrank()
    {
        await _warehouseRepository.DeleteStoragePlaceClasses();
        var articleWithRankCount = _articleRepository.GetArticleWithRankCount();
        var totalArticleCount = _articleRepository.GetArticleCount();
        var storagePlaceCount = _warehouseRepository.GetStoragePlaceCount();
        var classSize = Math.Round(storagePlaceCount / (float)totalArticleCount);

        var storagePlacesByDistance = _warehouseRepository.GetStoragePlacesByDistance();
        var counter = 0;
        var groundZoneIncluded = false;
        var classToSet = 1;
        foreach (var storagePlace in storagePlacesByDistance)
        {
            if (storagePlace.IsBodenzone == 1)
            {
                groundZoneIncluded = true;
            }

            storagePlace.StoragePlaceClass = classToSet;
            counter++;
            if (counter >= classSize && groundZoneIncluded)
            {
                counter = 0;
                groundZoneIncluded = false;
                classToSet++;
            }

            if (classToSet == articleWithRankCount + 1)
            {
                break;
            }
        }

        var tasks = new ConcurrentBag<Task>();
        var storagePlacesChunks = storagePlacesByDistance.Where(x => x.StoragePlaceClass > 0).Chunk(_configuration.GetValue<int>("ChunkSize"));
        foreach (var chunk in storagePlacesChunks)
        {
            Parallel.ForEach(chunk, storagePlace =>
            {
                tasks.Add(_warehouseRepository.SetClassForStoragePlace(storagePlace.PlatzId, storagePlace.StoragePlaceClass));
            });
            await Task.WhenAll(tasks);
        }

        var leftStoragePlaces = _warehouseRepository.GetStoragePlacesByDistance();
        var newTasks = new ConcurrentBag<Task>();
        var leftStoragePlacesChunks = leftStoragePlaces.Chunk(_configuration.GetValue<int>("ChunkSize"));
        foreach (var chunk in leftStoragePlacesChunks)
        {
            Parallel.ForEach(chunk,
                storagePlace => { newTasks.Add(_warehouseRepository.SetClassForStoragePlace(storagePlace.PlatzId, articleWithRankCount+1)); });
            await Task.WhenAll(newTasks);
        }
    }

    private async Task SetSalesRanks(DateTime date, bool exactForecast)
    {
        await _articleRepository.DeleteSalesRanks();
        var articlesBySalesrank = exactForecast ?
            _articleRepository.GetExactSalesRank(date.Month, date.Year) :
            _articleRepository.GetSalesRanks(date.Month, date);

        var tasks = new ConcurrentBag<Task>();
        Parallel.For(0, articlesBySalesrank.Count,
            i => { tasks.Add(_articleRepository.SetSalesRank(articlesBySalesrank[i], i + 1)); });
        await Task.WhenAll(tasks);
    }

    public async Task Classes(DateTime date, int numberOfClasses = 2)
    {
        if (!_classesSet)
        {
            await SetStoragePlaceClasses(numberOfClasses);
            _classesSet = true;
        }
        if (date.Month != _salesRankStatus.Date.Month)
        {
            await SetSalesRanks(date, false);
            await SetArticleClasses(numberOfClasses);
            _salesRankStatus.Date = date;
        }

        var zugaenge = _stockRepository.GetZugaengeAtSpecificDate(date);
        foreach (var zugang in zugaenge)
        {
            if (zugang.Menge <= _configuration.GetValue<int>("LowStockThreshold"))
            {
                zugang.PlatzId = _warehouseRepository.GetAllMixedArticleStoragePlaces().First();
            }
            else
            {
                var articleClass = _articleRepository.GetClassToArticle(zugang.ToArticle());
                if (articleClass == 0)
                {
                    articleClass = _warehouseRepository.GetMaximumClass();
                }
                zugang.PlatzId = zugang.PlatzId = GetPlatzIdInClass(articleClass);
            }
            await _storageRepository.StoreArticles(zugang.ToStockBooking());
        }
    }

    private async Task SetStoragePlaceClasses(int numberOfClasses)
    {
        await _warehouseRepository.DeleteStoragePlaceClasses();
        var storagePlaceCount = _warehouseRepository.GetStoragePlaceCount();
        var divider = _configuration.GetSection($"Classes:{numberOfClasses}").Get<List<float>>();

        for (var i = 0; i < numberOfClasses; i++)
        {
            var limit = (int)Math.Round(storagePlaceCount * (i < divider.Count ? divider[i] : 1) -
                                        storagePlaceCount * (i - 1 >= 0 ? divider[i - 1] : 0));
            limit = i < divider.Count ? limit : storagePlaceCount;
            var offset = (int)Math.Round(storagePlaceCount * (i - 1 >= 0 ? divider[i - 1] : 0));
            var storagePlacesInClass = _warehouseRepository.GetStoragePlacesToClass(limit, offset);

            var tasks = new ConcurrentBag<Task>();
            var storagePlacesChunks = storagePlacesInClass.Chunk(_configuration.GetValue<int>("ChunkSize"));
            foreach (var chunk in storagePlacesChunks)
            {
                var classToSet = i + 1;
                Parallel.ForEach(chunk,
                    storagePlace => { tasks.Add(_warehouseRepository.SetClassForStoragePlace(storagePlace, classToSet)); });
                await Task.WhenAll(tasks);
            }
        }
    }

    private async Task SetArticleClasses(int numberOfClasses)
    {
        await _articleRepository.DeleteArticleClasses();
        var articleCount = _articleRepository.GetArticleCount();
        var divider = _configuration.GetSection($"Classes:{numberOfClasses}").Get<List<float>>();

        for (var i = 0; i < numberOfClasses; i++)
        {
            var limit = (int)Math.Round(articleCount * (i < divider.Count ? divider[i] : 1) -
                                        articleCount * (i - 1 >= 0 ? divider[i - 1] : 0));
            limit = i < divider.Count ? limit : articleCount;
            var offset = (int)Math.Round(articleCount * (i - 1 >= 0 ? divider[i - 1] : 0));
            var articlesInClass = _articleRepository.GetArticlesToClass(limit, offset);

            var tasks = new ConcurrentBag<Task>();
            var articleChunks = articlesInClass.Chunk(_configuration.GetValue<int>("ChunkSize"));
            foreach (var chunk in articleChunks)
            {
                var classToSet = i + 1;
                Parallel.ForEach(chunk,
                    article => { tasks.Add(_articleRepository.SetClassForArticle(article, classToSet)); });
                await Task.WhenAll(tasks);
            }
        }
    }
}
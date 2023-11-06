using System.Collections.Concurrent;
using simulation.Models;
using simulation.Models.Content;
using simulation.Providers.Content;
using simulation.Repository.Articles;

namespace simulation.Managers.Articles;

public class ArticleManager : IArticleManager
{
    private readonly IContentProvider _contentProvider;
    private readonly IArticleRepository _articleRepository;

    public ArticleManager(IContentProvider contentProvider, IArticleRepository articleRepository)
    {
        _contentProvider = contentProvider;
        _articleRepository = articleRepository;
    }
    public async Task<List<ArticleAttributes>> GetArticleAttributes(List<string> articles, bool useContentApi)
    {
        return useContentApi ? await GetArticleAttributesWithContentApi(articles) : GetArticleAttributesWithSage(articles);
    }

    private async Task<List<ArticleAttributes>> GetArticleAttributesWithContentApi(List<string> articles)
    {
        var contentResponse = await _contentProvider.GetArticleAttributes(articles);
        var articleAttributes = MapDimensions(contentResponse);

        return articleAttributes.Select(article => _articleRepository.GetMaxAndMinAreas(article)).ToList();
    }

    private List<ArticleAttributes> GetArticleAttributesWithSage(List<string> articleStrings)
    {
        return articleStrings
            .Select(GetArticleNumberFromString)
            .Select(article => _articleRepository.GetAllDimensionsForArticle(article.Artikelnummer, article.Variante))
            .ToList();
    }

    private static List<ArticleAttributes> MapDimensions(List<ContentResponse> contentResponse)
    {
        return contentResponse.Select(article => new ArticleAttributes
            {
                Length = article.Attributes!.SingleItemPackageLength.First().Value,
                Width = article.Attributes!.SingleItemPackageWidth.First().Value,
                Height = article.Attributes!.SingleItemPackageHeight.First().Value
            })
            .ToList();
    }

    private static Article GetArticleNumberFromString(string articleString)
    {
        var article = articleString.Split('_');
        if (int.TryParse(article[0], out var artikelnummer) && int.TryParse(article[1], out var variante))
        {
            return new Article
            {
                Artikelnummer = artikelnummer,
                Variante = variante
            };
        }

        throw new InvalidDataException($"Die Artikelnummer {articleString} ist nicht gültig.");
    }

    public async Task CalculatePalletSizes()
    {
        var tasks = new ConcurrentBag<Task>();
        var articles = _articleRepository.GetAllArticles();


        Parallel.ForEach(articles, article =>
        {
            article.PalletSize = _articleRepository.GetPalletSize(article);
            tasks.Add(_articleRepository.SetPalletSize(article));
        });

        await Task.WhenAll(tasks);
    }
}
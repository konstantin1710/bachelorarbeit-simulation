using simulation.Models;

namespace simulation.Managers.Articles;

public interface IArticleManager
{
    Task<List<ArticleAttributes>> GetArticleAttributes(List<string> articles, bool useContentApi);
    Task CalculatePalletSizes();
}
using simulation.Models;

namespace simulation.Repository.Articles;

public interface IArticleRepository
{
    public ArticleAttributes GetAllDimensionsForArticle(int artikelnummer, int variante);

    ArticleAttributes GetMaxAndMinAreas(ArticleAttributes article);
    List<Article> GetAllArticles();
    int GetPalletSize(Article article);
    Task SetPalletSize(Article article);
    List<Article> GetArticlesWithMultipleGroundzoneStorageplaces();
    List<Article> GetSalesRanks(int month, DateTime date);
    Task SetSalesRank(Article article, int rank);
    int GetArticleCount();
    List<Article> GetArticlesToClass(int limit, int offset);
    Task SetClassForArticle(Article article, int classToSet);
    int GetClassToArticle(Article article);
    Task DeleteSalesRanks();
    int GetArticleWithRankCount();
    Task DeleteArticleClasses();
    List<Article> GetExactSalesRank(int month, int year);
    int GetExpectedSalesNumber(Article article, DateTime date);
    int GetExactSalesNumber(Article article, DateTime date);
}
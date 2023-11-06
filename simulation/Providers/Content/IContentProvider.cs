using simulation.Models.Content;

namespace simulation.Providers.Content;

public interface IContentProvider
{
    Task<List<ContentResponse>> GetArticleAttributes(List<string> articles);
}
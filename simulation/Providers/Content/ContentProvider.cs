using simulation.Models.Content;

namespace simulation.Providers.Content;

public sealed class ContentProvider : ApiProvider, IContentProvider
{
    public ContentProvider(IConfiguration configuration, IHttpContextAccessor contextAccessor) : base(contextAccessor)
    {
        Url = configuration.GetSection("Urls")["ContentApi"];
    }

    public async Task<List<ContentResponse>> GetArticleAttributes(List<string> articles)
    {
        var queryParameters = new Dictionary<string, string>
        {
            {"articleNumbers", string.Join(',', articles)},
            {"page", "1"},
            {"productsPerPage", articles.Count.ToString()}
        };
        return await GetRestResponse<List<ContentResponse>>("products", queryParameters);
    }
}
using System.Web;
using Newtonsoft.Json;

namespace simulation.Providers;

public abstract class ApiProvider
{
    protected HttpClient Client = null!;
    protected string Url = null!;

    protected ApiProvider(IHttpContextAccessor contextAccessor)
    {
        if (contextAccessor.HttpContext != null)
        {
            var handler = new HttpClientHandler();

            Client = new HttpClient(handler);
            Client.Timeout = Timeout.InfiniteTimeSpan;
        }
    }

    protected async Task<T> GetRestResponse<T>(string endpoint, Dictionary<string, string>? queryParameters = null)
    {
        var uri = BuildUri(endpoint, queryParameters);
        var response = await Client.GetAsync(uri);

        if ((int)response.StatusCode >= 400)
        {
            throw await GetErrorFromHttpResponse(response);
        }

        return await ReadResponse<T>(response);
    }

    private async Task<T> ReadResponse<T>(HttpResponseMessage response)
    {
        if (typeof(T) == typeof(string))
        {
            var test = await response.Content.ReadAsStringAsync();
            return (T)Convert.ChangeType(test, typeof(T));
        }

        var contentStream = await response.Content.ReadAsStreamAsync();
        using var streamReader = new StreamReader(contentStream);
        await using var jsonReader = new JsonTextReader(streamReader);
        var serializer = new JsonSerializer();
        var responseObject = serializer.Deserialize<T>(jsonReader)!;
        return responseObject;
    }

    private Uri BuildUri(string endPoint, Dictionary<string, string>? queryParameters = null)
    {
        var uriBuilder = new UriBuilder(Url);
        uriBuilder.Path += endPoint;
        if (queryParameters != null)
        {
            uriBuilder.Query =
                string.Join("&", queryParameters
                    .Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}")
                );
        }
        return uriBuilder.Uri;
    }

    private async Task<BadHttpRequestException> GetErrorFromHttpResponse(HttpResponseMessage response)
    {
        var contentStream = await response.Content.ReadAsStreamAsync();
        using var streamReader = new StreamReader(contentStream);
        await using var jsonReader = new JsonTextReader(streamReader);

        return new BadHttpRequestException(await response.Content.ReadAsStringAsync(), (int)response.StatusCode);
    }
}
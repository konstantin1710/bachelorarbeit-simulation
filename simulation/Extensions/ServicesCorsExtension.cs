using System.Text.RegularExpressions;

namespace simulation.Extensions;

/// <summary>
/// Herauslösen der CORS-Einstellungen aus der Program.cs
/// </summary>
public static class ServicesCorsExtension
{
    /// <summary>
    /// Konfiguriert die CORS-Policy für die API
    /// </summary>
    /// <param name="services">Services, die konfiguriert werden</param>
    /// <param name="environment">Das ASPNETCORE_ENVIRONMENT</param>
    public static void ConfigureCors(this IServiceCollection services, string environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(
                "Private",
                configurePolicy =>
                {
                    configurePolicy.SetIsOriginAllowed(
                            origin => Regex.IsMatch(
                                origin,
                                GetCorsRegex(environment),
                                RegexOptions.IgnoreCase)).AllowAnyHeader().AllowAnyMethod()
                        .AllowCredentials().WithExposedHeaders("Pagination").WithExposedHeaders("Link");
                });
        });
    }

    private static string GetCorsRegex(string configuration)
    {
        var corsString = @"(http(s)?:\/\/(.+\.)?relaxdays\.(de|local|cloud|on-rcs\.com)(:\d{1,5})?$)";

        if (configuration is "Development" or "Staging")
        {
            corsString += @"|(http(s)?:\/\/(.*\.)?localhost(:\d{1,5})?$)";
        }

        return corsString;
    }
}

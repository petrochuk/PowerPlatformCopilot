using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AP2.DataverseAzureAI.Settings;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AP2.DataverseAzureAI;

public static class DataverseAIClientExtensions
{
    public static IServiceCollection AddDataverseAIClient(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

        services.AddSingleton<DataverseAIClient>();
        services.AddSingleton<WamAuthorizationHeaderHandler>();
        services.AddHttpClient(nameof(DataverseAIClient), c =>
        {
            c.DefaultRequestVersion = HttpVersion.Version30;
        })
        .AddHttpMessageHandler<WamAuthorizationHeaderHandler>();

        services.AddOptions<AzureAISettings>().Bind(configuration.GetSection("AzureAI"));
        services.AddOptions<PowerPlatformSettings>().Bind(configuration.GetSection("PowerPlatform"));
        services.AddSingleton<IAuthenticationProvider, GraphAuthenticationProvider>();
        services.AddSingleton<GraphServiceClient>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }


    /// <summary>
    /// Trims publisher prefix from the enity or attribute name
    /// </summary>
    public static string TrimPublisher(this string name)
    {
        var index = name.IndexOf('_');
        if (index < 0)
            return name;

        return name.Substring(index + 1);
    }
}

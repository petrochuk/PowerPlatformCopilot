using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AP2.DataverseAzureAI.Settings;

namespace AP2.DataverseAzureAI;

public static class DataverseAIClientExtensions
{
    public static IServiceCollection AddDataverseAIClient(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

        services.AddTransient<DataverseAIClient>();
        services.AddTransient<WamAuthorizationHeaderHandler>();
        services.AddHttpClient<DataverseAIClient>(c =>
        {
            c.DefaultRequestVersion = HttpVersion.Version30;
        })
        .AddHttpMessageHandler<WamAuthorizationHeaderHandler>();

        services.AddOptions<AzureAISettings>().Bind(configuration.GetSection("AzureAI"));
        services.AddOptions<PowerPlatformSettings>().Bind(configuration.GetSection("PowerPlatform"));

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

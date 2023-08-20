using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PowerAppGenerator.Settings;
using System.Reflection;

namespace PowerAppGenerator;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
		    builder.Logging.AddDebug();
#endif
        ConfigureServices(builder.Services);
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var config = LoadConfiguration();
        var appSettings = config.GetSection(nameof(AppSettings)).Get<AppSettings>();

        services.AddOptions<AppSettings>().Bind(config.GetSection(nameof(AppSettings)));
    }

    private static IConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

#if DEBUG
        builder.AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true);
#endif

        return builder.Build();
    }

}
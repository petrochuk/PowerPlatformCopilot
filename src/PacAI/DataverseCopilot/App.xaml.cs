using bolt;
using bolt.authentication;
using bolt.authentication.profiles;
using bolt.authentication.store;
using bolt.cli;
using bolt.dataverse;
using bolt.dataverse.client;
using bolt.module.admin;
using bolt.module.auth;
using bolt.system;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DataverseCopilot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly Uri MicrosoftGraph = new Uri("https://graph.microsoft.com");

        public static IServiceProvider ServiceProvider { get; private set; }

        static App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var config = LoadConfiguration();
            var pacAppSettings = config.GetSection(nameof(PacAppSettings)).Get<PacAppSettings>();

            services.AddOptions<PacAppSettings>().Bind(config.GetSection(nameof(PacAppSettings)));
            services.AddSingleton<ILocalizedStrings<LocString>>(_ => new LocalizedStrings<LocString>(CultureInfo.CurrentUICulture, "loc"));
            services.AddSingleton<IAuthTokenStore, AuthTokenStore>();
            services.AddSingleton<IFeatureFlags, FeatureFlags>();
            services.AddSingleton<IAuthModuleCommandFormatter, AuthCommandFormatter>();
            services.AddSingleton<IAuthProfilesManager, TransitoryAuthProfilesManager>();
            services.AddSingleton<IAuthenticatedHttpClient, AuthenticatedHttpClient>();
            services.AddSingleton<IAudienceResolver, AudienceResolver>();
            services.AddSingleton<IAuthorityResolver, AuthorityResolverProvider>();
            services.AddSingleton(new AuthOptions());
            services.AddSingleton(new MsalLoggingOptions
            {
                MinLogLevel = LogLevel.Trace, // Enable Trace, but our NLog config will apply the actual filter
                EnablePiiLogging = true,
            });
            services.AddSingleton<IEnvirons, Environs>();
            services.AddSingleton<IProcessLauncher, ProcessLauncher>();
            services.AddSingleton<IOutputWindow, OutputWindow>();
            services.AddSingleton<IAuthenticatedClientFactory, AuthenticatedClientFactory>();
            services.AddSingleton<IAuthKindDescription, DataverseAuthKindDescription>();
            services.AddSingleton<IAuthKindDescription, UniversalAuthKindDescription>();
            services.AddSingleton<CdsClientConnector>();
            services.AddLazyServiceResolution();

            services.AddSingleton(new ClientApplicationConfiguration(
                clientId: new Guid(pacAppSettings?.AzureAppId!),
                new Uri($"app://{pacAppSettings?.AzureAppId}")));

            services.AddHttpClient(DataverseConstants.HttpClientName, (client) =>
            {
                const string userAgent = "User-Agent";
                client.DefaultRequestHeaders.Remove(userAgent);
                client.DefaultRequestHeaders.Add(userAgent, $"pac/example.0 (win;)");
            });
            services.AddLogging(configure => configure.AddDebug());
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
#if DEBUG
                .AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true);
#else
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
#endif
            return builder.Build();
        }
    }
}

#region usings
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
using DataverseCopilot.Graph;
using DataverseCopilot.TextToSpeech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
#endregion

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
            var appSettings = config.GetSection(nameof(AppSettings)).Get<AppSettings>();

            services.AddOptions<AppSettings>().Bind(config.GetSection(nameof(AppSettings)));
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
                MinLogLevel = Microsoft.Extensions.Logging.LogLevel.Trace, // Enable Trace, but our NLog config will apply the actual filter
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
                new Guid(appSettings?.AzureAppId!),
                new Uri($"app://{appSettings?.AzureAppId}")));

            services.AddSingleton<IAuthenticationProvider, GraphAuthenticationProvider>();
            services.AddSingleton<GraphServiceClient>();

            services.AddHttpClient(DataverseConstants.HttpClientName, (client) =>
            {
                const string userAgent = "User-Agent";
                client.DefaultRequestHeaders.Remove(userAgent);
                client.DefaultRequestHeaders.Add(userAgent, $"pac/example.0 (win;)");
            });
            services.AddLogging(configure => configure.AddDebug());

            services.AddSingleton<ISpeechAssistant, SpeechAssistant>();
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

using bolt.authentication.store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace DataverseCopilot.Graph;

internal class GraphAuthenticationProvider : IAuthenticationProvider
{
    ILoggerFactory _loggerFactory;
    AppSettings _appSettings;
    MsalLoggingOptions _msalLoggingOptions;
    IPublicClientApplication? _publicClientApplication;

    public GraphAuthenticationProvider(
        IOptions<AppSettings> appSettings,
        MsalLoggingOptions msalLoggingOptions,
        ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _appSettings = appSettings.Value;
        _msalLoggingOptions = msalLoggingOptions;
    }

    async Task IAuthenticationProvider.AuthenticateRequestAsync(RequestInformation request, 
        Dictionary<string, object>? additionalAuthenticationContext, 
        CancellationToken cancellationToken)
    {
        if (_publicClientApplication == null)
        {
            var appBuilder = PublicClientApplicationBuilder.Create(_appSettings.AzureAppIdForGraph);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                appBuilder.WithParentActivityOrWindow(GetMainWindowHandle);
                appBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
            }

            _publicClientApplication = appBuilder.Build();
        }

        var result = await _publicClientApplication.AcquireTokenSilent(
            new string[] { "https://graph.microsoft.com//.default" }, 
            PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false);

        request.Headers.Add(
            "Authorization",
            new string[] { $"Bearer {result.AccessToken}" });
    }

    public static IntPtr GetMainWindowHandle()
    {
        return new WindowInteropHelper(App.Current.MainWindow).Handle;
    }
}

using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using AP2.DataverseAzureAI.Native;
using AP2.DataverseAzureAI.Settings;

namespace AP2.DataverseAzureAI;

public class WamAuthorizationHeaderHandler : DelegatingHandler
{
    readonly Lazy<IPublicClientApplication> _publicClientApplication;

    public WamAuthorizationHeaderHandler(IOptions<PowerPlatformSettings> settings)
    {
        _publicClientApplication = new Lazy<IPublicClientApplication>(() =>
        {
            var appBuilder = PublicClientApplicationBuilder.Create(settings.Value.AzureAppId.ToString());

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                appBuilder.WithParentActivityOrWindow(NativeMethods.GetProcessMainWindowHandle);
                appBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
            }

            return appBuilder.Build();
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var authority = request.RequestUri!.GetLeftPart(UriPartial.Authority);
        if (authority.EndsWith("api.powerplatform.com", StringComparison.OrdinalIgnoreCase))
            authority = "https://api.powerplatform.com";
        else if (authority.EndsWith("blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
            authority = null;

        if (authority != null)
        {
            var result = await _publicClientApplication.Value.AcquireTokenSilent(
                new string[] { $"{authority}//.default" },
                PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false);

            request.Headers.Add("Authorization", new string[] { $"Bearer {result.AccessToken}" });
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

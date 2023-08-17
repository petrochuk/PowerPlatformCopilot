using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Broker;
using AP2.DataverseAzureAI.Settings;

namespace AP2.DataverseAzureAI;

internal class GraphAuthenticationProvider : IAuthenticationProvider
{
	IPublicClientApplication? _publicClientApplication;
    private readonly IOptions<PowerPlatformSettings> _powerPlatformSettings;

    public GraphAuthenticationProvider(IOptions<PowerPlatformSettings> powerPlatformSettings)
	{
        _powerPlatformSettings = powerPlatformSettings ?? throw new ArgumentNullException(nameof(powerPlatformSettings));
	}

	async Task IAuthenticationProvider.AuthenticateRequestAsync(RequestInformation request,
		Dictionary<string, object>? additionalAuthenticationContext,
		CancellationToken cancellationToken)
	{
		if (_publicClientApplication == null)
		{
			var appBuilder = PublicClientApplicationBuilder.Create(_powerPlatformSettings.Value.GraphAppId.ToString());

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				appBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
			}

			_publicClientApplication = appBuilder.Build();
		}

		var result = await _publicClientApplication.AcquireTokenSilent(
			new string[] { "https://graph.microsoft.com//.default" },
			PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false);

		request.Headers.Add("Authorization", new string[] { $"Bearer {result.AccessToken}" });
	}
}

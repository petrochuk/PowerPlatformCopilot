using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AP2.DataverseAzureAI;

namespace ConsoleTestApp;

#pragma warning disable CA1303 // Do not pass literals as localized parameters

internal class Program
{
    const string DataverseAIAssistantName = "Dataverse AI Assistant";
    private static IHost? _host;

    private async Task Run(string[] args)
    {
        var testAppSettings = _host!.Services.GetRequiredService<IOptions<TestAppSettings>>();
        var client = _host!.Services.GetRequiredService<DataverseAIClient>();

        WriteLine(ConsoleColor.DarkGray, "Connecting to the environment");
        client.EnvironmentId = testAppSettings.Value.EnvironmentId;
        await client.LoadMetadata().ConfigureAwait(false);
        WriteLine(ConsoleColor.DarkGray, $"Connected to {client.EnvironmentInstance.FriendlyName}");
        Console.WriteLine();

        var prompt = GetPrompt(args, client);
        while (!string.IsNullOrWhiteSpace(prompt))
        {
            var chatCompletion = await client.GetChatCompletionAsync(prompt).ConfigureAwait(false);

            Console.WriteLine();
            WriteLine(ConsoleColor.DarkCyan, $"{DataverseAIAssistantName}:");
            WriteLine(ConsoleColor.Cyan, chatCompletion);

            Console.WriteLine();
            WriteLine(ConsoleColor.Gray, $"{(client.GivenName != null ? client.GivenName : "User")}:");
            prompt = Console.ReadLine();
        }
    }

    private string GetPrompt(string[] args, DataverseAIClient client)
    {
        WriteLine(ConsoleColor.Gray, $"{(client.GivenName != null ? client.GivenName : "User")}:");

        // Get prompt from command line or arguments
        string? prompt;
        if (args.Length <= 0)
        {
            prompt = Console.ReadLine();
        }
        else
        {
            prompt = args[0];
            Console.WriteLine($"{prompt}");
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt is empty.");
        }

        return prompt;
    }

    #region Configuration

    static async Task Main(string[] args)
    {
        try
        {
            var configuration = LoadConfiguration();
            _host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder =>
                {
                    builder.Sources.Clear();
                    builder.AddConfiguration(configuration);
                })
                .ConfigureLogging((builder, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                })
                .ConfigureServices(ConfigureServices)
                .ConfigureServices(services => services.AddSingleton<Program>())
            .Build();

            await _host.Services.GetRequiredService<Program>().Run(args).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Console.ResetColor();
        }
    }

    private void WriteLine(ConsoleColor consoleColor, string message)
    {
        Console.ForegroundColor = consoleColor;
        Console.WriteLine(message);
        Console.ForegroundColor = ConsoleColor.White;
    }

    private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddDataverseAIClient(hostContext.Configuration);
        services.AddOptions<TestAppSettings>().Bind(hostContext.Configuration.GetSection("TestApp"));
    }

    private static IConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

#if DEBUG
        builder.AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true);
#endif

        return builder.Build();
    }

    #endregion
}

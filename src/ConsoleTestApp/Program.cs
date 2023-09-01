using AP2.DataverseAzureAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace ConsoleTestApp;

#pragma warning disable CA1303 // Do not pass literals as localized parameters

internal class Program
{
    const string DataverseAIAssistantName = "ProDev Copilot";
    private static IHost? _host;

    private async Task Run(string[] args)
    {
        /* Save canvas demo
        _demo.Add("which canvas apps do I have?");
        _demo.Add("how do I export one of them as msapp file from Power Apps Maker Studio?");
        _demo.Add("Too many steps, just save first one to SavedApps");
        */
        /* Roles
        _demo.Add("give Person 1 a maker role in DevTools");
        _demo.Add("and Person 2 too");
        _demo.Add("and make Person 3 customizer");
        _demo.Add("Person 4 can be sys admin in ProDev");
        */
        /* Email and Share
        _demo.Add("which canvas apps were modified this month?");
        _demo.Add("and in Env1 environment?");
        _demo.Add("email me this list");
        _demo.Add("Share first three with Person 5");
        */
                
        /* Email and Share
        _demo.Add("which canvas apps I modified this week?");
        _demo.Add("How can I deploy 'Equipment Request App' to different environment? I heard I need to use solutions.");
        _demo.Add("Which solutions do I have here?");
        _demo.Add("Which ones are unmanaged?");
        _demo.Add("who created second solution?");
        _demo.Add("What's inside it?");
        _demo.Add("Just create new solution named Power Apps Demo Fest");
        _demo.Add("Add 'Equipment Request App' to it");
        _demo.Add("Export the solution to SavedApps");
        _demo.Add("Email my boss I am finished");
        */

        using var client = _host!.Services.GetRequiredService<DataverseAIClient>();

        WriteLine(ConsoleColor.Magenta, client.WelcomeMessage);
        client.Run();
        Console.WriteLine();

        var prompt = GetPrompt(args, client);
        while (!string.IsNullOrWhiteSpace(prompt))
        {
            var chatCompletion = await client.GetChatCompletionAsync(prompt).ConfigureAwait(false);

            Console.WriteLine();
            WriteLine(ConsoleColor.DarkCyan, $"{DataverseAIAssistantName}:");
            WriteLine(ConsoleColor.Cyan, chatCompletion);

            Console.WriteLine();
            prompt = GetPrompt(args, client);
        }
    }

    private List<string> _demo = new List<string>();
    Random _random = new Random();

    private string GetPrompt(string[] args, DataverseAIClient client)
    {
        WriteLine(ConsoleColor.Gray, $"{(client.GivenName != null ? client.GivenName : "User")}:");

        // Get prompt from command line or arguments
        string prompt;
        if (args.Length <= 0)
        {
            var nextPrompt = _demo.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(nextPrompt))
            {
                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Enter)
                {
                    prompt = Console.ReadLine() ?? string.Empty;
                    return key.KeyChar + prompt;
                }
                _demo.RemoveAt(0);
                foreach(var character in nextPrompt)
                {
                    Console.Write(character);
                    if (character == ',')
                        Thread.Sleep(500);
                    else
                        Thread.Sleep(_random.Next(50, 200));
                }
                Console.WriteLine();
                return nextPrompt;
            }
            return Console.ReadLine() ?? string.Empty;
        }
        else
        {
            prompt = args[0];
            Console.WriteLine($"{prompt}");
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
        var userAppSettings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DataverseAIClient.LocalAppDataFolderName, "appSettings.json");
        if (!File.Exists(userAppSettings))
        {
            if (!Directory.Exists(Path.GetDirectoryName(userAppSettings)))
                Directory.CreateDirectory(Path.GetDirectoryName(userAppSettings)!);

            var httpClient = new HttpClient();
            var responseStream = httpClient.GetStreamAsync("https://ap2public.blob.core.windows.net/oaipublic/appSettings.json").Result;
            using var fileStream = new FileStream(userAppSettings, FileMode.Create);
            responseStream.CopyTo(fileStream);
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(userAppSettings, optional: true, reloadOnChange: true);

#if DEBUG
        builder.AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true);
#endif

        return builder.Build();
    }

    #endregion
}

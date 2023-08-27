using System.Text.Json;
using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Settings;

public class UserSettings
{
    public string? LastUsedEnvironmentId { get; set; }
    public string? GivenName { get; set; }
    public string? DisplayName { get; set; }
    public string? EntraId { get; set; }
    public string? UserPrincipalName { get; set; }

    [JsonIgnore]
    public string FileName { get; private set; } = string.Empty;

    public UserSettings()
    {
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FileName, json);
    }

    public static UserSettings Load(string folderName)
    {
        _ = folderName ?? throw new ArgumentNullException(nameof(folderName));

        var settingsFile = Path.Combine(folderName, $"{nameof(UserSettings)}.json");
        UserSettings? settings;
        if (File.Exists(settingsFile))
        {
            var json = File.ReadAllText(settingsFile);
            settings = JsonSerializer.Deserialize<UserSettings>(json);
            if (settings == null)
                throw new InvalidOperationException($"Unable to deserialize {nameof(UserSettings)} from {settingsFile}");
        }
        else
        {
            settings = new UserSettings();
        }
        settings.FileName = settingsFile;

        return settings;
    }
}

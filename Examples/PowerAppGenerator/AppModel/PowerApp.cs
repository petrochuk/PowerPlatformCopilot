using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerAppGenerator.AppModel;

internal class PowerApp
{
    List<Screen> _screens = new();
    JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public void Add(IList<Screen> screens)
    {
        if (screens is null)
            return;

        _screens.AddRange(screens);
    }

    public void Add(Screen screen)
    {
        if (screen is null)
            return;

        _screens.Add(screen);
    }

    public void SaveAs(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        var msappFileInfo = MakeUnique(fileName);
        
        using (var z = ZipFile.Open(msappFileInfo.FullName, ZipArchiveMode.Create))
        {
            // Add all files from template
            var appTemplates = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "AppTemplates");
            foreach (var filePath in Directory.GetFiles(appTemplates, "*.*", SearchOption.AllDirectories))
            {
                var entryName = filePath.Substring(appTemplates.Length + 1, filePath.Length - appTemplates.Length - 1);
                z.CreateEntryFromFile(filePath, entryName);
            }

            // Assign unique ids to screens
            var uniqueId = 4;
            foreach (var screen in _screens)
            {
                screen.ControlUniqueId = uniqueId++.ToString();
            }

            // Add all screens
            foreach (var screen in _screens)
            {
                uniqueId = UpdateUniqueIds(screen.Children, uniqueId);

                var zipEntry = z.CreateEntry(Path.Combine("Controls", screen.ControlUniqueId + ".json"));
                using var zipEntryStream = zipEntry.Open();

                JsonSerializer.Serialize(zipEntryStream, new Root() { ControlInfo = screen }, _jsonSerializerOptions);
            }
        }
    }

    private int UpdateUniqueIds(List<ControlInfo> children, int uniqueId)
    {
        foreach (var child in children)
        {
            child.ControlUniqueId = uniqueId++.ToString();
            uniqueId = UpdateUniqueIds(child.Children, uniqueId);
        }

        return uniqueId;
    }

    public FileInfo MakeUnique(string path)
    {
        string dir = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileNameWithoutExtension(path);
        string fileExt = Path.GetExtension(path);

        for (int i = 1; ; ++i)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return new FileInfo(path);

            path = Path.Combine(dir, fileName + " " + i + fileExt);
        }
    }
}

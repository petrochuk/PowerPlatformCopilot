using System.IO;
using System.Text.Json;

namespace DataverseCopilot.Graph;

internal class GreetingHistory
{
    public const int MaxItems = 20;

    public class Item
    {
        public string? Text { get; set; }
    }

    public IList<Item> Items { get; set; } = new List<Item>();

    public static GreetingHistory Load()
    {
        GreetingHistory? collection = null;
        try
        {
            using var openStream = File.OpenRead(Path.Combine(App.DataFolder, $"{nameof(GreetingHistory)}.json"));
            collection = JsonSerializer.Deserialize<GreetingHistory>(openStream);
        }
        catch (IOException)
        {
        }
        catch (JsonException)
        {
        }

        if (collection == null)
            collection = new GreetingHistory();

        return collection;
    }

    public void Add(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var item = new Item { Text = text };
        Items.Add(item);
        Save();
    }

    public void Save()
    {
        try
        {

            while (MaxItems < Items.Count)
                Items.RemoveAt(0);

            using var saveStream = File.Create(Path.Combine(App.DataFolder, $"{nameof(GreetingHistory)}.json"));
            JsonSerializer.Serialize(saveStream, this);
        } catch (IOException) { }
    }
}

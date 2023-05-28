using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Text.Json;

namespace DataverseCopilot;

// The class derived from DynamicObject.
public class DynamicRow : DynamicObject
{
    Dictionary<string, object> _items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, DynamicRow> _nestedItems = new Dictionary<string, DynamicRow>(StringComparer.OrdinalIgnoreCase);

    public DynamicRow(Dictionary<string, object> items)
    {
        foreach (var item in items)
        {
            Add(item.Key, item.Value);
        }
    }
    public DynamicRow(string key, object value)
    {
        Add(key, value);
    }

    public void Add(string key, object value)
    {
        if (!key.Contains('.'))
        {
            _items.Add(key, value);
            return;
        }

        var nestedKey = key.Substring(0, key.IndexOf('.'));
        var nestedName = key.Substring(key.IndexOf('.') + 1);

        if (!_nestedItems.TryGetValue(nestedKey, out var nestedItem))
        {
            nestedItem = new DynamicRow(nestedName, value);
            _nestedItems.Add(nestedKey, nestedItem);
        }
        else
        {
            nestedItem.Add(nestedName, value);
        }
    }

    public int Count
    {
        get => _items.Count;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        result = null;
        if (!_items.TryGetValue(binder.Name, out var resultItem))
        {
            if(_nestedItems.TryGetValue(binder.Name, out var nestedItem))
            {
                result = nestedItem;
                return true;
            }
            return true;
        }

        var jsonElement = (JsonElement)resultItem;
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Number:
                result = jsonElement.GetDecimal();
                break;
            case JsonValueKind.False:
                result = false;
                break;
            case JsonValueKind.True:
                result = true;
                break;
            case JsonValueKind.String:
                result = jsonElement.GetString();
                break;
            default:
                result = jsonElement.GetString();
                break;
        }

        return true;
    }
}
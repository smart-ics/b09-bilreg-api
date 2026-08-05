using System.Text.Json;

namespace Bilreg.Application.Helpers;

internal static class PropertyChangeHelper
{
    public static TResult? CloneObject<TIn, TResult>(TIn source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<TResult>(json);
    }
    
    public static List<ChangeLog>? GetChanges<T>(T oldEntry, T newEntry)
    {
        List<ChangeLog> logs = [];
    
        var oldProperties = oldEntry?.GetType().GetProperties();
        var newProperties = newEntry?.GetType().GetProperties();

        if (oldProperties is null)
            return null;
        
        foreach(var oldProperty in oldProperties)
        {
            var matchingProperty = newProperties?
                .FirstOrDefault(x => x.Name == oldProperty.Name && x.PropertyType == oldProperty.PropertyType);
            
            if (matchingProperty == null)
                continue;
            
            var oldValue = oldProperty.GetValue(oldEntry)?.ToString();
            var newValue = matchingProperty.GetValue(newEntry)?.ToString();
            if (oldValue != newValue)
                logs.Add(new ChangeLog(
                    matchingProperty.Name, 
                    oldProperty.GetValue(oldEntry)?.ToString()!,
                    matchingProperty.GetValue(newEntry)?.ToString()!
                ));
        }

        return logs.Count == 0 ? null : logs;
    }
}

internal class ChangeLog(string PropertyName, string OldValue, string NewValue)
{
    public string PropertyName { get; set; } = PropertyName;
    public string OldValue { get; set; } = OldValue;
    public string NewValue { get; set; } = NewValue;
}
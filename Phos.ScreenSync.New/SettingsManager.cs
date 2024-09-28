using System;
using System.IO;
using System.Text.Json;

namespace Phos.ScreenSync.New;

public class SettingsManager<T> where T : class
{
    private readonly string _filePath;

    public SettingsManager(string fileName)
    {
        _filePath = GetLocalFilePath(fileName);
    }

    private string GetLocalFilePath(string fileName)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, fileName);
    }

    /// <summary>
    /// If the file exists, load and parse the json file
    /// </summary>
    /// <returns></returns>
    public T? LoadSettings() {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<T>(json);
        }

        return null;
    }

    /// <summary>
    /// Saves the settings to a json file
    /// </summary>
    public void SaveSettings(T settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
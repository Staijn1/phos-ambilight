using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Phos.ScreenSync;

public class SettingsManager<T> where T : class
{
    private readonly string _filePath;
    private readonly IConfiguration _configuration;

    public SettingsManager(string fileName)
    {
        _filePath = GetLocalFilePath(fileName);
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
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
            var settings = JsonSerializer.Deserialize<T>(json);

            // Load BattlefieldApiUrl from appsettings.json
            var battlefieldApiUrl = _configuration["BattlefieldApiUrl"];
            if (settings != null && !string.IsNullOrEmpty(battlefieldApiUrl))
            {
                var property = typeof(T).GetProperty("BattlefieldApiUrl");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(settings, battlefieldApiUrl);
                }
            }

            return settings;
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

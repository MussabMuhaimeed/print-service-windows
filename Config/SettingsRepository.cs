using System.Text.Json;
using PrintService.Windows.Models;

namespace PrintService.Windows.Config;

public sealed class SettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly string _settingsPath;
    private AppSettings _settings;

    public SettingsRepository()
    {
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
        _settings = Load();
    }

    public AppSettings Get() => _settings;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _settings = AppSettings.CreateDefault();
                Save(_settings);
                return _settings;
            }

            var json = File.ReadAllText(_settingsPath);
            _settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? AppSettings.CreateDefault();
            return _settings;
        }
        catch
        {
            _settings = AppSettings.CreateDefault();
            return _settings;
        }
    }

    public void Save(AppSettings settings)
    {
        _settings = settings;
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Phos.ScreenSync.New.ViewModels;

public partial class SettingsPageViewModel: ViewModelBase
{
    private readonly SettingsManager<UserSettings> _settingsManager;

    [ObservableProperty]
    private UserSettings _userSettings;

    public SettingsPageViewModel()
    {
        _settingsManager = new SettingsManager<UserSettings>("UserSettings.json");
        _userSettings = _settingsManager.LoadSettings() ?? new UserSettings();
    }

    [RelayCommand]
    private void Save()
    {
        _settingsManager.SaveSettings(_userSettings);
    }
}
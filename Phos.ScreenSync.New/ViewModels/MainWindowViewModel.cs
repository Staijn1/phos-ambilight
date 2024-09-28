using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Phos.ScreenSync.New.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty] 
    private ViewModelBase _currentScreen = new ScreenSyncPageViewModel();

    [RelayCommand]
    public void NavigateToScreenSyncCommand()
    {
        CurrentScreen = new ScreenSyncPageViewModel();
    }

    [RelayCommand]
    public void NavigateToSettings()
    {
        CurrentScreen = new SettingsPageViewModel();
    }
}
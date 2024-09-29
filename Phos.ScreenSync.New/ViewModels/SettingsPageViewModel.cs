using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phos.Connections;
using Phos.Data;

namespace Phos.ScreenSync.New.ViewModels;

public partial class SettingsPageViewModel: ViewModelBase
{
    private readonly SettingsManager<UserSettings> _settingsManager;
    private readonly PhosSocketIOClient _connection;

    [ObservableProperty]
    private UserSettings _userSettings;

    [ObservableProperty]
    private ObservableCollection<Room> _availableRooms = new();

    [ObservableProperty]
    private ObservableCollection<Room> _selectedRooms = new();


    public SettingsPageViewModel(PhosSocketIOClient connection)
    {
        _settingsManager = new SettingsManager<UserSettings>("UserSettings.json");
        _userSettings = _settingsManager.LoadSettings() ?? new UserSettings();
        _connection = connection;

        LoadAvailableRooms();
        
        _userSettings.PropertyChanged += OnUserSettingsChanged;
        _selectedRooms = new ObservableCollection<Room>(_userSettings.SelectedRooms);
    }

    private void OnUserSettingsChanged(object sender, PropertyChangedEventArgs e)
    {
        // Local asynchronous function to handle the connection and loading of available rooms
        async void HandleAutoConnect()
        {
            if (!_connection.IsConnected)
            {
                await _connection.Connect();
            }
        
            LoadAvailableRooms();
        }

        // Perform additional logic when AutoConnect changes to true
        if (e.PropertyName == nameof(UserSettings.AutoConnect) && _userSettings.AutoConnect)
        {
            HandleAutoConnect();
        }
    }


    [RelayCommand]
    private void Save()
    {
        _settingsManager.SaveSettings(_userSettings);
    }
    
    private async void LoadAvailableRooms()
    {
        if (!_connection.IsConnected)
        {
            return;
        }
        
        
        var response = await _connection.SendEvent(PhosSocketMessage.GetNetworkState);
        var networkState = response.GetValue<NetworkState>();
        AvailableRooms = new ObservableCollection<Room>(networkState.Rooms);
    }
}
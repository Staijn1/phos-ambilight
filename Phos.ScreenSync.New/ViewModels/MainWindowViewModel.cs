using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phos.Connections;
using Phos.Data;
using SocketIOClient;
using SocketIOClient.Transport;

namespace Phos.ScreenSync.New.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SettingsManager<UserSettings> _settingsManager;
    private PhosSocketIOClient? _connection;
    
    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty] 
    private ViewModelBase _currentScreen = new ScreenSyncPageViewModel();

    public MainWindowViewModel()
    {
        _settingsManager = new SettingsManager<UserSettings>("userSettings.json");
        var settings = _settingsManager.LoadSettings();

        if (settings is { AutoConnect: true })
        {
            ConnectWebSocket(settings.WebSocketUrl);
        }
    }
    
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
    
    private void ConnectWebSocket(string webSocketUrl)
    {
        var deviceName = "Phos Screensync - " + Environment.MachineName;

        if (string.IsNullOrEmpty(webSocketUrl))
        {
            return;
        }

        _connection = new PhosSocketIOClient(webSocketUrl, new SocketIOOptions
        {
            Transport = TransportProtocol.WebSocket,
            Query = new List<KeyValuePair<string, string>>
            {
                new("deviceName", deviceName),
            }
        });

        _connection.OnConnect += async (sender, args) =>
        {
            IsConnected = true;
            // First register ourselves as a user
            await _connection.SendEvent(PhosSocketMessage.RegisterAsUser);
            var response = await _connection.SendEvent(PhosSocketMessage.GetNetworkState);

            var networkState = response.GetValue<NetworkState>();
            // OnNewNetworkState(networkState);
        };
        
        _connection.OnDisconnect += (sender, args) => { IsConnected = false; };
        _connection.OnDatabaseChange += (sender, response) => { Console.WriteLine("Database change event received"); };
    }
}
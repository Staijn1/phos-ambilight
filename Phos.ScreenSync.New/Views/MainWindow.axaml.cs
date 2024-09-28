using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Phos.Connections;
using Phos.Data;
using SocketIOClient;
using SocketIOClient.Transport;

namespace Phos.ScreenSync.New.Views;

public partial class MainWindow : Window
{
    private readonly UserSettings? _settings;
    private PhosSocketIOClient _connection;

    public MainWindow()
    {
        InitializeComponent();
        SettingsManager<UserSettings> settingsManager = new("userSettings.json");

        // Load the WebSocket URL from the settings
        _settings = settingsManager.LoadSettings();

        if (_settings is { AutoConnect: true })
        {
            ConnectWebSocket();
        }
    }

    private void ConnectWebSocket()
    {
        var deviceName = "Phos Screensync - " + Environment.MachineName;

        if (_settings?.WebSocketUrl == null)
        {
            return;
        }

        _connection = new PhosSocketIOClient(_settings?.WebSocketUrl, new SocketIOOptions
        {
            Transport = TransportProtocol.WebSocket,
            Query = new List<KeyValuePair<string, string>>
            {
                new("deviceName", deviceName),
            }
        });

        _connection.OnConnect += async (sender, args) =>
        {
            // First register ourselves as a user
            await _connection.SendEvent(PhosSocketMessage.RegisterAsUser);
            var response = await _connection.SendEvent(PhosSocketMessage.GetNetworkState);

            var networkState = response.GetValue<NetworkState>();
            // OnNewNetworkState(networkState);
        };
        _connection.OnDatabaseChange += (sender, response) => { Console.WriteLine("Database change event received"); };
    }
}
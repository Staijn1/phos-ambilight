using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phos.Connections;
using Phos.Data;
using Phos.Screencapture;
using Phos.ScreenSync.New.Views;
using ScreenCapture.NET;

namespace Phos.ScreenSync.New.ViewModels;

public partial class ScreenSyncPageViewModel: ViewModelBase
{
    private readonly PhosScreenCapture _screenCapture;
    private Task? screenCaptureThread;
    private Display _selectedDisplay;
    private PhosSocketIOClient _connection;
    
    [ObservableProperty]
    private bool _hasCustomAreaSelected = false;

    [ObservableProperty]
    private bool _isCapturing = false;

    [ObservableProperty] 
    private BitmapSource? _capturedImage;

    private readonly List<Room> _selectedRooms;


    public ScreenSyncPageViewModel(PhosSocketIOClient connection)
    {
        var settingsManager = new SettingsManager<UserSettings>("userSettings.json");
        _connection = connection;
        _screenCapture = new PhosScreenCapture();
        _selectedDisplay = _screenCapture.GetDisplays().First();
        _screenCapture.SelectDisplay(_selectedDisplay);

        _selectedRooms = settingsManager.LoadSettings()?.SelectedRooms ?? new List<Room>();
    }
    
    [RelayCommand]
    private void OpenSelectAreaWindow()
    {
        var window = new SelectAreaWindow();
        window.AreaSelected += (x, y, w, h) =>
        {
            _screenCapture.CreateCaptureZone(x, y, w, h);
            window.Close();
            HasCustomAreaSelected = true;
        };
        window.Show();
    }
    
    public void ToggleScreenCapture()
    {
        if (IsCapturing)
        {
            // Stop capturing
            IsCapturing = false;
            // Stop capture thread
            screenCaptureThread?.Wait();
            screenCaptureThread = null;
            Console.WriteLine("Stopped capturing.");
        }
        else
        {
            Console.WriteLine("Starting capture");
            // Start capturing
            IsCapturing = true;

            if (!HasCustomAreaSelected)
            {
                _screenCapture.CreateCaptureZone(0, 0, _selectedDisplay.Width, _selectedDisplay.Height);
            }

            // Start the screen capture on a new thread
          StartScreenCapture();
        }
    }
    
    private async void StartScreenCapture()
    {
        var newState = await PrepareSelectedRoomsForScreenSync();


        while (IsCapturing)
        {
            Console.WriteLine("Capturing...");
            var averageColor = _screenCapture.GetAverageColorInArea();
            var colors = newState.Colors;
            colors[0] = ColorUtils.ColorRGBToHex(averageColor);
            newState.Colors = colors;
            await _connection.SendEvent(PhosSocketMessage.SetNetworkState, _selectedRooms.Select(r => r.Id).ToList(), newState);

            // Update the Image control on the UI thread
            var bitmap = _screenCapture.GetImageAsBitmap();
            CapturedImage = bitmap;
        }
    }

    private async Task<State> PrepareSelectedRoomsForScreenSync()
    {
        var colors = new List<string> { "#000000", "#000000", "#000000" };
        // Set the mode for all rooms to the visualizer mode with FFT 255 (max)
        var newState = new State
        {
            Mode = 0,
            Colors = colors,
            Brightness = 255,
            Speed = 2000
        };
        await _connection.SendEvent(PhosSocketMessage.SetNetworkState, _selectedRooms.Select(r => r.Id).ToList(), newState);
        return newState;
    }
}
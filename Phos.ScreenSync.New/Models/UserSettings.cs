using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Phos.Data;

namespace Phos.ScreenSync;

public partial class UserSettings: ObservableObject
{
    [ObservableProperty]
    private bool _autoConnect = false;
    public string WebSocketUrl { get; set; } = string.Empty;
    
    public List<Room> SelectedRooms { get; set; } = new();
}
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
    public MainWindow()
    {
        InitializeComponent();
    }
}
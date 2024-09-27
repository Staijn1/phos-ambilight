using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Phos.ScreenSync.New.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isConnected;


    public MainWindowViewModel()
    {
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            this.IsConnected = true;
        });
    }
}
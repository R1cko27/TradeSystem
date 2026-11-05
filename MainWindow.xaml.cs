// MainWindow.xaml.cs

using System.Windows;

namespace TradeSystem.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            await vm.StartAsync();
    }
}

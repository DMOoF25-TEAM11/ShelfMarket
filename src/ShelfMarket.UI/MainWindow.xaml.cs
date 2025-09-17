using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Infrastructure.Persistence;
using ShelfMarket.UI.Views.Windows;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
/// <remarks>
/// Hosts the main UI layout.
/// </remarks>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        DataContext = new MainWindowViewModel();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
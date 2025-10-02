using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.UI.ViewModels;
using System.ComponentModel;

namespace ShelfMarket.UI.Views.Windows;

public partial class ShelfInfoWindow : UserControl
{
    private int _shelfNumber;
    private ShelfViewModel? _viewModel;

    public ShelfInfoWindow()
    {
        InitializeComponent();
    }

    public void SetViewModel(ShelfViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public void SetShelfNumber(int number)
    {
        _shelfNumber = number;
        if (TitleText != null)
        {
            TitleText.Text = $"Reol {number}";
        }
        if (_viewModel != null)
        {
            _viewModel.ShelfNumber = number;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
        if (overlay != null)
        {
            overlay.Visibility = Visibility.Collapsed;
        }
    }

    private void ReolTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void ChooseShelves_Click(object sender, RoutedEventArgs e) { }
    private void DeleteShelf_Click(object sender, RoutedEventArgs e) { }
}

using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class MainWindowViewModel : ModelBase
{
    private string _selectedMenu;
    public string SelectedMenu
    {
        get => _selectedMenu;
        set
        {
            if (_selectedMenu == value) return;
            _selectedMenu = value;
            OnPropertyChanged();
            UpdateCurrentViewModelForMenu(value);
        }
    }

    private ModelBase? _currentViewModel;
    public ModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (_currentViewModel == value) return;
            _currentViewModel = value;
            OnPropertyChanged();
        }
    }

    public MainWindowViewModel()
    {
        // Default til Reoler som forside
        _selectedMenu = "Reoler";
        UpdateCurrentViewModelForMenu(_selectedMenu);
    }

    private void UpdateCurrentViewModelForMenu(string? menu)
    {
        switch (menu)
        {
            case "Reoler":
                CurrentViewModel = new ShelfViewModel();
                break;
            case "Salg":
                CurrentViewModel = new SalesViewModel();
                break;
            case "Økonomi":
                CurrentViewModel = new FinanceViewModel();
                break;
            case "Arrangementer":
                CurrentViewModel = new EventsViewModel();
                break;
            case "Lejere":
                CurrentViewModel = new TenantsViewModel();
                break;
            case "Vedligeholdelse":
                CurrentViewModel = new MaintenanceViewModel();
                break;
            // Tilføj cases når andre views får rigtige ViewModels
            default:
                CurrentViewModel = null; // Ingen visning for placeholders
                break;
        }
    }
}



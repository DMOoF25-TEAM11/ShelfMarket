using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Application.Abstract.Services.Barcodes;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class EanLabelGeneratorViewModel : ModelBase
{
    private readonly IEan13Generator _generator;
    private readonly IShelfTenantRepository _tenantRepo;
    private readonly IShelfRepository _shelfRepo;

    public EanLabelGeneratorViewModel(IEan13Generator generator)
    {
        _generator = generator;
        // resolve repos
        _tenantRepo = App.HostInstance.Services.GetRequiredService<IShelfTenantRepository>();
        _shelfRepo = App.HostInstance.Services.GetRequiredService<IShelfRepository>();

        SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        Price = 0m;

        SearchTenantCommand = new RelayCommand(async () => await SearchTenantAsync(), () => IsValidEmail(SearchEmail));
        ClearTenantCommand = new RelayCommand(ClearTenant);

        GenerateCommand = new RelayCommand(async () => await GenerateAsync(), CanGenerate);

        _ = LoadShelvesAsync();
    }

    #region Tenant search
    private string _searchEmail = string.Empty;
    public string SearchEmail
    {
        get => _searchEmail;
        set
        {
            if (_searchEmail == value) return;
            _searchEmail = value;
            OnPropertyChanged();
            (SearchTenantCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private string _searchResultMessage = string.Empty;
    public string SearchResultMessage
    {
        get => _searchResultMessage;
        private set { if (_searchResultMessage == value) return; _searchResultMessage = value; OnPropertyChanged(); }
    }

    private ShelfTenant? _tenant;
    public bool HasTenant => _tenant != null;
    public string TenantDisplayName => _tenant == null ? string.Empty : $"{_tenant.FirstName} {_tenant.LastName}";

    public ICommand SearchTenantCommand { get; }
    public ICommand ClearTenantCommand { get; }

    private async Task SearchTenantAsync()
    {
        try
        {
            Error = string.Empty;
            _tenant = await _tenantRepo.GetByEmailAsync(SearchEmail.Trim());
            if (_tenant == null)
            {
                SearchResultMessage = "Ingen lejer fundet.";
            }
            else
            {
                SearchResultMessage = "Lejer fundet.";
            }
            OnPropertyChanged(nameof(HasTenant));
            OnPropertyChanged(nameof(TenantDisplayName));
            _ = LoadShelvesAsync(); // Refresh shelves after tenant search
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            _ = LoadShelvesAsync(); // Ensure shelves cleared on error
        }
        finally
        {
            (GenerateCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private void ClearTenant()
    {
        _tenant = null;
        SearchResultMessage = "Ryddet.";
        OnPropertyChanged(nameof(HasTenant));
        OnPropertyChanged(nameof(TenantDisplayName));
        (GenerateCommand as RelayCommand)?.RaiseCanExecuteChanged();
        _ = LoadShelvesAsync(); // Clear shelves when tenant cleared
    }
    #endregion

    #region Shelves / Price
    public ObservableCollection<int> ShelfNumbers { get; } = new();

    private int? _selectedShelf;
    public int? SelectedShelf
    {
        get => _selectedShelf;
        set
        {
            if (_selectedShelf == value) return;
            _selectedShelf = value;
            OnPropertyChanged();
            UpdateDisplays();
            (GenerateCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private DateTime _selectedDate = DateTime.Now;
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate == value) return;
            _selectedDate = value;
            OnPropertyChanged();
            _ = LoadShelvesAsync(); // Refresh shelves when date changes
        }
    }

    private decimal _price;
    public decimal Price
    {
        get => _price;
        set
        {
            if (_price == value) return;
            _price = value;
            OnPropertyChanged();
            UpdateDisplays();
            (GenerateCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
    #endregion

    #region Outputs
    private string _ean = string.Empty;
    public string Ean
    {
        get => _ean;
        private set { if (_ean == value) return; _ean = value; OnPropertyChanged(); }
    }

    private ImageSource? _barcodeImage;
    public ImageSource? BarcodeImage
    {
        get => _barcodeImage;
        private set { if (_barcodeImage == value) return; _barcodeImage = value; OnPropertyChanged(); }
    }

    private string _shelfDisplay = string.Empty;
    public string ShelfDisplay
    {
        get => _shelfDisplay;
        private set { if (_shelfDisplay == value) return; _shelfDisplay = value; OnPropertyChanged(); }
    }

    private string _priceDisplay = string.Empty;
    public string PriceDisplay
    {
        get => _priceDisplay;
        private set { if (_priceDisplay == value) return; _priceDisplay = value; OnPropertyChanged(); }
    }
    #endregion

    #region Error (reuse from ModelBase)
    public string Error
    {
        get => _error;
        private set { if (_error == value) return; _error = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }
    private string _error = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    #endregion

    #region Commands
    public ICommand GenerateCommand { get; }
    private bool CanGenerate() =>
        HasTenant &&
        SelectedShelf.HasValue &&
        SelectedShelf > 0 &&
        Price > 0m;

    private async Task GenerateAsync()
    {
        try
        {
            Error = string.Empty;
            if (!SelectedShelf.HasValue)
            {
                Error = "Vælg en reol.";
                return;
            }
            var code = _generator.Build(SelectedShelf.Value.ToString(), Price);
            Ean = code;
            var bytes = await _generator.RenderPngAsync(code, scale: 3, barHeight: 60, includeNumbers: true);
            BarcodeImage = ToImage(bytes);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }
    #endregion

    #region Helpers
    private async Task LoadShelvesAsync()
    {
        try
        {
            ShelfNumbers.Clear();
            if (_tenant?.Id == null)
            {
                // No tenant selected, nothing to load
                return;
            }
            var all = await _shelfRepo.GetAvailableShelvesForTenantAsync(_tenant.Id.Value, SelectedDate);
            foreach (var s in all.OrderBy(s => s.Number))
                ShelfNumbers.Add(s.Number);
        }
        catch (Exception ex)
        {
            Error = $"Kunne ikke hente reoler: {ex.Message}";
        }
    }

    private void UpdateDisplays()
    {
        ShelfDisplay = SelectedShelf.HasValue ? SelectedShelf.Value.ToString() : "-";
        PriceDisplay = Price > 0 ? $"{Price:0.00} kr." : "-";
    }

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) &&
        System.Text.RegularExpressions.Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static ImageSource ToImage(byte[] data)
    {
        var bmp = new BitmapImage();
        using var ms = new MemoryStream(data);
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.StreamSource = ms;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }
    #endregion
}

using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Application.Abstract.Services.Barcodes;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public sealed class EanLabelGeneratorViewModel : ModelBase
{
    private readonly IEan13Generator _barcode;

    #region Commands Declaration
    public ICommand GenerateCommand { get; }
    #endregion

    public EanLabelGeneratorViewModel(IEan13Generator barcode)
    {
        _barcode = barcode ?? throw new ArgumentNullException(nameof(barcode));
        GenerateCommand = new RelayCommand(OnGenerate, CanGenerate);
        _shelfTenantsRepository = App.HostInstance.Services.GetRequiredService<IShelfTenantRepository>();
        _shelfRepository = App.HostInstance.Services.GetRequiredService<IShelfRepository>();

        GetShelfTenantsAsync().GetAwaiter();
    }

    #region Form Fields
    // Collections for dropdowns
    private readonly IShelfTenantRepository _shelfTenantsRepository;
    public ObservableCollection<ShelfTenant> ShelfTenants { get; } = new();
    private readonly IShelfRepository _shelfRepository;
    public ObservableCollection<Shelf> Shelfs { get; } = new();

    private string? _selectedShelfTenant;
    public string? SelectedShelfTenant
    {
        get => _selectedShelfTenant;
        set
        {
            if (Set(ref _selectedShelfTenant, value))
            {
                // TODO: Load shelf numbers for selected tenant from your repository/service.
                // For now, clear and keep current selection consistent.
                Shelfs.Clear();
                SelectedShelfNumber = null;
                GetShelfsForTenantAsync(Guid.Parse(value ?? Guid.Empty.ToString())).GetAwaiter();
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    private int? _selectedShelfNumber;
    public int? SelectedShelfNumber
    {
        get => _selectedShelfNumber;
        set
        {
            if (Set(ref _selectedShelfNumber, value))
            {
                OnPropertyChanged(nameof(ShelfDisplay));
                RaiseCanExecuteChanged();
            }
        }
    }

    private decimal _price;
    public decimal Price
    {
        get => _price;
        set
        {
            if (Set(ref _price, value))
            {
                OnPropertyChanged(nameof(PriceDisplay));
                RaiseCanExecuteChanged();
            }
        }
    }

    private string? _ean;
    public string? Ean
    {
        get => _ean;
        private set => Set(ref _ean, value);
    }

    private ImageSource? _barcodeImage;
    public ImageSource? BarcodeImage
    {
        get => _barcodeImage;
        private set => Set(ref _barcodeImage, value);
    }

    private string? _error;
    public string? Error
    {
        get => _error;
        private set
        {
            if (Set(ref _error, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    // Derived displays
    public string ShelfDisplay => SelectedShelfNumber is int n ? n.ToString("000000", CultureInfo.InvariantCulture) : string.Empty;
    public string PriceDisplay => Price.ToString("C", CultureInfo.CurrentCulture);
    #endregion

    #region Load handler
    private async Task GetShelfTenantsAsync()
    {
        foreach (var t in await _shelfTenantsRepository.GetAllAsync())
            ShelfTenants.Add(t);
    }

    private async Task GetShelfsForTenantAsync(Guid tenant)
    {
        var shelf = await _shelfRepository.GetByIdAsync(tenant);
        if (shelf != null)
            Shelfs.Add(shelf);
    }
    //private async Task GetShelfNumbersForTenant(string tenant)
    //{
    //    foreach (var n in await _shelfTenantsRepository.GetShelfNumbersAsync(tenant))
    //        ShelfNumbers.Add(n);
    //}
    #endregion

    #region CanXXX Command States
    private bool CanGenerate()
        => SelectedShelfNumber is int n && n >= 0 && n <= 999_999
           && Price >= 0m && Price <= 9_999.99m;
    #endregion

    #region OnXXX Command Handlers
    private void OnGenerate()
    {
        Error = null;
        try
        {
            if (SelectedShelfNumber is not int shelf) return;

            var ean = _barcode.Build(shelf.ToString(), Price);
            Ean = ean;

            var bytes = _barcode.RenderPng(ean, scale: 3, barHeight: 60, includeNumbers: true);
            BarcodeImage = ToImageSource(bytes);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Error = ex.Message;
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
        }
    }
    #endregion


    // Helpers
    private static ImageSource ToImageSource(byte[] data)
    {
        var bi = new BitmapImage();
        using var ms = new MemoryStream(data, writable: false);
        bi.BeginInit();
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.StreamSource = ms;
        bi.EndInit();
        bi.Freeze();
        return bi;
    }


    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void RaiseCanExecuteChanged()
    {
        if (GenerateCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
    }
}

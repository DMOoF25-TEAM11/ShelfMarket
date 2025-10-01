using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Application.Abstract.Services.Barcodes;
using ShelfMarket.Application.DTOs;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Domain.Enums;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class SalesViewModel : ViewModelBase<ISalesRepository, SalesReceipt>
{
    public ObservableCollection<SalesReceiptLine> SalesLines { get; set; } = new ObservableCollection<SalesReceiptLine>();

    // Add a private readonly field for IEan13Generator
    private readonly IEan13Generator _ean13Generator;

    public SalesViewModel(ISalesRepository? selected = null, IEan13Generator? ean13Generator = null) : base(selected ?? App.HostInstance.Services.GetRequiredService<ISalesRepository>())
    {
        _ean13Generator = ean13Generator ?? App.HostInstance.Services.GetRequiredService<IEan13Generator>();
        SalesLines.CollectionChanged += (s, e) =>
        {
            RefreshCommandStates();
        };
    }

    public SalesReceiptWithTotalAmountDto? SalesCommit { get; set; } = null;



    #region Form Fields
    private string _shelfNumber = string.Empty;
    public string ShelfNumber
    {
        get => _shelfNumber;
        set
        {
            if (_shelfNumber == value) return;
            _shelfNumber = value;
            OnPropertyChanged();
            RefreshCommandStates();
        }
    }

    private string _eanNumber = string.Empty;
    public string EanNumber
    {
        get { return _eanNumber; }
        set { _eanNumber = value; }
    }

    private string _unitPrice = string.Empty;
    public string UnitPrice
    {
        get { return _unitPrice; }
        set
        {
            if (_unitPrice == value) return;
            _unitPrice = value;
            OnPropertyChanged();
            RefreshCommandStates();
        }
    }

    public SalesReceiptWithTotalAmountDto? ConfirmSale { get; set; }
    #endregion


    #region Load handler
    #endregion

    #region Commands
    public ICommand CashPayCommand => new RelayCommand(OnCashPay, CanCashPay);
    public ICommand MobilePayCommand => new RelayCommand(async () => await OnMobilePayAsync(), CanMobilePay);
    public ICommand BeginNewSaleCommand => new RelayCommand(OnNewSale);
    public ICommand PrintReceiptCommand => new RelayCommand(OnPrintReceipt, CanPrintReceipt);
    public ICommand OnShelfNumberEnteredCommand => new RelayCommand(OnShelfNumberEntered, CanShelfNumberEntered);
    public ICommand OnPriceEnteredCommand => new RelayCommand(OnPriceEntered);
    #endregion

    #region CanXXX Command
    protected override bool CanCancel()
        => base.CanCancel()
        && ShelfNumber != string.Empty;

    private bool CanCashPay() => true;

    private bool CanMobilePay() => true;
    private bool CanPrintReceipt()
    {
        // Enable only if there is a confirmed sale
        return ConfirmSale != null;
    }

    private bool CanShelfNumberEntered() => !string.IsNullOrWhiteSpace(ShelfNumber);
    #endregion

    #region OnXXX Command
    protected override Task OnResetFormAsync()
    {
        ConfirmSale = null;
        SalesLines.Clear();
        ShelfNumber = string.Empty;
        UnitPrice = string.Empty;
        StatusMessage = "Formular nulstillet.";
        OnPropertyChanged(nameof(ShelfNumber));
        OnPropertyChanged(nameof(UnitPrice));
        RefreshCommandStates();
        return Task.CompletedTask;
    }

    protected override Task<SalesReceipt> OnAddFormAsync()
    {
        throw new NotImplementedException();
    }

    protected override Task OnSaveFormAsync()
    {
        throw new NotImplementedException();
    }

    protected override Task OnLoadFormAsync(SalesReceipt entity)
    {
        throw new NotImplementedException();
    }

    private void OnNewSale()
    {
        // Clear sales lines and reset form fields for a new sale
        SalesLines.Clear();
        ShelfNumber = string.Empty;
        UnitPrice = string.Empty;
        StatusMessage = "Klar til nyt salg.";
        OnPropertyChanged(nameof(ShelfNumber));
        OnPropertyChanged(nameof(UnitPrice));
    }

    private void OnPrintReceipt()
    {
        // Implement receipt printing logic here
        StatusMessage = "Kvittering udskrevet.";
    }

    private void OnShelfNumberEntered()
    {
        int shelfNumber;
        decimal price;

        // 1. Verify EAN
        if (!IsValidEan(ShelfNumber.ToString()))
        {
            ErrorMessage = "Ugyldig EAN-nummer.";
            return;
        }

        // 2. Get shelf and unitPrice from helper
        TransformEanToShelfAndPrice(ShelfNumber, out shelfNumber, out price);

        ShelfNumber = shelfNumber.ToString() != "0" ? shelfNumber.ToString() : string.Empty;
        UnitPrice = price.ToString("C2");

        OnPropertyChanged(nameof(ShelfNumber));
        OnPropertyChanged(nameof(UnitPrice));

        // You may want to check if shelfNumber/unitPrice are valid here
        // For now, let's assume if shelfNumber is 0, it's not found
        if (shelfNumber == 0)
        {
            ErrorMessage = "EAN ikke fundet.";
            return;
        }

        // 3. Add sales line
        SalesLines.Add(new SalesReceiptLine
        {
            ShelfNumber = shelfNumber,
            UnitPrice = price
        });
        OnPropertyChanged(nameof(SalesLines));

        // 4. Clear form fields
        ShelfNumber = string.Empty;
        UnitPrice = string.Empty;
        OnPropertyChanged(nameof(ShelfNumber));
        OnPropertyChanged(nameof(UnitPrice));
        RefreshCommandStates();
        StatusMessage = "Salgs linje tilføjet.";
    }
    private void OnPriceEntered()
    {
        int shelfNumber;
        decimal unitPrice;

        int.TryParse(ShelfNumber, out shelfNumber);
        var normalizedPrice = UnitPrice.Replace(',', '.');
        decimal.TryParse(normalizedPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out unitPrice);
        //decimal.TryParse(UnitPrice.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out unitPrice);

        SalesLines.Add(new SalesReceiptLine
        {
            ShelfNumber = shelfNumber,
            UnitPrice = unitPrice
        });
        OnPropertyChanged(nameof(SalesLines));

        ShelfNumber = string.Empty;
        UnitPrice = string.Empty;
        OnPropertyChanged(nameof(ShelfNumber));
        OnPropertyChanged(nameof(UnitPrice));
        RefreshCommandStates();
        StatusMessage = "Salgs linje tilføjet.";
    }
    private void OnCashPay()
    {
        AddSalesLineToReposAndConfirmSale(PaymentMethod.Cash);
        //return Task.CompletedTask;
    }

    private async Task OnMobilePayAsync()
    {
        AddSalesLineToReposAndConfirmSale(PaymentMethod.MobilePay);
        await Task.CompletedTask;
    }
    #endregion


    #region Helpers
    private bool IsValidEan(string ean)
    {
        try
        {
            _ean13Generator.ValidateEan13(ean);
        }
        catch
        {
            ErrorMessage = "Ugyldig EAN-format.";
            return false;
        }

        // Implement EAN validation logic here
        return true; // Placeholder
    }

    public async Task<bool> IsShelfNumberValid(string shelfNumber)
    {
        var shelfrepo = App.HostInstance.Services.GetRequiredService<IShelfRepository>();
        uint num;
        uint.TryParse(shelfNumber, out num);
        return await shelfrepo.ExistsShelfNumberAsync((int)num);
    }

    private async void AddSalesLineToReposAndConfirmSale(PaymentMethod paymentMethod)
    {
        var salesLines = SalesLines.Select(line => new SalesReceiptLine
        {
            ShelfNumber = line.ShelfNumber,
            UnitPrice = line.UnitPrice
        }).ToList();

        // TODO : Move logic of Payment method to the repository
        var salesRecord = new SalesReceipt
        {
            IssuedAt = DateTime.Now,
            PaidByCash = paymentMethod == PaymentMethod.Cash,
            PaidByMobile = paymentMethod == PaymentMethod.MobilePay,
            SalesLine = salesLines
        };
        ConfirmSale = await _repository.SetSaleAsync(salesRecord, salesLines);
        RefreshCommandStates();
        await Task.CompletedTask;
    }

    private void TransformEanToShelfAndPrice(string ean, out int shelfNumber, out decimal price)
    {
        if (!IsValidEan(ean))
        {
            shelfNumber = 0;
            price = 0;
            return;
        }

        // Get the first 6 digits as shelf number and next 6 as unitPrice in cents
        string shelfPart = ean[..6];
        string pricePart = ean[6..12];

        if (!int.TryParse(shelfPart, out shelfNumber) || !int.TryParse(pricePart, out int priceInCents))
        {
            shelfNumber = 0;
            price = 0;
            return;
        }

        price = priceInCents / 100;
    }
    #endregion

    protected override void RefreshCommandStates()
    {
        base.RefreshCommandStates();
        (CashPayCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (MobilePayCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (PrintReceiptCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (BeginNewSaleCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
}


using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Application.Abstract.Services.Barcodes;
using ShelfMarket.Application.DTOs;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class SalesViewModel : ViewModelBase<ISalesRepository, Sales>
{
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
        }
    }

    private int _eanNumber;

    public int EanNumber
    {
        get { return _eanNumber; }
        set { _eanNumber = value; }
    }

    private decimal _price;

    public decimal Price
    {
        get { return _price; }
        set { _price = value; }
    }

    public SalesReceiptWithTotalAmountDto? ConfirmSale { get; set; }
    #endregion

    public ObservableCollection<FiktivSalesReceiptLine> Lines { get; } = new();

    // Add a private readonly field for IEan13Generator
    private readonly IEan13Generator _ean13Generator;

    public SalesViewModel(ISalesRepository? selected = null, IEan13Generator? ean13Generator = null) : base(selected ?? App.HostInstance.Services.GetRequiredService<ISalesRepository>())
    {
        _ean13Generator = ean13Generator ?? App.HostInstance.Services.GetRequiredService<IEan13Generator>();
        CashPayCommand = new RelayCommand(async () => await OnCashPayAsync(), CanCashPay);
        MobilePayCommand = new RelayCommand(async () => await OnMobilePayAsync(), CanMobilePay);
        OnShelfNumberEnteredCommand = new RelayCommand(OnShelfNumberEntered);


    }



    #region Load handler
    #endregion

    #region Commands
    public ICommand CashPayCommand { get; }
    public ICommand MobilePayCommand { get; }
    public ICommand OnShelfNumberEnteredCommand { get; }
    #endregion

    #region CanXXX Command
    private bool CanCashPay()
    {
        // Example: Only allow if there are lines and not already processing
        return Lines.Count > 0;
    }

    private bool CanMobilePay()
    {
        // Example: Only allow if there are lines and not already processing
        return Lines.Count > 0;
    }
    #endregion

    #region OnXXX Command
    protected override Task OnResetFormAsync()
    {
        throw new NotImplementedException();
    }

    protected override Task<Sales> OnAddFormAsync()
    {
        throw new NotImplementedException();
    }

    protected override Task OnSaveFormAsync()
    {
        throw new NotImplementedException();
    }

    protected override Task OnLoadFormAsync(Sales entity)
    {
        throw new NotImplementedException();
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

        // 2. Get shelf and price from repository
        TransformEanToShelfAndPrice(ShelfNumber, out shelfNumber, out price);

        ShelfNumber = shelfNumber.ToString() != "0" ? shelfNumber.ToString() : string.Empty;
        Price = price;

        OnPropertyChanged(nameof(ShelfNumber));
        OnPropertyChanged(nameof(Price));

        // You may want to check if shelfNumber/price are valid here
        // For now, let's assume if shelfNumber is 0, it's not found
        if (shelfNumber == 0)
        {
            ErrorMessage = "EAN ikke fundet.";
            return;
        }

        // 3. Add sales line
        Lines.Add(new FiktivSalesReceiptLine
        {
            ShelfNumber = shelfNumber,
            UnitPrice = price
        });

        // 4. Clear form fields
        ShelfNumber = "0";
        Price = 0;
        OnPropertyChanged(nameof(ShelfNumber));
        OnPropertyChanged(nameof(Price));
        StatusMessage = "Salgs linje tilføjet.";
    }

    private async Task OnCashPayAsync()
    {
        var salesLines = Lines.Select(line => new SalesLine
        {
            ShelfNumber = (uint)line.ShelfNumber,
            Price = line.UnitPrice
            // Map other required properties if needed
        }).ToList();

        await _repository.SetSaleAsync(salesLines, paidByCash: true, paidByMobile: false);
        Lines.Clear();
    }

    private async Task OnMobilePayAsync()
    {
        var salesLines = Lines.Select(line => new SalesLine
        {
            ShelfNumber = (uint)line.ShelfNumber,
            Price = line.UnitPrice
            // Map other required properties if needed
        }).ToList();

        await _repository.SetSaleAsync(salesLines, paidByCash: false, paidByMobile: true);
        Lines.Clear();
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



    private bool isEanExists(int ean)
    {
        // Implement logic to check if EAN exists in the database
        return true; // Placeholder
    }

    private void TransformEanToShelfAndPrice(string ean, out int shelfNumber, out decimal price)
    {
        if (!IsValidEan(ean))
        {
            shelfNumber = 0;
            price = 0;
            return;
        }

        string shelfPart = ean.Substring(0, 6);
        string pricePart = ean.Substring(5, 6);

        if (!int.TryParse(shelfPart, out shelfNumber) || !int.TryParse(pricePart, out int priceInCents))
        {
            shelfNumber = 0;
            price = 0;
            return;
        }

        price = priceInCents / 100;
    }
    #endregion
}

// Helper DTO for lines
public class FiktivSalesReceiptLine
{
    public int ShelfNumber { get; set; }
    public decimal UnitPrice { get; set; }
}



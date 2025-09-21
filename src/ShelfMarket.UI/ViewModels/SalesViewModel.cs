using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class SalesViewModel : ViewModelBase<ISalesRepository, Sales>
{
    #region Form Fields
    private int _shelfNumber;

    public int ShelfNumber
    {
        get { return _shelfNumber; }
        set
        {
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

    private int _price;

    public int Price
    {
        get { return _price; }
        set { _price = value; }
    }
    #endregion

    public SalesViewModel(ISalesRepository? selected = null) : base(selected ?? App.HostInstance.Services.GetRequiredService<ISalesRepository>())
    {

    }

    #region Load handler
    #endregion

    #region Helpers
    private bool isEanValid(int ean)
    {
        // Implement EAN validation logic here
        return true; // Placeholder
    }

    private bool isEanExists(int ean)
    {
        // Implement logic to check if EAN exists in the database
        return true; // Placeholder
    }

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
    #endregion
}



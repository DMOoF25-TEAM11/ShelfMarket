using ShelfMarket.Application.Abstract;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;
using ShelfMarket.UI.ViewModels.List.Item;

namespace ShelfMarket.UI.ViewModels.List;

public class ManagesShelfTanentContractListViewModel : ListViewModelBase<IShelfTenantContractRepository, ManagesShelfTenantContractListItemViewModel>
{
    public ManagesShelfTanentContractListViewModel(IShelfTenantContractRepository repository) : base(repository)
    {
    }

    public override async Task RefreshAsync()
    {
        Error = null;
        IsLoading = true;

        try
        {
            var items = await _repository.GetAllAsync();

            // Clear and repopulate the Items collection
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(new ManagesShelfTenantContractListItemViewModel(item));
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsLoading = false;
            (RefreshCommandState as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}

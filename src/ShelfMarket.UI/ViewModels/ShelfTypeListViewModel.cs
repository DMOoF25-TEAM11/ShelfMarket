using ShelfMarket.Application.Interfaces;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public sealed class ShelfTypeListViewModel : ListViewModelBase<IShelfTypeRepository, ShelfTypeListItemViewModel>
{
    public ShelfTypeListViewModel(IShelfTypeRepository repository) : base(repository)
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
                Items.Add(new ShelfTypeListItemViewModel(item));
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

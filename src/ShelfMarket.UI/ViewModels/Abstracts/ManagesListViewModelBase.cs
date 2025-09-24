using System.Collections.ObjectModel;
using System.Windows.Input;
using ShelfMarket.Application.Abstract;
using ShelfMarket.UI.Commands;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ShelfMarket.UI.ViewModels.Abstracts;

// Manages-list base: collection + selection + refresh, built on CRUD base
public abstract class ManagesListViewModelBase<TRepos, TEntity> : CrudViewModelBase<TRepos, TEntity>
    where TRepos : notnull, IRepository<TEntity>
    where TEntity : class
{
    public ObservableCollection<TEntity> Items { get; } = new();

    private TEntity? _selectedItem;
    public TEntity? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (ReferenceEquals(_selectedItem, value)) return;
            _selectedItem = value;
            OnPropertyChanged();
            _ = OnSelectedItemChangedAsync(value);
        }
    }

    public bool IsLoading { get; protected set; }

    public ICommand RefreshCommand { get; }

    protected ManagesListViewModelBase(TRepos repository) : base(repository)
    {
        RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsLoading);
    }

    protected virtual async Task OnSelectedItemChangedAsync(TEntity? item)
    {
        if (item is null)
        {
            CurrentEntity = null;
            IsEditMode = false;
            RefreshCommandStates();
            return;
        }

        CurrentEntity = item;
        await OnLoadFormAsync(item);
        IsEditMode = true;
        RefreshCommandStates();
    }

    public async Task RefreshAsync()
    {
        Error = null;
        IsLoading = true;
        try
        {
            var data = await LoadItemsAsync();
            Items.Clear();
            foreach (var it in data)
                Items.Add(it);
            OnPropertyChanged(nameof(Items));
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsLoading = false;
            (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
            RefreshCommandStates();
        }
    }

    protected abstract Task<IEnumerable<TEntity>> LoadItemsAsync();
}
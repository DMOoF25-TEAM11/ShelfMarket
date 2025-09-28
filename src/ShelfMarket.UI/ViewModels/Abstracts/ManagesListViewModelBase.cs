using System.Collections.ObjectModel;
using System.Windows.Input;
using ShelfMarket.Application.Abstract;
using ShelfMarket.UI.Commands;

namespace ShelfMarket.UI.ViewModels.Abstracts;

/// <summary>
/// Base view model that extends <see cref="CrudViewModelBase{TRepos, TEntity}"/> with
/// list management capabilities (collection + selection + refresh) for WPF MVVM scenarios.
/// </summary>
/// <typeparam name="TRepos">
/// Repository abstraction used for persistence; must implement <see cref="IRepository{TEntity}"/>.
/// </typeparam>
/// <typeparam name="TEntity">
/// Entity type managed by the list and CRUD operations.
/// </typeparam>
/// <remarks>
/// Responsibilities:
///  - Maintains an observable collection of entities (<see cref="Items"/>).
///  - Handles selection changes and loads form data for the selected entity.
///  - Provides a thread-safe asynchronous refresh mechanism (<see cref="RefreshAsync"/>) guarded
///    by a <see cref="SemaphoreSlim"/> to prevent overlapping loads.
///  - Inherits add / save / delete functionality from the CRUD base.
/// </remarks>
public abstract class ManagesListViewModelBase<TRepos, TEntity> : CrudViewModelBase<TRepos, TEntity>
    where TRepos : notnull, IRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Backing collection of entities displayed in the UI (e.g., bound to an <c>ItemsControl</c> or <c>DataGrid</c>).
    /// </summary>
    public ObservableCollection<TEntity> Items { get; } = new();

    private TEntity? _selectedItem;

    /// <summary>
    /// Currently selected item from <see cref="Items"/>. Setting this triggers loading of the item
    /// into the edit form (entering edit mode) or clearing the form (leaving edit mode) when set to <c>null</c>.
    /// </summary>
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

    /// <summary>
    /// Indicates whether a refresh (data load) operation is currently in progress.
    /// </summary>
    public bool IsLoading { get; protected set; }

    /// <summary>
    /// Command that triggers a reload of the entity list by invoking <see cref="RefreshAsync"/>.
    /// Disabled while a refresh is running.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Synchronization primitive preventing overlapping refresh operations.
    /// </summary>
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>
    /// Initializes the list-managing view model with a repository instance (resolved via DI if <c>null</c>).
    /// </summary>
    /// <param name="repository">Repository implementation used for data access.</param>
    protected ManagesListViewModelBase(TRepos repository) : base(repository)
    {
        RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsLoading);
    }

    /// <summary>
    /// Handles selection changes. Loads form data for the selected item (entering edit mode),
    /// or clears the form when selection is cleared.
    /// </summary>
    /// <param name="item">The newly selected item or <c>null</c>.</param>
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

    /// <summary>
    /// Refreshes the <see cref="Items"/> collection by invoking <see cref="LoadItemsAsync"/>.
    /// Ensures only one refresh runs at a time and updates <see cref="IsLoading"/> + error state.
    /// </summary>
    public async Task RefreshAsync()
    {
        await _refreshLock.WaitAsync();
        IsLoading = true;
        try
        {
            Error = null;
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
            _refreshLock.Release();
            (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
            RefreshCommandStates();
        }
    }

    /// <summary>
    /// Loads the entities to populate <see cref="Items"/>. Implementations should perform any
    /// required filtering, ordering, or projection before returning results.
    /// </summary>
    /// <returns>An enumerable sequence of entities.</returns>
    protected abstract Task<IEnumerable<TEntity>> LoadItemsAsync();
}
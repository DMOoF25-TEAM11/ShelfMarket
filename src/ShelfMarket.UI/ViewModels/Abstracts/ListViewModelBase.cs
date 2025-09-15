using System.Collections.ObjectModel;
using System.Windows.Input;
using ShelfMarket.UI.Commands;

namespace ShelfMarket.UI.ViewModels.Abstracts;

/// <summary>
/// Provides a base class for list view models in the MVVM pattern, supporting item selection, error handling, and refresh logic.
/// </summary>
/// <typeparam name="TEntityRepo">The type of the repository or data source used by the view model.</typeparam>
/// <typeparam name="ListItemVM">The type of the item view model contained in the list.</typeparam>
public abstract class ListViewModelBase<TEntityRepo, ListItemVM> : ModelBase
    where TEntityRepo : class
    where ListItemVM : class
{
    /// <summary>
    /// The repository or data source instance used by the view model.
    /// </summary>
    protected readonly TEntityRepo _repository;

    /// <summary>
    /// Backing field for the <see cref="SelectedItem"/> property.
    /// </summary>
    protected ListItemVM? _selectedItem;

    /// <summary>
    /// Gets or sets the currently selected item in the list.
    /// </summary>
    public ListItemVM? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (Equals(_selectedItem, value)) return;
            _selectedItem = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the collection of item view models displayed in the list.
    /// </summary>
    public ObservableCollection<ListItemVM> Items { get; } = new();

    #region Error and State Management
    /// <summary>
    /// Backing field for the <see cref="IsLoading"/> property.
    /// </summary>
    protected bool _isLoading;

    /// <summary>
    /// Gets a value indicating whether the view model is currently loading data.
    /// </summary>
    public bool IsLoading { get; protected set; }

    /// <summary>
    /// Backing field for the <see cref="Error"/> property.
    /// </summary>
    protected string? _error;

    /// <summary>
    /// Gets or sets the error message for the view model.
    /// </summary>
    public string? Error
    {
        get => _error;
        protected set
        {
            if (_error == value) return;
            _error = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>
    /// Gets a value indicating whether the view model currently has an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(Error);
    #endregion

    /// <summary>
    /// Gets the command for refreshing the list of items.
    /// </summary>
    public ICommand RefreshCommandState { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListViewModelBase{TEntity, ListItemVM}"/> class.
    /// </summary>
    /// <param name="repository">The repository or data source instance to use.</param>
    public ListViewModelBase(TEntityRepo repository)
    {
        _repository = repository;
        RefreshCommandState = new RelayCommand(async () => await RefreshAsync(), () => !IsLoading);
        _ = RefreshAsync(); // initial load
    }

    /// <summary>
    /// Asynchronously refreshes the list of items. Must be implemented by derived classes.
    /// </summary>
    /// <returns>A task representing the asynchronous refresh operation.</returns>
    /// <TODO>Make this a virtual method</TODO>
    public abstract Task RefreshAsync();

}

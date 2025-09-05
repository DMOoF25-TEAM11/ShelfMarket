using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.UI.Commands;

namespace ShelfMarket.UI.ViewModels.Abstracts;

/// <summary>
/// Provides a generic base class for view models in the MVVM pattern, supporting command and state management.
/// </summary>
/// <typeparam name="TRepos">The type of the repository used by the view model.</typeparam>
/// <typeparam name="TEntity">The type of the entity managed by the view model.</typeparam>
public abstract class ViewModelBase<TRepos, TEntity> : ModelBase
    where TRepos : notnull
{
    private const int _infoMessageDuration = 3000; // Duration in milliseconds

    /// <summary>
    /// The repository instance used by the view model.
    /// </summary>
    protected readonly TRepos _repository;

    #region Commands Declaration
    /// <summary>
    /// Gets the command for adding a new entity.
    /// </summary>
    public ICommand AddCommand { get; }
    /// <summary>
    /// Gets the command for saving changes to an entity.
    /// </summary>
    public ICommand SaveCommand { get; }
    /// <summary>
    /// Gets the command for deleting an entity.
    /// </summary>
    public ICommand DeleteCommand { get; }
    /// <summary>
    /// Gets the command for resetting the view model state.
    /// </summary>
    public ICommand ResetCommand { get; }
    /// <summary>
    /// Gets the command for canceling the current operation.
    /// </summary>
    public ICommand CancelCommand { get; }
    #endregion

    #region Error and State Management
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
        protected set { if (_error == value) return; _error = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Gets a value indicating whether the view model currently has an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(Error);

    protected string? _infoMessage;
    /// <summary>
    /// Gets or sets the informational message for the view model.
    /// When set to a non-null or non-empty value, the message will automatically clear after a short delay.
    /// </summary>
    public string? InfoMessage
    {
        get => _infoMessage;
        protected set
        {
            if (_infoMessage == value) return;
            _infoMessage = value;
            OnPropertyChanged();
            if (!string.IsNullOrEmpty(value))
                _ = AutoClearInfoMessageAsync();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the view model currently has an informational message.
    /// </summary>
    public bool HasInfoMessage => !string.IsNullOrEmpty(InfoMessage);

    /// <summary>
    /// Backing field for the <see cref="IsSaving"/> property.
    /// </summary>
    protected bool _isSaving;

    /// <summary>
    /// Gets or sets a value indicating whether the view model is currently saving data.
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        protected set { if (_isSaving == value) return; _isSaving = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    /// <summary>
    /// Backing field for the <see cref="IsEditMode"/> property.
    /// </summary>
    protected bool _isEditMode;

    /// <summary>
    /// Gets or sets a value indicating whether the view model is in edit mode.
    /// </summary>
    public bool IsEditMode
    {
        get => _isEditMode;
        protected set
        {
            if (_isEditMode == value) return;
            _isEditMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAddMode));
            RefreshCommandStates();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the view model is in add mode.
    /// </summary>
    public bool IsAddMode => !IsEditMode;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelBase{TRepos, TEntity}"/> class.
    /// </summary>
    /// <param name="repository">The repository instance to use. If null, the repository is resolved from the service provider.</param>
    protected ViewModelBase(TRepos repository)
    {
        _repository = repository ?? App.HostInstance.Services.GetRequiredService<TRepos>();

        // Change RelayCommand instantiations to use async-compatible command
        AddCommand = new RelayCommand(async () => await OnAddAsync(), CanAdd);
        SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
        DeleteCommand = new RelayCommand(async () => await OnDeleteAsync(), CanDelete);
        ResetCommand = new RelayCommand(async () => await OnResetAsync(), CanReset);
        CancelCommand = new RelayCommand(async () => await OnCancelAsync(), CanCancel);

        IsEditMode = false;
    }

    #region Load method
    // Intentionally left blank for derived classes to implement loading logic.
    #endregion

    #region CanXXX methods
    /// <summary>
    /// Determines whether the core submit operation can be performed.
    /// </summary>
    /// <returns>True if the operation can be performed; otherwise, false.</returns>
    protected virtual bool CanSubmitCore()
    {
        return !IsSaving && !HasError;
    }

    /// <summary>
    /// Determines whether the add operation can be performed.
    /// </summary>
    /// <returns>True if add is allowed; otherwise, false.</returns>
    protected virtual bool CanAdd() => CanSubmitCore() && IsAddMode;

    /// <summary>
    /// Determines whether the save operation can be performed.
    /// </summary>
    /// <returns>True if save is allowed; otherwise, false.</returns>
    protected virtual bool CanSave() => CanSubmitCore() && IsEditMode;

    /// <summary>
    /// Determines whether the reset operation can be performed.
    /// </summary>
    /// <returns>True if reset is allowed; otherwise, false.</returns>
    protected virtual bool CanReset() => IsEditMode && !IsSaving;

    /// <summary>
    /// Determines whether the delete operation can be performed.
    /// </summary>
    /// <returns>True if delete is allowed; otherwise, false.</returns>
    protected virtual bool CanDelete() => IsEditMode && !IsSaving;

    /// <summary>
    /// Determines whether the cancel operation can be performed.
    /// </summary>
    /// <returns>True if cancel is allowed; otherwise, false.</returns>
    protected virtual bool CanCancel() => IsEditMode && !IsSaving;
    #endregion

    #region Command Handlers
    /// <summary>
    /// Handles the add command asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task OnAddAsync();

    /// <summary>
    /// Handles the save command asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task OnSaveAsync();

    /// <summary>
    /// Handles the cancel command asynchronously by resetting the view model.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnCancelAsync()
    {
        await OnResetAsync();
    }

    /// <summary>
    /// Handles the delete command asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task OnDeleteAsync();

    /// <summary>
    /// Handles the reset command asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task OnResetAsync();

    #endregion

    /// <summary>
    /// Automatically clears the informational message after a predefined delay.
    /// </summary>
    private async Task AutoClearInfoMessageAsync()
    {
        await Task.Delay(_infoMessageDuration);
        if (!string.IsNullOrEmpty(_infoMessage))
        {
            _infoMessage = null;
            //OnPropertyChanged(nameof(InfoMessage));
        }
    }

    /// <summary>
    /// Refreshes the state of all commands, causing their CanExecute status to be re-evaluated.
    /// </summary>
    protected virtual void RefreshCommandStates()
    {
        (AddCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ResetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
}


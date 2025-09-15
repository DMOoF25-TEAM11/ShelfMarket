using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.UI.Commands;

namespace ShelfMarket.UI.ViewModels.Abstracts;

/// <summary>
/// Provides a generic base class for view models in the MVVM pattern, supporting command and state management.
/// </summary>
/// <typeparam name="TRepos">The type of the repository used by the view model.</typeparam>
/// <typeparam name="TEntity">The type of the entity managed by the view model.</typeparam>
public abstract class ViewModelBase<TRepos, TEntity> : ModelBase
    where TRepos : notnull, IRepository<TEntity>
    where TEntity : class
{
    #region Constants
    /// <summary>
    /// The logical name of the entity type handled by the view model.
    /// Note: Uses nameof(TEntityRepo), which evaluates to the string "TEntityRepo".
    /// </summary>
    protected const string _entityName = nameof(TEntity);

    /// <summary>
    /// Prefix used for error messages.
    /// </summary>
    private const string _errorPrefix = "Fejl: ";

    /// <summary>
    /// Default message indicating that the requested entity could not be found.
    /// </summary>
    protected const string _errorEntityNotFound = _errorPrefix + _entityName + " " + " blev ikke fundet.";

    /// <summary>
    /// Prefix used for informational messages.
    /// </summary>
    private const string _infoPrefix = "Info: ";

    /// <summary>
    /// Default message indicating that an entity has been deleted.
    /// </summary>
    protected const string _infoDeleted = _entityName + " er slettet.";

    /// <summary>
    /// Default message indicating that an entity has been saved.
    /// </summary>
    protected const string _infoSaved = _entityName + " er gemt.";

    /// <summary>
    /// Title used for the delete confirmation dialog.
    /// </summary>
    protected const string _confirmDeleteTitle = "Bekræft slet " + _entityName;

    /// <summary>
    /// Message used for the delete confirmation dialog.
    /// </summary>
    protected string _confirmDelete = "Er du sikker på, at du vil slette " + _entityName + "?";

    /// <summary>
    /// The duration, in milliseconds, before the informational message is automatically cleared.
    /// </summary>
    private const int _infoMessageDuration = 3000; // Duration in milliseconds
    #endregion

    /// <summary>
    /// The currently selected or edited entity instance. When null, no entity is in context.
    /// </summary>
    protected TEntity? _currentEntity;

    /// <summary>
    /// Gets the currently selected or edited entity instance.
    /// Setting this value triggers property change notifications and refreshes command states.
    /// </summary>
    public TEntity? CurrentEntity
    {
        get => _currentEntity;
        protected set
        {
            if (_currentEntity == value) return;
            _currentEntity = value;
            OnPropertyChanged();
            RefreshCommandStates();
        }
    }

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
        protected set
        {
            if (_error == value) return; _error = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>
    /// Gets a value indicating whether the view model currently has an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(Error);

    /// <summary>
    /// Backing field for the <see cref="InfoMessage"/> property.
    /// </summary>
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
            OnPropertyChanged();                   // InfoMessage
            OnPropertyChanged(nameof(HasInfoMessage)); // ensure Visibility updates
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
    /// Changing this value raises <see cref="ModelBase.OnPropertyChanged(string?)"/> and refreshes commands.
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
    /// Changing this value notifies dependent properties and updates command states.
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
    /// Occurs when an entity has been successfully saved, added, or deleted.
    /// The event argument contains the affected entity instance or null (for delete).
    /// </summary>
    public event EventHandler<TEntity?>? EntitySaved;

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
    public async Task LoadAsync(Guid id)
    {
        Error = null;
        _currentEntity = null;
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity is null)
            {
                Error = _errorEntityNotFound;
                await OnResetAsync();
                return;
            }
            CurrentEntity = entity;
            await OnLoadFormAsync(entity);
            IsEditMode = true;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            RefreshCommandStates();
        }
    }
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
    protected virtual bool CanCancel() => !IsSaving;
    #endregion

    #region Command Handlers
    /// <summary>
    /// Handles the add command asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnAddAsync()
    {
        if (!CanAdd()) return;
        IsSaving = true;
        Error = null;
        var entity = await OnAddFormAsync();
        try
        {
            await _repository.AddAsync(entity);
            EntitySaved?.Invoke(this, entity);
            InfoMessage = _infoSaved;
            await OnResetAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsSaving = false;
            RefreshCommandStates();
        }
    }

    /// <summary>
    /// Handles the save command asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnSaveAsync()
    {
        if (!CanSave()) return;
        IsSaving = true;
        Error = null;
        try
        {
            if (_currentEntity is null) throw new InvalidOperationException("No current entity to save.");
            await OnSaveFormAsync();
            await _repository.UpdateAsync(_currentEntity);
            EntitySaved?.Invoke(this, _currentEntity);
            InfoMessage = _infoSaved;
            await OnResetAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsSaving = false;
            RefreshCommandStates();
        }
        await Task.CompletedTask;
    }

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
    protected async Task OnDeleteAsync()
    {
        if (_currentEntity is null) return;

        // Confirm deletion with the user
        if (MessageBox.Show(
                _confirmDelete,
                _confirmDeleteTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning)
            != MessageBoxResult.Yes)
            return;

        IsSaving = true;

        try
        {
            var id = GetEntityId(_currentEntity);
            if (id is null) return;

            await _repository.DeleteAsync(id.Value);
            EntitySaved?.Invoke(this, null);
            InfoMessage = _infoDeleted;
            await OnResetAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsSaving = false;
            RefreshCommandStates();
        }
    }

    /// <summary>
    /// Handles the reset command asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task OnResetAsync()
    {
        Error = null;
        _currentEntity = null; // switched from _currentId to _currentEntity
        await OnResetFormAsync();
        IsEditMode = false;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Called by <see cref="OnResetAsync"/> to reset the form state (e.g., clear fields).
    /// Implement in derived classes to restore the UI to its initial state.
    /// </summary>
    /// <returns>A task that completes when the form reset is finished.</returns>
    protected abstract Task OnResetFormAsync();

    /// <summary>
    /// Called by <see cref="OnAddAsync"/> to build and validate a new entity from the current form values.
    /// Implement in derived classes to map UI inputs to a <typeparamref name="TEntity"/> instance.
    /// </summary>
    /// <returns>The newly created entity.</returns>
    protected abstract Task<TEntity> OnAddFormAsync();
    protected abstract Task OnSaveFormAsync();

    protected abstract Task OnLoadFormAsync(TEntity entity);

    #endregion

    #region Helpers
    /// <summary>
    /// Extracts an entity identifier as a Guid from the supplied entity instance using a property named "Id".
    /// Override in derived classes for better performance or to handle different key names and types.
    /// </summary>
    /// <param name="entity">The entity instance from which to retrieve the identifier.</param>
    /// <returns>The Guid identifier if available; otherwise, null.</returns>
    protected virtual Guid? GetEntityId(TEntity entity)
    {
        var prop = typeof(TEntity).GetProperty("Id");
        if (prop is null) return null;
        return prop.GetValue(entity) as Guid?;
    }

    private async Task AutoClearInfoMessageAsync()
    {
        await Task.Delay(_infoMessageDuration);
        if (!string.IsNullOrEmpty(_infoMessage))
        {
            _infoMessage = null;
            OnPropertyChanged(nameof(InfoMessage));
            OnPropertyChanged(nameof(HasInfoMessage));
        }
    }
    #endregion

    #region Command State Refresh
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
    #endregion
}


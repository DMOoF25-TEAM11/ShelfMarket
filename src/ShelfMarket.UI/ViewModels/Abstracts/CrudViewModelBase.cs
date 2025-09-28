using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.UI.Commands;

namespace ShelfMarket.UI.ViewModels.Abstracts;

/// <summary>
/// Generic CRUD (Create / Read / Update / Delete) base view model intended for a single-entity
/// edit/create form in a WPF MVVM scenario.
/// </summary>
/// <typeparam name="TRepos">
/// Repository abstraction used to persist <typeparamref name="TEntity"/> instances. Must implement
/// <see cref="IRepository{TEntity}"/> and be resolvable via DI if not directly supplied.
/// </typeparam>
/// <typeparam name="TEntity">
/// Entity type represented by the form. Must be a reference type and (optionally) expose a public
/// <c>Id</c> property of type <see cref="Guid"/> for delete/update operations.
/// </typeparam>
/// <remarks>
/// Provides:
///  - Standard command set: Add, Save (update), Delete, Reset (clear form), Cancel (alias of reset)
///  - Status properties for UI binding: <see cref="IsEditMode"/>, <see cref="IsAddMode"/>, <see cref="IsSaving"/>, 
///    <see cref="Error"/>, <see cref="InfoMessage"/>
///  - Asynchronous command execution with simple optimistic error handling
///  - Automatic (timed) clearing of transient info messages
/// Derived classes implement the form binding logic via the abstract On*Form* methods.
/// </remarks>
public abstract class CrudViewModelBase<TRepos, TEntity> : ModelBase
    where TRepos : notnull, IRepository<TEntity>
    where TEntity : class
{
    // ---------------------------------------------------------------------
    // Constant message fragments / localization placeholders
    // ---------------------------------------------------------------------

    /// <summary>Logical (generic) entity name (uses the generic type parameter name).</summary>
    protected const string _entityName = nameof(TEntity);

    private const string _errorPrefix = "Fejl: ";
    /// <summary>Standard error shown when a requested entity was not located.</summary>
    protected const string _errorEntityNotFound = _errorPrefix + _entityName + " " + " blev ikke fundet.";
    private const string _infoPrefix = "Info: ";
    /// <summary>Informational message displayed after a successful delete.</summary>
    protected const string _infoDeleted = _entityName + " er slettet.";
    /// <summary>Informational message displayed after a successful add or save.</summary>
    protected const string _infoSaved = _entityName + " er gemt.";
    /// <summary>Dialog title used for delete confirmation.</summary>
    protected const string _confirmDeleteTitle = "Bekræft slet " + _entityName;
    /// <summary>Delete confirmation message body.</summary>
    protected string _confirmDelete = "Er du sikker på, at du vil slette " + _entityName + "?";
    private const int _infoMessageDuration = 3000;

    // ---------------------------------------------------------------------
    // Repository
    // ---------------------------------------------------------------------

    /// <summary>
    /// Backing repository used for CRUD persistence operations.
    /// </summary>
    protected readonly TRepos _repository;

    // ---------------------------------------------------------------------
    // Current entity under edit
    // ---------------------------------------------------------------------

    /// <summary>
    /// Backing field for <see cref="CurrentEntity"/>.
    /// </summary>
    protected TEntity? _currentEntity;

    /// <summary>
    /// The entity currently being added or edited. Setting this property triggers
    /// property change notifications and command state refresh.
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

    #region Command properties

    /// <summary>
    /// Command that creates and persists a new entity instance using <see cref="OnAddFormAsync"/>.
    /// Only enabled while in add mode (<see cref="IsAddMode"/>).
    /// </summary>
    public ICommand AddCommand { get; }

    /// <summary>
    /// Command that updates the existing <see cref="CurrentEntity"/> using
    /// <see cref="OnSaveFormAsync"/>. Enabled only when in edit mode.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Command that deletes the current entity after user confirmation.
    /// </summary>
    public ICommand DeleteCommand { get; }

    /// <summary>
    /// Command that resets the form (clears entity data and exits edit mode).
    /// </summary>
    public ICommand ResetCommand { get; }

    /// <summary>
    /// Alias for <see cref="ResetCommand"/>; intended for UX scenarios where cancel semantics are preferred.
    /// </summary>
    public ICommand CancelCommand { get; }
    #endregion

    #region Info / Error messages and state properties

    /// <summary>
    /// Backing field for <see cref="Error"/>.
    /// </summary>
    protected string? _error;

    /// <summary>
    /// Current error message (if any). Setting this toggles <see cref="HasError"/>.
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
    /// Indicates whether an error message is currently present.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(Error);

    /// <summary>
    /// Backing field for <see cref="InfoMessage"/>.
    /// </summary>
    protected string? _infoMessage;

    /// <summary>
    /// Transient informational message (e.g., save/delete confirmation). Automatically cleared
    /// after a short delay.
    /// </summary>
    public string? InfoMessage
    {
        get => _infoMessage;
        protected set
        {
            if (_infoMessage == value) return;
            _infoMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasInfoMessage));
            if (!string.IsNullOrEmpty(value))
                _ = AutoClearInfoMessageAsync();
        }
    }

    /// <summary>
    /// Indicates whether an informational message is currently displayed.
    /// </summary>
    public bool HasInfoMessage => !string.IsNullOrEmpty(InfoMessage);

    /// <summary>
    /// Backing field for <see cref="IsSaving"/>.
    /// </summary>
    protected bool _isSaving;

    /// <summary>
    /// True while an add/save/delete operation is executing; prevents concurrent command execution.
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        protected set { if (_isSaving == value) return; _isSaving = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    /// <summary>
    /// Backing field for <see cref="IsEditMode"/>.
    /// </summary>
    protected bool _isEditMode;

    /// <summary>
    /// True when editing an existing entity (as opposed to creating a new one).
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
    /// Convenience inverse of <see cref="IsEditMode"/>. True while in "add new entity" mode.
    /// </summary>
    public bool IsAddMode => !IsEditMode;
    #endregion

    // ---------------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------------

    /// <summary>
    /// Raised after a successful save (add or update) or delete. The argument value is:
    ///  - The saved entity (after add/update).
    ///  - <c>null</c> after delete (indicates removal).
    /// </summary>
    public event EventHandler<TEntity?>? EntitySaved;

    /// <summary>
    /// Initializes the CRUD base with an optional repository. If the supplied repository is
    /// <c>null</c>, the type is resolved from the application's service provider (DI container).
    /// </summary>
    /// <param name="repository">Repository instance or <c>null</c> to resolve from DI.</param>
    protected CrudViewModelBase(TRepos repository)
    {
        _repository = repository ?? App.HostInstance.Services.GetRequiredService<TRepos>();

        AddCommand = new RelayCommand(async () => await OnAddAsync(), CanAdd);
        SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
        DeleteCommand = new RelayCommand(async () => await OnDeleteAsync(), CanDelete);
        ResetCommand = new RelayCommand(async () => await OnResetAsync(), CanReset);
        CancelCommand = new RelayCommand(async () => await OnCancelAsync(), CanCancel);

        IsEditMode = false;
    }

    /// <summary>
    /// Loads an entity by identifier, populating the form via <see cref="OnLoadFormAsync(TEntity)"/>
    /// and entering edit mode if successful.
    /// </summary>
    /// <param name="id">Entity identifier.</param>
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

    #region CanXXX commands

    /// <summary>
    /// Base predicate for submission-related commands. Disallows action while saving or in error state.
    /// </summary>
    protected virtual bool CanSubmitCore() => !IsSaving && !HasError;

    /// <summary>Determines whether the Add operation can execute.</summary>
    protected virtual bool CanAdd() => CanSubmitCore() && IsAddMode;

    /// <summary>Determines whether the Save (update) operation can execute.</summary>
    protected virtual bool CanSave() => CanSubmitCore() && IsEditMode;

    /// <summary>Determines whether the Reset operation can execute.</summary>
    protected virtual bool CanReset() => IsEditMode && !IsSaving;

    /// <summary>Determines whether the Delete operation can execute.</summary>
    protected virtual bool CanDelete() => IsEditMode && !IsSaving;

    /// <summary>Determines whether the Cancel operation can execute.</summary>
    protected virtual bool CanCancel() => !IsSaving;
    #endregion

    #region OnXXX commands

    /// <summary>
    /// Executes the add workflow: gathers form data, persists it, raises <see cref="EntitySaved"/>,
    /// shows info, and resets the form.
    /// </summary>
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
        catch (Exception ex) { Error = ex.Message; }
        finally { IsSaving = false; RefreshCommandStates(); }
    }

    /// <summary>
    /// Executes the save/update workflow: applies form changes, updates repository,
    /// raises <see cref="EntitySaved"/>, shows info, and resets the form.
    /// </summary>
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
        catch (Exception ex) { Error = ex.Message; }
        finally { IsSaving = false; RefreshCommandStates(); }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Cancels current operation (alias to reset form).
    /// </summary>
    protected async Task OnCancelAsync() => await OnResetAsync();

    /// <summary>
    /// Executes the delete workflow with user confirmation; raises <see cref="EntitySaved"/>
    /// (passing null) and resets the form upon success.
    /// </summary>
    protected async Task OnDeleteAsync()
    {
        if (_currentEntity is null) return;

        if (MessageBox.Show(_confirmDelete, _confirmDeleteTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
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
        catch (Exception ex) { Error = ex.Message; }
        finally { IsSaving = false; RefreshCommandStates(); }
    }

    /// <summary>
    /// Resets form state and exits edit mode (clears current entity and errors).
    /// </summary>
    protected async Task OnResetAsync()
    {
        Error = null;
        _currentEntity = null;
        await OnResetFormAsync();
        IsEditMode = false;
        await Task.CompletedTask;
    }

    // Abstract hooks for derived view models:

    /// <summary>
    /// Called when the form should be cleared (e.g., after save/delete or initial setup).
    /// Implementations should reset bound fields to defaults.
    /// </summary>
    protected abstract Task OnResetFormAsync();

    /// <summary>
    /// Called to construct a new entity from current form field values for Add operations.
    /// </summary>
    protected abstract Task<TEntity> OnAddFormAsync();

    /// <summary>
    /// Called prior to persisting updates to <see cref="CurrentEntity"/> in Save operations.
    /// Implementations should transfer form field values into the entity instance.
    /// </summary>
    protected abstract Task OnSaveFormAsync();

    /// <summary>
    /// Called after an entity is loaded for editing in <see cref="LoadAsync(Guid)"/> allowing
    /// the derived class to populate form fields from the entity.
    /// </summary>
    /// <param name="entity">Loaded entity.</param>
    protected abstract Task OnLoadFormAsync(TEntity entity);
    #endregion

    #region Helpers

    /// <summary>
    /// Attempts to read a <see cref="Guid"/> identifier from a property named <c>Id</c> on
    /// the entity via reflection. Returns null if not present or incompatible.
    /// </summary>
    /// <param name="entity">Entity to inspect.</param>
    protected virtual Guid? GetEntityId(TEntity entity)
    {
        var prop = typeof(TEntity).GetProperty("Id");
        if (prop is null) return null;
        return prop.GetValue(entity) as Guid?;
    }

    /// <summary>
    /// Asynchronously clears <see cref="InfoMessage"/> after a predefined delay to avoid stale notifications.
    /// </summary>
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

    /// <summary>
    /// Requests re-evaluation of all command CanExecute states.
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
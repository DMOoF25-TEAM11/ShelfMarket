using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.UI.Commands;

namespace ShelfMarket.UI.ViewModels.Abstracts;

// CRUD base: single-entity form + commands
public abstract class CrudViewModelBase<TRepos, TEntity> : ModelBase
    where TRepos : notnull, IRepository<TEntity>
    where TEntity : class
{
    // Constants
    protected const string _entityName = nameof(TEntity);
    private const string _errorPrefix = "Fejl: ";
    protected const string _errorEntityNotFound = _errorPrefix + _entityName + " " + " blev ikke fundet.";
    private const string _infoPrefix = "Info: ";
    protected const string _infoDeleted = _entityName + " er slettet.";
    protected const string _infoSaved = _entityName + " er gemt.";
    protected const string _confirmDeleteTitle = "Bekræft slet " + _entityName;
    protected string _confirmDelete = "Er du sikker på, at du vil slette " + _entityName + "?";
    private const int _infoMessageDuration = 3000;

    // Repository
    protected readonly TRepos _repository;

    // Current entity
    protected TEntity? _currentEntity;
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
    // Commands
    public ICommand AddCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand CancelCommand { get; }
    #endregion

    #region Info / Error messages and state properties
    // Error / Info / State
    protected string? _error;
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
    public bool HasError => !string.IsNullOrEmpty(Error);

    protected string? _infoMessage;
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
    public bool HasInfoMessage => !string.IsNullOrEmpty(InfoMessage);

    protected bool _isSaving;
    public bool IsSaving
    {
        get => _isSaving;
        protected set { if (_isSaving == value) return; _isSaving = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    protected bool _isEditMode;
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
    public bool IsAddMode => !IsEditMode;
    #endregion

    // Event
    public event EventHandler<TEntity?>? EntitySaved;

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

    // Optional load by Id
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
    // CanXXX
    protected virtual bool CanSubmitCore() => !IsSaving && !HasError;
    protected virtual bool CanAdd() => CanSubmitCore() && IsAddMode;
    protected virtual bool CanSave() => CanSubmitCore() && IsEditMode;
    protected virtual bool CanReset() => IsEditMode && !IsSaving;
    protected virtual bool CanDelete() => IsEditMode && !IsSaving;
    protected virtual bool CanCancel() => !IsSaving;
    #endregion

    #region OnXXX commands
    // Command handlers
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

    protected async Task OnCancelAsync() => await OnResetAsync();

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

    protected async Task OnResetAsync()
    {
        Error = null;
        _currentEntity = null;
        await OnResetFormAsync();
        IsEditMode = false;
        await Task.CompletedTask;
    }

    // Abstracts
    protected abstract Task OnResetFormAsync();
    protected abstract Task<TEntity> OnAddFormAsync();
    protected abstract Task OnSaveFormAsync();
    protected abstract Task OnLoadFormAsync(TEntity entity);
    #endregion

    #region Helpers
    // Helpers
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
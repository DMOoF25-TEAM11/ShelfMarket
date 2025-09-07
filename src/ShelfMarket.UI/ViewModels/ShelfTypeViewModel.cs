using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public sealed class ShelfTypeViewModel : ViewModelBase<IShelfTypeRepository, ShelfType>
{
    private const string _entityName = "Reol Type";
    private const string _errorEntityNotFound = _errorPrefix + _entityName + " " + " blev ikke fundet.";

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    public event EventHandler<ShelfType>? ShelfTypeSaved;

    public ShelfTypeViewModel(IShelfTypeRepository? selected = null) : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTypeRepository>())
    {
        // Initialize commands and other properties here
    }

    #region Load handler
    public async Task LoadAsync(Guid id)
    {
        Error = null;
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity != null)
            {
                _currentId = entity.Id;
                Name = entity.Name;
                Description = entity.Description;
            }
            else
            {
                Error = _errorEntityNotFound;
            }
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

    #region CanXXX Command States
    #endregion

    #region Command Handlers
    protected override async Task OnAddAsync()
    {
        if (!CanAdd()) return;
        IsSaving = true;
        Error = null;
        var shelfType = new ShelfType(Name, Description);
        try
        {
            await _repository.AddAsync(shelfType);
            ShelfTypeSaved?.Invoke(this, shelfType);
            InfoMessage = $"{_entityName} er oprettet.";
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

    protected override Task OnSaveAsync()
    {
        throw new NotImplementedException();
    }


    protected override Task OnDeleteAsync()
    {
        throw new NotImplementedException();
    }

    protected override async Task OnResetAsync()
    {
        if (!CanReset()) return;
        Description = string.Empty;
        Name = string.Empty;
        Error = null;
        _currentId = Guid.Empty;
        await Task.CompletedTask;
    }
    #endregion

}

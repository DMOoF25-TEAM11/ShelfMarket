using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class ShelfViewModel : ViewModelBase<IShelfRepository, Shelf>
{
    /// <summary>
    /// The display name for the entity, used in UI messages.
    /// </summary>
    private new const string _entityName = "Reol";

    #region Form Fields
    private int _shelfNumber;
    public int ShelfNumber
    {
        get => _shelfNumber;
        set
        {
            if (_shelfNumber != value)
            {
                _shelfNumber = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private Guid _shelfTypeId;
    public Guid ShelfTypeId
    {
        get => _shelfTypeId;
        set
        {
            if (_shelfTypeId != value)
            {
                _shelfTypeId = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private DateTime? _selectedDate;
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate != value)
            {
                _selectedDate = value;
                OnPropertyChanged();
            }
        }
    }
    #endregion

    #region Dropdown Fields
    public ObservableCollection<ShelfType> ShelfTypes { get; private set; } = [];
    #endregion

    public ShelfViewModel(IShelfRepository? selected = null) : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfRepository>())
    {
        // Initialize with current month/year
        _selectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        _ = LoadShelfTypeOptionAsync();
    }

    #region Load handler

    private async Task LoadShelfTypeOptionAsync()
    {
        try
        {
            using var scope = App.HostInstance.Services.CreateScope();
            var shelfTypeRepository = scope.ServiceProvider.GetRequiredService<IShelfTypeRepository>();
            var shelfTypes = await shelfTypeRepository.GetAllAsync();
            ShelfTypes.Clear();
            foreach (var i in shelfTypes.OrderBy(i => i.Name))
            {
                ShelfTypes.Add(i);
            }
            OnPropertyChanged(nameof(ShelfTypes));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Fejl ved indlæsning af reoltyper: {ex.Message}";
        }
        finally
        {
            RefreshCommandStates();
        }
    }
    #endregion

    #region CanXXX Command States
    protected override bool CanAdd() =>
        base.CanAdd() &&
        ShelfNumber > 0 &&
        ShelfTypeId != Guid.Empty;

    protected override bool CanSave() =>
        base.CanSave() &&
        ShelfNumber > 0 &&
        ShelfTypeId != Guid.Empty &&
        (ShelfNumber != CurrentEntity?.Number || ShelfTypeId != CurrentEntity?.ShelfTypeId);

    #endregion

    #region Command Handlers
    protected override async Task<Shelf> OnAddFormAsync()
    {
        // Provide default values for locationX and locationY (e.g., 0)
        var entity = new Shelf(ShelfNumber, ShelfTypeId, 0, 0);
        await Task.CompletedTask;
        return entity;
    }

    protected override async Task OnResetFormAsync()
    {
        CurrentEntity = null;
        ShelfNumber = 0;
        ShelfTypeId = Guid.Empty;
        await Task.CompletedTask;
    }

    protected override async Task OnSaveFormAsync()
    {
        if (CurrentEntity == null)
        {
            ErrorMessage = _errorEntityNotFound;
            return;
        }
        CurrentEntity.Number = ShelfNumber;
        CurrentEntity.ShelfTypeId = ShelfTypeId;
        await Task.CompletedTask;
    }

    protected override Task OnLoadFormAsync(Shelf entity)
    {
        ShelfNumber = entity.Number;
        ShelfTypeId = entity.ShelfTypeId;
        return Task.CompletedTask;
    }
    #endregion
}

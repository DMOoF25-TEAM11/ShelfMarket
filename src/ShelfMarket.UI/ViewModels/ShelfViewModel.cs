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
                _ = UpdateTenantInfoAsync();
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

    private DateTime _selectedDate;
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate != value)
            {
                _selectedDate = value;
                OnPropertyChanged();
                _ = UpdateTenantInfoAsync();
            }
        }
    }

    private int _currentMonth;
    private int _currentYear;
    public int CurrentMonth
    {
        get => _currentMonth;
        set
        {
            if (_currentMonth != value)
            {
                _currentMonth = value;
                OnPropertyChanged();
                SelectedDate = new DateTime(CurrentYear, _currentMonth, 1);
            }
        }
    }

    public int CurrentYear
    {
        get => _currentYear;
        set
        {
            if (_currentYear != value)
            {
                _currentYear = value;
                OnPropertyChanged();
                SelectedDate = new DateTime(_currentYear, CurrentMonth, 1);
            }
        }
    }
    #endregion

    #region Dropdown Fields
    public ObservableCollection<ShelfType> ShelfTypes { get; private set; } = [];
    #endregion

    #region Tenant Info
    private string? _tenantFirstName;
    public string? TenantFirstName
    {
        get => _tenantFirstName;
        private set { if (_tenantFirstName == value) return; _tenantFirstName = value; OnPropertyChanged(); }
    }

    private string? _tenantLastName;
    public string? TenantLastName
    {
        get => _tenantLastName;
        private set { if (_tenantLastName == value) return; _tenantLastName = value; OnPropertyChanged(); }
    }

    private DateTime? _contractEndDate;
    public DateTime? ContractEndDate
    {
        get => _contractEndDate;
        private set { if (_contractEndDate == value) return; _contractEndDate = value; OnPropertyChanged(); }
    }
    #endregion

    public ShelfViewModel(IShelfRepository? selected = null) : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfRepository>())
    {
        // Initialize with current month/year
        _currentMonth = DateTime.Now.Month;
        _currentYear = DateTime.Now.Year;
        SelectedDate = new DateTime(_currentYear, _currentMonth, 1);
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

    private async Task UpdateTenantInfoAsync()
    {
        try
        {
            using var scope = App.HostInstance.Services.CreateScope();
            var contractRepo = scope.ServiceProvider.GetRequiredService<IShelfTenantContractRepository>();
            var lineRepo = scope.ServiceProvider.GetRequiredService<IShelfTenantContractLineRepository>();
            var tenantRepo = scope.ServiceProvider.GetRequiredService<IShelfTenantRepository>();
            var shelfRepo = scope.ServiceProvider.GetRequiredService<IShelfRepository>();

            var shelves = await shelfRepo.GetAllAsync();
            var shelf = shelves.FirstOrDefault(s => s.Number == ShelfNumber);
            if (shelf == null || shelf.Id == null) { TenantFirstName = null; TenantLastName = null; ContractEndDate = null; return; }
            var shelfId = shelf.Id.Value;

            var lines = await lineRepo.GetAllAsync();
            var linesForShelf = lines.Where(l => l.ShelfId == shelfId);
            if (!linesForShelf.Any()) { TenantFirstName = null; TenantLastName = null; ContractEndDate = null; return; }

            var contracts = await contractRepo.GetAllAsync();
            var month = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
            var activeLine =
                (from l in linesForShelf
                 join c in contracts on l.ShelfTenantContractId equals c.Id!.Value
                 where c.StartDate.Date <= month.Date && c.EndDate.Date >= month.Date && c.CancelledAt == null
                 orderby c.StartDate descending
                 select new { Line = l, Contract = c }).FirstOrDefault();

            if (activeLine == null)
            {
                TenantFirstName = null;
                TenantLastName = null;
                ContractEndDate = null;
                return;
            }

            var tenant = await tenantRepo.GetByIdAsync(activeLine.Contract.ShelfTenantId);
            TenantFirstName = tenant?.FirstName;
            TenantLastName = tenant?.LastName;
            ContractEndDate = activeLine.Contract.EndDate;
        }
        catch
        {
            TenantFirstName = null;
            TenantLastName = null;
            ContractEndDate = null;
        }
    }
}

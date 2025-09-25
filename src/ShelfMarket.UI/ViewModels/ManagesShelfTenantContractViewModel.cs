using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class ManagesShelfTenantContractViewModel : ManagesListViewModelBase<IShelfTenantContractRepository, ShelfTenantContract>
{
    public ManagesShelfTenantContractViewModel(IShelfTenantContractRepository? selected = null)
        : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTenantContractRepository>())
    {
        // Refresh list after add/save/delete and notify when a new contract was created
        EntitySaved += async (_, entity) =>
        {
            await RefreshAsync();

            if (_lastOperationWasAdd && entity is ShelfTenantContract c)
            {
                _lastOperationWasAdd = false;
                OnContractCreated(c.ContractNumber);
            }
        };

        CancelContractCommand = new RelayCommand(async () => await CancelContractAsync(), CanCancelContract);

        // Initial load
        _ = RefreshAsync();
    }

    public ManagesShelfTenantContractViewModel(ShelfTenant shelfTenant, IShelfTenantContractRepository? selected = null)
        : this(selected)
    {
        // Pre-select the given entity
        ShelfTenant = shelfTenant;
        ShelfTenantDisplayName = $"{shelfTenant.FirstName} {shelfTenant.LastName}";
    }

    public event EventHandler<ContractCreatedEventArgs>? ContractCreated;

    // Call this right after persisting a new contract to the DB
    private void OnContractCreated(int contractId)
        => ContractCreated?.Invoke(this, new ContractCreatedEventArgs(contractId));

    private ShelfTenant _shelfTenant = null!;
    public ShelfTenant ShelfTenant
    {
        get => _shelfTenant;
        set { if (_shelfTenant == value) return; _shelfTenant = value; OnPropertyChanged(); }
    }

    private string _shelfTenantDisplayName = string.Empty;
    public string ShelfTenantDisplayName
    {
        get => _shelfTenantDisplayName;
        private set { if (_shelfTenantDisplayName == value) return; _shelfTenantDisplayName = value; OnPropertyChanged(); }
    }

    public bool IsContractCancelled => SelectedItem?.CancelledAt != null;
    public bool IsContractActive => SelectedItem?.CancelledAt == null;

    // Track whether the last operation was an Add (not Save)
    private bool _lastOperationWasAdd;

    #region Form Fields
    private DateTime _startDate;
    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            var first = FirstOfMonth(value);
            if (_startDate == first) return;
            _startDate = first;
            OnPropertyChanged();
            // Ensure EndDate is always after StartDate
            if (EndDate <= _startDate)
                EndDate = _startDate.AddMonths(1);
            RefreshCommandStates();
        }
    }

    private DateTime _endDate;
    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            var coerced = value <= StartDate ? StartDate.AddMonths(1) : value;
            if (_endDate == coerced) return;
            _endDate = coerced;
            OnPropertyChanged();
            RefreshCommandStates();
        }
    }

    private Guid _shelfTenantId;
    public Guid ShelfTenantId
    {
        get => _shelfTenantId;
        set { if (_shelfTenantId == value) return; _shelfTenantId = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private DateTime? _cancelledAt;
    public DateTime? CancelledAt
    {
        get => _cancelledAt;
        set { if (_cancelledAt == value) return; _cancelledAt = value; OnPropertyChanged(); RefreshCommandStates(); }
    }
    #endregion

    // Cancel contract command
    public ICommand CancelContractCommand { get; }

    private bool CanCancelContract()
        => SelectedItem is { CancelledAt: null };

    protected override async Task<IEnumerable<ShelfTenantContract>> LoadItemsAsync()
    {
        var all = await _repository.GetAllAsync();
        return all.OrderBy(i => i.StartDate);
    }

    // Ensure command re-evaluates on selection change
    protected override async Task OnSelectedItemChangedAsync(ShelfTenantContract? item)
    {
        await base.OnSelectedItemChangedAsync(item);

        // Sync form state when selection changes
        CurrentEntity = item;
        IsEditMode = item != null;
        if (item != null)
            await OnLoadFormAsync(item);
        else
            await OnResetFormAsync();

        OnPropertyChanged(nameof(IsContractCancelled));
        OnPropertyChanged(nameof(IsContractActive));
        RefreshCommandStates();
        (CancelContractCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    #region Onxxx command methods
    protected override Task<ShelfTenantContract> OnAddFormAsync()
    {
        _lastOperationWasAdd = true;

        var tenantId = ShelfTenant?.Id ?? Guid.Empty;
        if (tenantId == Guid.Empty && ShelfTenantId != Guid.Empty)
            tenantId = ShelfTenantId;

        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("A valid tenant must be selected before creating a contract.");

        // Normalize dates: Start = first of month, End = last day of month (and after Start)
        var start = FirstOfMonth(StartDate);
        var endMonth = EndDate <= start ? start.AddMonths(1) : EndDate;
        var end = EndOfMonth(endMonth);

        var entity = new ShelfTenantContract(
            shelfTenantId: tenantId,
            startDate: start,
            endDate: end,
            cancelledAt: CancelledAt
        );
        // reflect normalized StartDate in UI
        StartDate = start;
        EndDate = end;
        return Task.FromResult(entity);
    }

    protected override Task OnLoadFormAsync(ShelfTenantContract entity)
    {
        // Sync form fields when selecting an item
        CancelledAt = entity.CancelledAt;
        StartDate = FirstOfMonth(entity.StartDate);

        // Keep UI end date consistent with entity; entity already stores EOM
        EndDate = entity.EndDate <= entity.StartDate
            ? FirstOfMonth(entity.StartDate).AddMonths(1)
            : entity.EndDate;

        ShelfTenantId = entity.ShelfTenantId;
        return Task.CompletedTask;
    }

    protected override async Task OnResetFormAsync()
    {
        Error = string.Empty;
        CurrentEntity = null;
        SelectedItem = null;
        StartDate = FirstOfMonth(DateTime.Now);
        EndDate = StartDate.AddMonths(1); // keep EndDate after StartDate
        ShelfTenantId = ShelfTenant.Id ?? Guid.Empty;
        CancelledAt = null;
        await Task.CompletedTask;
    }

    protected override Task OnSaveFormAsync()
    {
        // Map form fields back to the entity before calling repository.UpdateAsync
        if (CurrentEntity is null)
            return Task.CompletedTask;

        // Normalize Start = first of month, End = last day of its month (and after Start)
        var start = FirstOfMonth(StartDate);
        var endMonth = EndDate <= start ? start.AddMonths(1) : EndDate;
        var end = EndOfMonth(endMonth);

        CurrentEntity.StartDate = start;
        CurrentEntity.EndDate = end;
        CurrentEntity.CancelledAt = CancelledAt;
        CurrentEntity.ShelfTenantId = ShelfTenantId;

        // Reflect normalized dates back to the form
        StartDate = start;
        EndDate = end;

        return Task.CompletedTask;
    }
    #endregion

    private async Task CancelContractAsync()
    {
        if (SelectedItem is null) return;

        try
        {
            Error = string.Empty;

            // Set cancellation date to today
            SelectedItem.CancelledAt = DateTime.Today;

            // Mirror into form field (if bound elsewhere)
            CancelledAt = SelectedItem.CancelledAt;

            // Persist and refresh (ContractNumber is identity; not updated)
            await _repository.UpdateAsync(SelectedItem);
            OnPropertyChanged(nameof(IsContractCancelled));
            OnPropertyChanged(nameof(IsContractActive));
            (CancelContractCommand as RelayCommand)?.RaiseCanExecuteChanged();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    // Helper: first/last day of the month for a given date
    private static DateTime FirstOfMonth(DateTime dt) => new DateTime(dt.Year, dt.Month, 1);
    private static DateTime EndOfMonth(DateTime dt) =>
        new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month));
}

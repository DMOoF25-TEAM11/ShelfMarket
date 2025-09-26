using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;
using ShelfMarket.UI.Views.UserControls;

namespace ShelfMarket.UI.ViewModels;

public class ManagesShelfTenantContractViewModel : ManagesListViewModelBase<IShelfTenantContractRepository, ShelfTenantContract>
{
    public ManagesShelfTenantContractViewModel(IShelfTenantContractRepository? selected = null)
        : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTenantContractRepository>())
    {
        EntitySaved += async (_, entity) =>
        {
            await RefreshAsync();

            if (_lastOperationWasAdd && entity is ShelfTenantContract c)
            {
                _lastOperationWasAdd = false;
                await OnContractCreatedAsync(c);
            }
        };

        CancelContractCommand = new RelayCommand(async () => await CancelContractAsync(), CanCancelContract);

        // Set initial form values
        StartDate = FirstOfMonth(DateTime.Now);
        EndDate = StartDate.AddMonths(1);

        _ = RefreshAsync();
    }

    public ManagesShelfTenantContractViewModel(ShelfTenant shelfTenant, IShelfTenantContractRepository? selected = null)
        : this(selected)
    {
        ShelfTenant = shelfTenant;
        ShelfTenantDisplayName = $"{shelfTenant.FirstName} {shelfTenant.LastName}";
    }

    #region Fields state
    private bool _lastOperationWasAdd;
    #endregion

    #region Properties
    private ShelfTenant _shelfTenant = null!;
    public ShelfTenant ShelfTenant
    {
        get => _shelfTenant;
        set
        {
            if (_shelfTenant == value) return;
            _shelfTenant = value;
            OnPropertyChanged();
            ShelfTenantId = _shelfTenant?.Id ?? Guid.Empty;
            _ = RefreshAsync();
        }
    }

    private string _shelfTenantDisplayName = string.Empty;
    public string ShelfTenantDisplayName
    {
        get => _shelfTenantDisplayName;
        private set { if (_shelfTenantDisplayName == value) return; _shelfTenantDisplayName = value; OnPropertyChanged(); }
    }

    public bool IsContractCancelled => SelectedItem?.CancelledAt != null;
    public bool IsContractActive => SelectedItem?.CancelledAt == null;
    #endregion

    #region Command Properties
    public ICommand CancelContractCommand { get; }
    #endregion

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
        set
        {
            if (_shelfTenantId == value) return;
            _shelfTenantId = value;
            OnPropertyChanged();
            RefreshCommandStates();
            _ = RefreshAsync();
        }
    }

    private DateTime? _cancelledAt;
    public DateTime? CancelledAt
    {
        get => _cancelledAt;
        set { if (_cancelledAt == value) return; _cancelledAt = value; OnPropertyChanged(); RefreshCommandStates(); }
    }
    #endregion

    #region Load handlers
    protected override async Task<IEnumerable<ShelfTenantContract>> LoadItemsAsync()
    {
        var all = await _repository.GetAllAsync();
        var tenantId = ShelfTenant?.Id ?? ShelfTenantId;
        if (tenantId != Guid.Empty)
            return all.Where(i => i.ShelfTenantId == tenantId)
                      .OrderBy(i => i.StartDate);

        return [];
    }
    #endregion

    #region CanXXX Command States
    private bool CanCancelContract() => SelectedItem is { CancelledAt: null };

    protected override bool CanAdd() =>
        base.CanAdd()
        && (ShelfTenant?.Id ?? ShelfTenantId) != Guid.Empty
        && StartDate >= FirstOfMonth(DateTime.Now)
        && EndDate > StartDate;

    protected override bool CanSave() =>
        base.CanSave()
        && StartDate >= FirstOfMonth(DateTime.Now)
        && EndDate > StartDate;
    #endregion

    #region OnXXX Command
    protected override async Task OnSelectedItemChangedAsync(ShelfTenantContract? item)
    {
        await base.OnSelectedItemChangedAsync(item);

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

    protected override Task<ShelfTenantContract> OnAddFormAsync()
    {
        _lastOperationWasAdd = true;

        var tenantId = ShelfTenant?.Id ?? Guid.Empty;
        if (tenantId == Guid.Empty && ShelfTenantId != Guid.Empty)
            tenantId = ShelfTenantId;

        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("A valid tenant must be selected before creating a contract.");

        var start = FirstOfMonth(StartDate);
        var endMonth = EndDate <= start ? start.AddMonths(1) : EndDate;
        var end = EndOfMonth(endMonth);

        var entity = new ShelfTenantContract(
            shelfTenantId: tenantId,
            startDate: start,
            endDate: end,
            cancelledAt: CancelledAt
        );
        StartDate = start;
        EndDate = end;

        return Task.FromResult(entity);
    }

    protected override Task OnLoadFormAsync(ShelfTenantContract entity)
    {
        CancelledAt = entity.CancelledAt;
        StartDate = FirstOfMonth(entity.StartDate);
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
        EndDate = StartDate.AddMonths(1);
        ShelfTenantId = ShelfTenant?.Id ?? Guid.Empty;
        CancelledAt = null;
        await Task.CompletedTask;
    }

    protected override Task OnSaveFormAsync()
    {
        if (CurrentEntity is null)
            return Task.CompletedTask;

        var start = FirstOfMonth(StartDate);
        var endMonth = EndDate <= start ? start.AddMonths(1) : EndDate;
        var end = EndOfMonth(endMonth);

        CurrentEntity.StartDate = start;
        CurrentEntity.EndDate = end;
        CurrentEntity.CancelledAt = CancelledAt;
        CurrentEntity.ShelfTenantId = ShelfTenantId;

        StartDate = start;
        EndDate = end;

        return Task.CompletedTask;
    }

    private async Task CancelContractAsync()
    {
        if (SelectedItem is null) return;

        try
        {
            Error = string.Empty;
            SelectedItem.CancelledAt = DateTime.Today;
            CancelledAt = SelectedItem.CancelledAt;

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

    private async Task OnContractCreatedAsync(ShelfTenantContract shelfTenantContract)
    {
        if (App.Current?.MainWindow is not MainWindow mw)
        {
            return;
        }
        var host = mw.MainContent;
        if (host == null) return;
        host.Content = new ManageShelfTenantContractLineView(shelfTenantContract);

        await Task.CompletedTask;
        return;
    }
    #endregion

    #region Helpers
    private static DateTime FirstOfMonth(DateTime dt) => new DateTime(dt.Year, dt.Month, 1);
    private static DateTime EndOfMonth(DateTime dt) =>
        new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month));
    #endregion
}

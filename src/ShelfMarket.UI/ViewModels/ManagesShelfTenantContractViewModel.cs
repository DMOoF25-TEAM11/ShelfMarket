using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class ManagesShelfTenantContractViewModel : ManagesListViewModelBase<IShelfTenantContractRepository, ShelfTenantContract>
{
    public ManagesShelfTenantContractViewModel(IShelfTenantContractRepository? selected = null)
        : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTenantContractRepository>())
    {
        // Refresh list after add/save/delete
        EntitySaved += async (_, __) => await RefreshAsync();
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

    private ShelfTenant _shelfTenant = null!;

    public ShelfTenant ShelfTenant
    {
        get { return _shelfTenant; }
        set { _shelfTenant = value; }
    }


    private string _shelfTenantDisplayName = string.Empty;

    public string ShelfTenantDisplayName
    {
        get { return _shelfTenantDisplayName; }
        private set { _shelfTenantDisplayName = value; }
    }

    #region Form Fields
    private DateTime _startDate;

    public DateTime StartDate
    {
        get { return _startDate; }
        set { _startDate = value; }
    }

    private DateTime _endDate;
    public DateTime EndDate
    {
        get { return _endDate; }
        set { _endDate = value; }
    }

    private Guid _shelfTenantId;

    public Guid ShelfTenantId
    {
        get { return _shelfTenantId; }
        set { _shelfTenantId = value; }
    }

    private uint? _contractNumber;
    public uint? ContractNumber
    {
        get { return _contractNumber; }
        set { _contractNumber = value; }
    }

    private DateTime? _cancelledAt;
    public DateTime? CancelledAt
    {
        get { return _cancelledAt; }
        set { _cancelledAt = value; }
    }
    #endregion


    protected override async Task<IEnumerable<ShelfTenantContract>> LoadItemsAsync()
    {
        var all = await _repository.GetAllAsync();
        return all.OrderBy(i => i.StartDate);
    }

    #region Canxxx command methods

    #endregion

    #region Onxxx command methods
    protected override Task<ShelfTenantContract> OnAddFormAsync()
    {
        ShelfTenantContract entity = new ShelfTenantContract(
                shelfTenantId: ShelfTenantId,
                contractNumber: ContractNumber,
                startDate: StartDate,
                endDate: EndDate,
                cancelledAt: CancelledAt
            );
        return Task.FromResult(entity);
    }

    protected override Task OnLoadFormAsync(ShelfTenantContract entity)
    {
        return Task.CompletedTask;
    }

    protected override async Task OnResetFormAsync()
    {
        Error = string.Empty;
        CurrentEntity = null;
        SelectedItem = null;
        StartDate = DateTime.Now;
        EndDate = DateTime.Now;
        ShelfTenantId = ShelfTenant.Id ?? Guid.Empty; // Fix: handle nullable Id
        ContractNumber = null;
        CancelledAt = null;
        await Task.CompletedTask;
    }

    protected override Task OnSaveFormAsync()
    {
        return Task.CompletedTask;
    }
    #endregion
}

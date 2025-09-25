using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class ManagesShelfTenantContractLineViewModel : ManagesListViewModelBase<IShelfTenantContractLineRepository, ShelfTenantContractLine>
{
    public ManagesShelfTenantContractLineViewModel(IShelfTenantContractLineRepository? selected = null)
        : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTenantContractLineRepository>())
    {
        // Refresh list after add/save/delete
        EntitySaved += async (_, __) => await RefreshAsync();

        // Initial load
        _ = RefreshAsync();
    }

    public ManagesShelfTenantContractLineViewModel(ShelfTenantContract contract, IShelfTenantContractLineRepository? selected = null)
        : this(selected)
    {
        ParentContract = contract;
        ShelfTenantContractId = contract.Id ?? Guid.Empty;
    }

    private ShelfTenantContract? _parentContract;
    public ShelfTenantContract? ParentContract
    {
        get => _parentContract;
        private set { _parentContract = value; OnPropertyChanged(); }
    }

    #region Form Fields
    private Guid _shelfTenantContractId = Guid.Empty;
    public Guid ShelfTenantContractId
    {
        get => _shelfTenantContractId;
        set { if (_shelfTenantContractId == value) return; _shelfTenantContractId = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private Guid _shelfId = Guid.Empty;
    public Guid ShelfId
    {
        get => _shelfId;
        set { if (_shelfId == value) return; _shelfId = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private uint _lineNumber;
    public uint LineNumber
    {
        get => _lineNumber;
        set { if (_lineNumber == value) return; _lineNumber = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private decimal _pricePerMonth;
    public decimal PricePerMonth
    {
        get => _pricePerMonth;
        set { if (_pricePerMonth == value) return; _pricePerMonth = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private decimal? _pricePerMonthSpecial;
    public decimal? PricePerMonthSpecial
    {
        get => _pricePerMonthSpecial;
        set { if (_pricePerMonthSpecial == value) return; _pricePerMonthSpecial = value; OnPropertyChanged(); RefreshCommandStates(); }
    }
    #endregion

    #region List Load
    protected override async Task<IEnumerable<ShelfTenantContractLine>> LoadItemsAsync()
    {
        var all = await _repository.GetAllAsync();

        // If a parent contract is set, show only its lines
        if (ParentContract?.Id is Guid cid && cid != Guid.Empty)
            return all.Where(l => l.ShelfTenantContractId == cid)
                      .OrderBy(l => l.LineNumber);

        return all.OrderBy(l => l.ShelfTenantContractId).ThenBy(l => l.LineNumber);
    }
    #endregion

    #region CanXXX Command States
    protected override bool CanAdd() =>
        base.CanAdd()
        && IsValidGuid(ShelfTenantContractId)
        && IsValidGuid(ShelfId)
        && IsValidLineNumber(LineNumber)
        && IsValidPrice(PricePerMonth)
        && IsValidSpecialPrice(PricePerMonthSpecial);

    protected override bool CanSave() =>
        base.CanSave()
        && CurrentEntity != null
        && IsValidGuid(ShelfTenantContractId)
        && IsValidGuid(ShelfId)
        && IsValidLineNumber(LineNumber)
        && IsValidPrice(PricePerMonth)
        && IsValidSpecialPrice(PricePerMonthSpecial)
        && (
            ShelfTenantContractId != CurrentEntity.ShelfTenantContractId ||
            ShelfId != CurrentEntity.ShelfId ||
            LineNumber != CurrentEntity.LineNumber ||
            PricePerMonth != CurrentEntity.PricePerMonth ||
            PricePerMonthSpecial != CurrentEntity.PricePerMonthSpecial
        );

    protected override bool CanDelete() => base.CanDelete() && CurrentEntity != null;
    #endregion

    #region Command Handlers
    protected override async Task OnResetFormAsync()
    {
        Error = string.Empty;
        CurrentEntity = null;
        SelectedItem = null;

        ShelfTenantContractId = ParentContract?.Id ?? Guid.Empty;
        ShelfId = Guid.Empty;
        LineNumber = 0;
        PricePerMonth = 0m;
        PricePerMonthSpecial = null;

        await Task.CompletedTask;
    }

    protected override Task<ShelfTenantContractLine> OnAddFormAsync()
    {
        var entity = new ShelfTenantContractLine(
            shelfTenantContractId: ShelfTenantContractId,
            shelfId: ShelfId,
            lineNumber: LineNumber,
            pricePerMonth: PricePerMonth,
            pricePerMonthSpecial: PricePerMonthSpecial
        );

        return Task.FromResult(entity);
    }

    protected override async Task OnSaveFormAsync()
    {
        if (CurrentEntity == null)
        {
            Error = _errorEntityNotFound;
            await Task.CompletedTask;
            return;
        }

        CurrentEntity.ShelfTenantContractId = ShelfTenantContractId;
        CurrentEntity.ShelfId = ShelfId;
        CurrentEntity.LineNumber = LineNumber;
        CurrentEntity.PricePerMonth = PricePerMonth;
        CurrentEntity.PricePerMonthSpecial = PricePerMonthSpecial;

        await Task.CompletedTask;
    }

    protected override Task OnLoadFormAsync(ShelfTenantContractLine entity)
    {
        CurrentEntity = entity;

        ShelfTenantContractId = entity.ShelfTenantContractId;
        ShelfId = entity.ShelfId;
        LineNumber = entity.LineNumber;
        PricePerMonth = entity.PricePerMonth;
        PricePerMonthSpecial = entity.PricePerMonthSpecial;

        return Task.CompletedTask;
    }
    #endregion

    protected override void RefreshCommandStates()
    {
        base.RefreshCommandStates();
    }

    #region Validation
    private static bool IsValidGuid(Guid id) => id != Guid.Empty;
    private static bool IsValidLineNumber(uint n) => n > 0;
    private static bool IsValidPrice(decimal p) => p > 0m;
    private static bool IsValidSpecialPrice(decimal? p) => !p.HasValue || p.Value >= 0m;
    #endregion
}

using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Application.DTOs;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class ManagesShelfTenantContractLineViewModel : ManagesListViewModelBase<IShelfTenantContractLineRepository, ShelfTenantContractLine>
{
    public ManagesShelfTenantContractLineViewModel(IShelfTenantContractLineRepository? selected = null)
        : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTenantContractLineRepository>())
    {
        _shelfRepository = App.HostInstance.Services.GetRequiredService<IShelfRepository>();

        // Refresh list after add/save/delete
        EntitySaved += async (_, __) => await RefreshAndUpdateNextLineNumberAsync();

        // Initial loads
        _ = RefreshAndUpdateNextLineNumberAsync();
        if (ParentContract != null) // avoid loading shelves before ParentContract is set
            _ = LoadShelfOptionsAsync();
    }

    public ManagesShelfTenantContractLineViewModel(ShelfTenantContract contract, IShelfTenantContractLineRepository? selected = null)
        : this(selected)
    {
        ParentContract = contract;
        ShelfTenantContractId = contract.Id ?? Guid.Empty;

        // Now that ParentContract is set, load shelves for its date range
        _ = LoadShelfOptionsAsync();
    }

    #region Fields state
    #endregion

    #region Properties
    private readonly IShelfRepository _shelfRepository;

    private ShelfTenantContract? _parentContract;
    public ShelfTenantContract? ParentContract
    {
        get => _parentContract;
        private set { _parentContract = value; OnPropertyChanged(); }
    }
    #endregion

    #region Form Fields
    // Options for dropdowns
    public ObservableCollection<AvailableShelf> Shelves { get; } = new();

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

    private int _lineNumber = 1;
    public int LineNumber
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

    #region Load Handler
    protected override async Task<IEnumerable<ShelfTenantContractLine>> LoadItemsAsync()
    {
        var all = await _repository.GetAllAsync();

        // If a parent contract is set, show only its lines
        if (ParentContract?.Id is Guid cid && cid != Guid.Empty)
            return all.Where(l => l.ShelfTenantContractId == cid)
                      .OrderBy(l => l.LineNumber);

        return all.OrderBy(l => l.ShelfTenantContractId).ThenBy(l => l.LineNumber);
    }

    private async Task LoadShelfOptionsAsync()
    {
        try
        {
            if (ParentContract == null)
            {
                Error = "Parent contract is not set.";
                return;
            }

            // Guard against SQL datetime overflow before calling repository
            var sqlMin = System.Data.SqlTypes.SqlDateTime.MinValue.Value.Date;
            if (ParentContract.StartDate.Date < sqlMin || ParentContract.EndDate.Date < sqlMin)
            {
                Error = $"Kontraktdatoer skal være >= {sqlMin:yyyy-MM-dd}. Datoen var {ParentContract.StartDate:yyyy-MM-dd} - {ParentContract.EndDate:yyyy-MM-dd}.";
                return;
            }

            Shelves.Clear();

            var availableShelves = await _shelfRepository.GetAvailableShelves(ParentContract.StartDate, ParentContract.EndDate);
            foreach (var shelf in availableShelves)
            {
                Shelves.Add(shelf);
            }
            OnPropertyChanged(nameof(Shelves));
        }
        catch (Exception ex)
        {
            Error = $"Kunne ikke indlæse reoler: {ex.Message}";
        }
        finally
        {
            RefreshCommandStates();
        }
        await Task.CompletedTask;
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
            PricePerMonth != CurrentEntity.PricePerMonth ||
            PricePerMonthSpecial != CurrentEntity.PricePerMonthSpecial ||
            LineNumber != CurrentEntity.LineNumber
        );

    protected override bool CanDelete() => base.CanDelete() && CurrentEntity != null;
    #endregion

    #region OnXXX Command
    protected override async Task OnResetFormAsync()
    {
        Error = string.Empty;
        CurrentEntity = null;
        SelectedItem = null;

        ShelfTenantContractId = ParentContract?.Id ?? Guid.Empty;
        ShelfId = Guid.Empty;

        // Highest LineNumber among current contract's items (defaults to 1 if none)
        LineNumber = Items
            .Where(l => l.ShelfTenantContractId == ShelfTenantContractId)
            .Select(l => l.LineNumber)
            .DefaultIfEmpty(0)
            .Max() + 1;

        PricePerMonth = 0m;
        PricePerMonthSpecial = null;

        await Task.CompletedTask;
    }

    protected override Task<ShelfTenantContractLine> OnAddFormAsync()
    {
        var entity = new ShelfTenantContractLine
        {
            ShelfTenantContractId = ShelfTenantContractId,
            ShelfId = ShelfId,
            LineNumber = LineNumber,
            PricePerMonth = PricePerMonth,
            PricePerMonthSpecial = PricePerMonthSpecial ?? 0m
        };

        var shelfToRemove = Shelves.FirstOrDefault(s => s.Id == ShelfId);
        if (shelfToRemove != null)
            _ = Shelves.Remove(shelfToRemove); // remove selected shelf from options

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
        CurrentEntity.PricePerMonthSpecial = PricePerMonthSpecial ?? 0m;

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

    //protected override void RefreshCommandStates()
    //{
    //    base.RefreshCommandStates();
    //}

    #region Helpers
    private async Task RefreshAndUpdateNextLineNumberAsync()
    {
        await RefreshAsync();
        LineNumber = Items
            .Where(l => l.ShelfTenantContractId == ShelfTenantContractId)
            .Select(l => l.LineNumber)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }
    #endregion

    #region Validation
    private static bool IsValidGuid(Guid id) => id != Guid.Empty;
    private static bool IsValidLineNumber(int n) => n > 0;
    private static bool IsValidPrice(decimal p) => p > 0m;
    private static bool IsValidSpecialPrice(decimal? p) => !p.HasValue || p.Value >= 0m;
    #endregion
}

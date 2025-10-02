using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

/// <summary>
/// ViewModel for the Shelf Info popup. Responsible for presenting and updating
/// shelf-specific information without UI concerns (separation of concerns).
/// Currently manages the shelf type selection for a given shelf number.
/// </summary>
public sealed class ShelfInfoWindowViewModel : ModelBase
{
    private bool _isInitialized;

    private int _shelfNumber;
    public int ShelfNumber
    {
        get => _shelfNumber;
        private set { if (_shelfNumber == value) return; _shelfNumber = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ShelfType> ShelfTypes { get; } = new();

    private Guid _selectedShelfTypeId;
    public Guid SelectedShelfTypeId
    {
        get => _selectedShelfTypeId;
        set
        {
            if (_selectedShelfTypeId == value) return;
            _selectedShelfTypeId = value;
            OnPropertyChanged();
            if (_isInitialized)
            {
                // Fire and forget – UI should stay responsive. Errors can be surfaced later if needed.
                _ = SaveSelectedShelfTypeAsync();
            }
        }
    }

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

    private DateTime? _selectedDate;
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        private set { if (_selectedDate == value) return; _selectedDate = value; OnPropertyChanged(); }
    }

    private DateTime? _contractEndDate;
    public DateTime? ContractEndDate
    {
        get => _contractEndDate;
        private set { if (_contractEndDate == value) return; _contractEndDate = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Loads shelf and shelf type data and prepares bindings for the specified shelf number.
    /// </summary>
    public async Task InitializeAsync(int shelfNumber, DateTime? selectedMonth = null, CancellationToken cancellationToken = default)
    {
        _isInitialized = false;
        ShelfNumber = shelfNumber;

        SelectedDate = selectedMonth == null
            ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
            : new DateTime(selectedMonth.Value.Year, selectedMonth.Value.Month, 1);

        using (var scope = App.HostInstance.Services.CreateScope())
        {
            var shelfTypeRepo = scope.ServiceProvider.GetRequiredService<IShelfTypeRepository>();
            var shelfRepo = scope.ServiceProvider.GetRequiredService<IShelfRepository>();

            // Load types
            var types = await shelfTypeRepo.GetAllAsync(cancellationToken);
            ShelfTypes.Clear();
            foreach (var t in types.OrderBy(t => t.Name))
            {
                ShelfTypes.Add(t);
            }
            OnPropertyChanged(nameof(ShelfTypes));

            // Load shelf and preselect its type
            var shelves = await shelfRepo.GetAllAsync(cancellationToken);
            var shelf = shelves.FirstOrDefault(s => s.Number == shelfNumber);
            if (shelf != null)
            {
                _selectedShelfTypeId = shelf.ShelfTypeId;
                OnPropertyChanged(nameof(SelectedShelfTypeId));
            }
        }

        // Load tenant/contract for the selected month
        await LoadTenantAndContractAsync(cancellationToken);

        _isInitialized = true;
    }

    public async Task UpdateSelectedMonthAsync(DateTime? selectedMonth, CancellationToken cancellationToken = default)
    {
        SelectedDate = selectedMonth?.Date == null
            ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
            : new DateTime(selectedMonth!.Value.Year, selectedMonth.Value.Month, 1);
        await LoadTenantAndContractAsync(cancellationToken);
    }

    private async Task SaveSelectedShelfTypeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = App.HostInstance.Services.CreateScope();
            var shelfRepo = scope.ServiceProvider.GetRequiredService<IShelfRepository>();
            // Fetch shelf, update type and persist
            var shelves = await shelfRepo.GetAllAsync(cancellationToken);
            var shelf = shelves.FirstOrDefault(s => s.Number == ShelfNumber);
            if (shelf is null) return;
            if (shelf.ShelfTypeId == _selectedShelfTypeId) return;
            shelf.ShelfTypeId = _selectedShelfTypeId;
            await shelfRepo.UpdateAsync(shelf, cancellationToken);
        }
        catch
        {
            // For now, ignore errors. Could expose an ErrorMessage property if needed.
        }
    }

    private async Task LoadTenantAndContractAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = App.HostInstance.Services.CreateScope();
            var shelfRepo = scope.ServiceProvider.GetRequiredService<IShelfRepository>();
            var contractRepo = scope.ServiceProvider.GetRequiredService<IShelfTenantContractRepository>();
            var lineRepo = scope.ServiceProvider.GetRequiredService<IShelfTenantContractLineRepository>();
            var tenantRepo = scope.ServiceProvider.GetRequiredService<IShelfTenantRepository>();

            var shelves = await shelfRepo.GetAllAsync(cancellationToken);
            var shelf = shelves.FirstOrDefault(s => s.Number == ShelfNumber);
            if (shelf is null || shelf.Id is null) { TenantFirstName = null; TenantLastName = null; ContractEndDate = null; return; }
            var shelfId = shelf.Id.Value;

            var lines = await lineRepo.GetAllAsync(cancellationToken);
            var linesForShelf = lines.Where(l => l.ShelfId == shelfId);
            if (!linesForShelf.Any()) { TenantFirstName = null; TenantLastName = null; ContractEndDate = null; return; }

            var contracts = await contractRepo.GetAllAsync(cancellationToken);

            var month = SelectedDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var activeLine =
                (from l in linesForShelf
                 join c in contracts on l.ShelfTenantContractId equals c.Id!.Value
                 where c.StartDate.Date <= month.Date && c.EndDate.Date >= month.Date && c.CancelledAt == null
                 orderby c.StartDate descending
                 select new { Line = l, Contract = c }).FirstOrDefault();

            if (activeLine is null)
            {
                TenantFirstName = null;
                TenantLastName = null;
                ContractEndDate = null;
                return;
            }

            var tenant = await tenantRepo.GetByIdAsync(activeLine.Contract.ShelfTenantId, cancellationToken);
            TenantFirstName = tenant?.FirstName;
            TenantLastName = tenant?.LastName;
            ContractEndDate = activeLine.Contract.EndDate;
        }
        catch
        {
            // Silently ignore for now
        }
    }
}



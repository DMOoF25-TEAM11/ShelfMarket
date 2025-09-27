using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;
using ShelfMarket.UI.Views.UserControls;

namespace ShelfMarket.UI.ViewModels;

/// <summary>
/// ViewModel for creating and maintaining shelf tenant contracts.
/// Lifetime/refresh notes:
/// - Registered as Scoped in DI (see App.xaml.cs). Each AddContractWindow resolves it from
///   its own IServiceScope to ensure an isolated DbContext per popup.
/// - After a successful Add, we refresh our own list and, if the Tenants page (Lejere)
///   is currently displayed, we also refresh that VM so the UI updates immediately.
/// - Load guards for null/empty tenant id prevent freezes when the view first opens.
/// </summary>
public class AddContractWindowViewModel : ManagesListViewModelBase<IShelfTenantContractRepository, ShelfTenantContract>
{
    public AddContractWindowViewModel(IShelfTenantContractRepository? selected = null)
        : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTenantContractRepository>())
    {

        // Notify when a new contract was created
        EntitySaved += async (_, entity) =>
        {
            if (_lastOperationWasAdd && entity is ShelfTenantContract c)
            {
                _lastOperationWasAdd = false;

                // Refresh the list in this VM
                await RefreshAsync();

                // If Lejere (ManagesShelfTenantView) is currently displayed, refresh its VM too
                if (System.Windows.Application.Current.MainWindow is MainWindow mw)
                {
                    var host = mw.MainContent;
                    if (host?.Content is ManagesShelfTenantView tenantsView &&
                        tenantsView.DataContext is ManagesShelfTenantViewModel tenantsVm)
                    {
                        await tenantsVm.RefreshAsync();
                    }
                }

                // Close the popup window after successful creation
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
                if (overlay != null)
                {
                    overlay.Visibility = Visibility.Collapsed;
                }

            }
        };

        CancelContractCommand = new RelayCommand(async () => await CancelContractAsync(), CanCancelContract);

        // Custom cancel command that closes the window
        CustomCancelCommand = new RelayCommand(async () =>
        {
            await OnResetAsync();

            // Close the popup window
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
            if (overlay != null)
            {
                overlay.Visibility = Visibility.Collapsed;
            }
        });

        // Set initial form values
        StartDate = FirstOfMonth(DateTime.Now);
        EndDate = StartDate.AddMonths(1);

        _ = RefreshAsync();
    }

    public AddContractWindowViewModel(ShelfTenant shelfTenant, IShelfTenantContractRepository? selected = null)
        : this(selected)
    {
        ShelfTenant = shelfTenant;
        ShelfTenantDisplayName = $"{shelfTenant.FirstName} {shelfTenant.LastName}";
    }

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

    private bool _lastOperationWasAdd;


    #region Form Fields
    // Tenant fields for contract creation
    private string _firstName = string.Empty;
    public string FirstName
    {
        get => _firstName;
        set { if (_firstName == value) return; _firstName = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private string _lastName = string.Empty;
    public string LastName
    {
        get => _lastName;
        set { if (_lastName == value) return; _lastName = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private string _address = string.Empty;
    public string Address
    {
        get => _address;
        set { if (_address == value) return; _address = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private string _postalCode = string.Empty;
    public string PostalCode
    {
        get => _postalCode;
        set { if (_postalCode == value) return; _postalCode = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private string _city = string.Empty;
    public string City
    {
        get => _city;
        set { if (_city == value) return; _city = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set { if (_email == value) return; _email = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    private string _phoneNumber = string.Empty;
    public string PhoneNumber
    {
        get => _phoneNumber;
        set { if (_phoneNumber == value) return; _phoneNumber = value; OnPropertyChanged(); RefreshCommandStates(); }
    }

    // Contract fields
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


    // Cancel contract command
    public ICommand CancelContractCommand { get; }

    // Custom cancel command that closes the window
    public ICommand CustomCancelCommand { get; }

    private bool CanCancelContract()
        => SelectedItem is { CancelledAt: null };

    protected override async Task<IEnumerable<ShelfTenantContract>> LoadItemsAsync()
    {
        // Guard: ShelfTenant may not be set yet when the VM is constructed
        var effectiveTenantId = ShelfTenant?.Id ?? ShelfTenantId;
        if (effectiveTenantId == Guid.Empty)
            return Enumerable.Empty<ShelfTenantContract>();

        var all = await _repository.GetAllAsync();

        return all
            .Where(i => i.ShelfTenantId == effectiveTenantId)
            .OrderBy(i => i.ContractNumber);
    }




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

    #region Onxxx command methods
    protected override async Task<ShelfTenantContract> OnAddFormAsync()
    {
        _lastOperationWasAdd = true;

        var tenantId = ShelfTenant?.Id ?? Guid.Empty;
        if (tenantId == Guid.Empty && ShelfTenantId != Guid.Empty)
            tenantId = ShelfTenantId;

        // If no tenant is selected, create a new one from form data
        if (tenantId == Guid.Empty)
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
                throw new InvalidOperationException("Fornavn og efternavn skal udfyldes.");

            // Create new tenant
            using var scope = App.HostInstance.Services.CreateScope();
            var tenantRepository = scope.ServiceProvider.GetRequiredService<IShelfTenantRepository>();

            var newTenant = new ShelfTenant(FirstName, LastName, Address, PostalCode, City, Email, PhoneNumber);

            await tenantRepository.AddAsync(newTenant);
            tenantId = newTenant.Id ?? Guid.Empty;

            // Update the ShelfTenant property
            ShelfTenant = newTenant;
            ShelfTenantDisplayName = $"{FirstName} {LastName}";
        }

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
        return entity;
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

        // Reset tenant fields
        FirstName = string.Empty;
        LastName = string.Empty;
        Address = string.Empty;
        PostalCode = string.Empty;
        City = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;

        // Reset contract fields
        StartDate = FirstOfMonth(DateTime.Now);

        EndDate = StartDate.AddMonths(1); // keep EndDate after StartDate

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

    #region Command validation
    protected override bool CanAdd() =>
        base.CanAdd() &&
        !string.IsNullOrWhiteSpace(FirstName) &&
        !string.IsNullOrWhiteSpace(LastName);
    #endregion



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

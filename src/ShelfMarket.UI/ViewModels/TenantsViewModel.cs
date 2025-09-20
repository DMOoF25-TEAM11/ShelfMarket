using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;
using ShelfMarket.UI.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ShelfMarket.UI.ViewModels;

public class TenantsViewModel : ViewModelBase<ITenantRepository, ShelfTenant>
{
    #region Form Fields
    private Guid _tenantId;
    public Guid TenantId
    {
        get => _tenantId;
        set
        {
            if (_tenantId != value)
            {
                _tenantId = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private string _firstName = string.Empty;
    public string FirstName
    {
        get => _firstName;
        set
        {
            if (_firstName != value)
            {
                _firstName = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private string _lastName = string.Empty;
    public string LastName
    {
        get => _lastName;
        set
        {
            if (_lastName != value)
            {
                _lastName = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private string _address = string.Empty;
    public string Address
    {
        get => _address;
        set
        {
            if (_address != value)
            {
                _address = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private string _postalCode = string.Empty;
    public string PostalCode
    {
        get => _postalCode;
        set
        {
            if (_postalCode != value)
            {
                _postalCode = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private string _city = string.Empty;
    public string City
    {
        get => _city;
        set
        {
            if (_city != value)
            {
                _city = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private string _phoneNumber = string.Empty;
    public string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            if (_phoneNumber != value)
            {
                _phoneNumber = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private ShelfTenant? _selectedTenant;
    public ShelfTenant? SelectedTenant
    {
        get => _selectedTenant;
        set
        {
            if (!ReferenceEquals(_selectedTenant, value))
            {
                _selectedTenant = value;
                OnPropertyChanged();

                if (value != null)
                {
                    _ = OnLoadFormAsync(value);
                    CurrentEntity = value;
                    IsEditMode = true;
                }
                else
                {
                    IsEditMode = false;
                }

                RefreshCommandStates();
            }
        }
    }
    #endregion

    public TenantsViewModel()
        : this(App.HostInstance.Services.GetRequiredService<ITenantRepository>())
    {
    }

    #region Extra Commands
    public ICommand ResetFormCommand { get; }
    public ICommand RefreshCommand { get; }

    // Skal bruges til at åbne TenantContracts vindue
    //private readonly relaycommand _movetotenantcontractscommand;
    //public icommand movetotenantcontractscommand => _movetotenantcontractscommand;
    #endregion

    public TenantsViewModel(ITenantRepository tenantRepository)
        : base(tenantRepository)
    {
        _tenantRepository = tenantRepository;

        // Skal bruges til at åbne TenantContracts vindue
        //_moveToTenantContractsCommand = new RelayCommand(async () => await OpenTenantContractsAsync());

        ResetFormCommand = new RelayCommand(async () => await OnResetFormAsync(),
            () => IsEditMode ||
                  !string.IsNullOrWhiteSpace(FirstName) ||
                  !string.IsNullOrWhiteSpace(LastName) ||
                  !string.IsNullOrWhiteSpace(Address) ||
                  !string.IsNullOrWhiteSpace(PostalCode) ||
                  !string.IsNullOrWhiteSpace(City) ||
                  !string.IsNullOrWhiteSpace(Email));

        RefreshCommand = new RelayCommand(async () => await LoadTenantOptionAsync());

        EntitySaved += async (_, __) => await LoadTenantOptionAsync();

        _ = LoadTenantOptionAsync();
    }

    #region Dropdown Fields
    private readonly ITenantRepository _tenantRepository;
    public ObservableCollection<ShelfTenant> Tenants { get; private set; } = [];
    #endregion

    #region Command Overrides
    protected override bool CanAdd() =>
        base.CanAdd() &&
        !string.IsNullOrWhiteSpace(FirstName) &&
        !string.IsNullOrWhiteSpace(LastName) &&
        !string.IsNullOrWhiteSpace(Address) &&
        !string.IsNullOrWhiteSpace(PostalCode) &&
        !string.IsNullOrWhiteSpace(City) &&
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(PhoneNumber);

    protected override bool CanSave() =>
        base.CanSave() && CurrentEntity != null &&
        (!string.IsNullOrWhiteSpace(FirstName) && FirstName != CurrentEntity.FirstName ||
         !string.IsNullOrWhiteSpace(LastName) && LastName != CurrentEntity.LastName ||
         !string.IsNullOrWhiteSpace(Address) && Address != CurrentEntity.Address ||
         !string.IsNullOrWhiteSpace(PostalCode) && PostalCode != CurrentEntity.PostalCode ||
         !string.IsNullOrWhiteSpace(City) && City != CurrentEntity.City ||
         !string.IsNullOrWhiteSpace(Email) && Email != CurrentEntity.Email ||
         !string.IsNullOrWhiteSpace(PhoneNumber) && PhoneNumber != CurrentEntity.PhoneNumber);

    protected override bool CanDelete() =>
        base.CanDelete() && CurrentEntity != null;
    #endregion

    #region Load handler
    private async Task LoadTenantOptionAsync()
    {
        try
        {
            var tenant = await _tenantRepository.GetAllAsync();
            Tenants.Clear();
            foreach (var i in tenant.OrderBy(i => i.FirstName))
            {
                Tenants.Add(i);
            }
            OnPropertyChanged(nameof(Tenants));
        }
        catch (Exception ex)
        {
            Error = $"Fejl ved indlæsning af lejer: {ex.Message}";
        }
        finally
        {
            RefreshCommandStates();
        }
    }
    #endregion

    #region Command Handlers (form mapping only)
    protected override Task<ShelfTenant> OnAddFormAsync()
    {
        var entity = new ShelfTenant(
            firstName: FirstName,
            lastName: LastName,
            address: Address,
            postalCode: PostalCode,
            city: City,
            email: Email,
            phoneNumber: PhoneNumber
        );

        TenantId = entity.Id;
        return Task.FromResult(entity);
    }

    protected override Task OnResetFormAsync()
    {
        CurrentEntity = null;
        TenantId = Guid.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        Address = string.Empty;
        PostalCode = string.Empty;
        City = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        return Task.CompletedTask;
    }

    protected override Task OnSaveFormAsync()
    {
        if (CurrentEntity == null)
        {
            Error = _errorEntityNotFound;
            return Task.CompletedTask;
        }

        CurrentEntity.UpdateContact(FirstName, LastName, Email, PhoneNumber);
        CurrentEntity.UpdateAddress(Address, PostalCode, City);

        return Task.CompletedTask;
    }
    protected override Task OnLoadFormAsync(ShelfTenant entity)
    {
        CurrentEntity = entity;

        TenantId = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Address = entity.Address;
        PostalCode = entity.PostalCode;
        City = entity.City;
        Email = entity.Email;
        PhoneNumber = entity.PhoneNumber;
        return Task.CompletedTask;
    }

    // Skal bruges til at åbne TenantContracts vindue
    //private async Task OpenTenantContractsAsync()
    //{
    //    // Create the Page and its VM (replace with DI if you prefer)
    //    var page = new ManagesShelfTanentContractView
    //    {
    //        DataContext = new ManagesShelfTanentContractViewModel()
    //    };

    //    // Host the Page inside a Window (no Application.Current needed)
    //    var frame = new System.Windows.Controls.Frame
    //    {
    //        NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden,
    //        Content = page
    //    };

    //    var win = new System.Windows.Window
    //    {
    //        Title = page.Title,
    //        Content = frame,
    //        Width = 1000,
    //        Height = 700,
    //        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
    //    };

    //    win.Show(); // or ShowDialog();
    //}

    #endregion
}

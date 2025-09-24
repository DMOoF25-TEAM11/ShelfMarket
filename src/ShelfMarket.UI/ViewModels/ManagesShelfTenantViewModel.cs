using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;
using ShelfMarket.UI.Views;

namespace ShelfMarket.UI.ViewModels;

public class ManagesShelfTenantViewModel : ManagesListViewModelBase<IShelfTenantRepository, ShelfTenant>
{
    public ManagesShelfTenantViewModel(IShelfTenantRepository? selected = null)
        : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTenantRepository>())
    {
        OnNavigateToTenantContractsCommand = new RelayCommand(OnNavigateToTenantContractsExecute, CanNavigateToTenantContracts);

        // Refresh list after add/save/delete
        EntitySaved += async (_, __) => await RefreshAsync();

        // Initial load
        _ = RefreshAsync();
    }

    #region Form Fields
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

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        set { if (_status == value) return; _status = value; OnPropertyChanged(); RefreshCommandStates(); }
    }
    #endregion

    #region Commands
    public ICommand OnNavigateToTenantContractsCommand { get; }
    #endregion

    #region List Load
    protected override async Task<IEnumerable<ShelfTenant>> LoadItemsAsync()
    {
        var all = await _repository.GetAllAsync();
        return all.OrderBy(i => i.FirstName);
    }
    #endregion

    #region CanXXX Command States
    protected override bool CanAdd() =>
        base.CanAdd()
        && IsValidFirstName(FirstName)
        && IsValidLastName(LastName)
        && IsValidAddress(Address)
        && IsValidPostalCode(PostalCode)
        && IsValidCity(City)
        && IsValidEmail(Email)
        && IsValidPhone(PhoneNumber);

    protected override bool CanSave() =>
        base.CanSave() && CurrentEntity != null
        && IsValidFirstName(FirstName)
        && IsValidLastName(LastName)
        && IsValidAddress(Address)
        && IsValidPostalCode(PostalCode)
        && IsValidCity(City)
        && IsValidEmail(Email)
        && IsValidPhone(PhoneNumber)
        && (
            FirstName != CurrentEntity.FirstName ||
            LastName != CurrentEntity.LastName ||
            Address != CurrentEntity.Address ||
            PostalCode != CurrentEntity.PostalCode ||
            City != CurrentEntity.City ||
            Email != CurrentEntity.Email ||
            PhoneNumber != CurrentEntity.PhoneNumber
        );

    protected override bool CanDelete() => base.CanDelete() && CurrentEntity != null;

    protected bool CanNavigateToTenantContracts() => CurrentEntity != null;
    #endregion

    #region Command Handlers
    private void OnNavigateToTenantContractsExecute() => _ = OnNavigateToShelfTenantContractsAsync();

    protected override async Task OnResetFormAsync()
    {
        Error = string.Empty;
        CurrentEntity = null;
        SelectedItem = null;
        FirstName = string.Empty;
        LastName = string.Empty;
        Address = string.Empty;
        PostalCode = string.Empty;
        City = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        Status = string.Empty;
        await Task.CompletedTask;
    }

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
        return Task.FromResult(entity);
    }

    protected override async Task OnSaveFormAsync()
    {
        if (CurrentEntity == null)
        {
            Error = _errorEntityNotFound;
            await Task.CompletedTask;
            return; // Add return to prevent dereferencing null
        }

        CurrentEntity.UpdateContact(FirstName, LastName, Email, PhoneNumber);
        CurrentEntity.UpdateAddress(Address, PostalCode, City);
        await Task.CompletedTask;
    }

    protected override Task OnLoadFormAsync(ShelfTenant entity)
    {
        CurrentEntity = entity;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Address = entity.Address;
        PostalCode = entity.PostalCode;
        City = entity.City;
        Email = entity.Email;
        PhoneNumber = entity.PhoneNumber;
        return Task.CompletedTask;
    }

    private async Task OnNavigateToShelfTenantContractsAsync()
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mw && CurrentEntity != null)
        {
            mw.MainContent.Content = new ManagesShelfTenantContractView(CurrentEntity);
        }
        await Task.CompletedTask;
    }
    #endregion

    protected override void RefreshCommandStates()
    {
        base.RefreshCommandStates();
        if (OnNavigateToTenantContractsCommand is RelayCommand rc)
            rc.RaiseCanExecuteChanged();
    }

    #region Validation (regex helpers)
    private static readonly Regex NameRegex = new(
        @"^[\p{L}\p{M}'\- ]{2,100}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.NonBacktracking);

    private static readonly Regex AddressRegex = new(
        @"^[\p{L}\p{M}\p{N}\s\.,'\-/#]{5,200}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.NonBacktracking);

    private static readonly Regex PostalCodeRegex = new(
        @"^\d{4}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.NonBacktracking);

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.NonBacktracking);

    private static readonly Regex PhoneRegex = new(
        @"^[+\d][\d\s\-]{7,14}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.NonBacktracking);

    public static bool IsValidFirstName(string? s) => !string.IsNullOrWhiteSpace(s) && NameRegex.IsMatch(s!);
    public static bool IsValidLastName(string? s) => !string.IsNullOrWhiteSpace(s) && NameRegex.IsMatch(s!);
    public static bool IsValidAddress(string? s) => !string.IsNullOrWhiteSpace(s) && AddressRegex.IsMatch(s!);
    public static bool IsValidPostalCode(string? s) => !string.IsNullOrWhiteSpace(s) && PostalCodeRegex.IsMatch(s!);
    public static bool IsValidCity(string? s) => !string.IsNullOrWhiteSpace(s) && NameRegex.IsMatch(s!);
    public static bool IsValidEmail(string? s) => !string.IsNullOrWhiteSpace(s) && EmailRegex.IsMatch(s!);
    public static bool IsValidPhone(string? s) => !string.IsNullOrWhiteSpace(s) && PhoneRegex.IsMatch(s!);
    #endregion
}

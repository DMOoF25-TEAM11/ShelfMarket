using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Domain.Enums;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public sealed class LoginRoleViewModel : ModelBase
{
    private readonly IPrivilegeService _privileges;

    public ObservableCollection<PrivilegeLevel> Levels { get; } =
        new(new[] { PrivilegeLevel.Guest, PrivilegeLevel.User, PrivilegeLevel.Admin });

    private PrivilegeLevel _selectedLevel = PrivilegeLevel.Guest;
    public PrivilegeLevel SelectedLevel
    {
        get => _selectedLevel;
        set { if (Set(ref _selectedLevel, value)) RefreshStates(); }
    }

    private string? _password;
    public string? Password
    {
        get => _password;
        set { if (Set(ref _password, value)) RefreshStates(); }
    }

    private string? _error;
    public string? Error
    {
        get => _error;
        private set
        {
            if (Set(ref _error, value))
                OnPropertyChanged(nameof(HasError));
        }
    }
    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    public LoginRoleViewModel()
    {
        _privileges = App.HostInstance.Services.GetRequiredService<IPrivilegeService>();
        ConfirmCommand = new RelayCommand(OnConfirm, CanConfirm);
        CancelCommand = new RelayCommand(OnCancel);
    }

    private bool CanConfirm()
        => SelectedLevel == PrivilegeLevel.Guest || !string.IsNullOrWhiteSpace(Password);

    private void OnConfirm()
    {
        Error = null;
        var ok = _privileges.SignIn(SelectedLevel, Password);
        if (!ok)
        {
            Error = "Invalid password.";
            return;
        }
        Password = string.Empty;
    }

    private void OnCancel()
    {
        Password = string.Empty;
        Error = null;
        SelectedLevel = _privileges.CurrentLevel;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);

        return true;
    }

    private void RefreshStates()
    {
        if (ConfirmCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
    }
}
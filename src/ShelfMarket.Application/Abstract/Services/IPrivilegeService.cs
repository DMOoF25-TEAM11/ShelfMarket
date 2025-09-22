using ShelfMarket.Domain.Enums;

namespace ShelfMarket.Application.Abstract.Services;

public interface IPrivilegeService
{
    PrivilegeLevel CurrentLevel { get; }
    bool SignIn(PrivilegeLevel level, string? password);
    void SignOut();
    bool CanAccess(PrivilegeLevel required);

    event EventHandler? CurrentLevelChanged;
}

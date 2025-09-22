using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Domain.Enums;

namespace ShelfMarket.Application.Services;

public sealed class PrivilegeService : IPrivilegeService
{
    public PrivilegeLevel CurrentLevel { get; private set; } = PrivilegeLevel.Guest;

    public bool SignIn(PrivilegeLevel level, string? password)
    {
        bool ok = level switch
        {
            PrivilegeLevel.Admin => password == "1234",
            PrivilegeLevel.User => password == "1234",
            PrivilegeLevel.Guest => true,
            _ => false
        };

        if (ok) CurrentLevel = level;
        return ok;
    }

    public void SignOut() => CurrentLevel = PrivilegeLevel.Guest;

    public bool CanAccess(PrivilegeLevel required)
    {
        return required switch
        {
            PrivilegeLevel.Admin => CurrentLevel == PrivilegeLevel.Admin,
            PrivilegeLevel.User => CurrentLevel is PrivilegeLevel.User or PrivilegeLevel.Admin,
            PrivilegeLevel.Guest => true,
            _ => false
        };
    }
}
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Domain.Enums;

namespace ShelfMarket.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="IPrivilegeService"/> used to manage the
/// current session's <see cref="PrivilegeLevel"/>.
/// </summary>
/// <remarks>
/// This implementation is intentionally simple and NOT secure:
///  - Credentials are hard‑coded (demo / placeholder only).
///  - No hashing, throttling, auditing, or external identity integration.
/// Replace with a proper authentication/authorization mechanism for production.
/// Thread-safety: Not thread-safe; assumed to be used on a single UI thread / scoped context.
/// </remarks>
public sealed class PrivilegeService : IPrivilegeService
{
    /// <inheritdoc />
    public PrivilegeLevel CurrentLevel { get; private set; } = PrivilegeLevel.Guest;

    /// <summary>
    /// Raised when <see cref="CurrentLevel"/> changes as a result of a successful sign-in
    /// or a call to <see cref="SignOut"/>.
    /// </summary>
    public event EventHandler? CurrentLevelChanged;

    /// <summary>
    /// Attempts to elevate the current privilege level to <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The target privilege level.</param>
    /// <param name="password">Password required for non-guest levels (demo: "1234").</param>
    /// <returns>
    /// <c>true</c> if credentials (where applicable) are valid and the level was changed
    /// (or already at requested level); otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Hard-coded password logic is for demonstration only. Both <see cref="PrivilegeLevel.User"/>
    /// and <see cref="PrivilegeLevel.Admin"/> require the same placeholder password.
    /// Event <see cref="CurrentLevelChanged"/> is only raised when the level actually changes.
    /// </remarks>
    public bool SignIn(PrivilegeLevel level, string? password)
    {
        bool ok = level switch
        {
            PrivilegeLevel.Admin => password == "1234",
            PrivilegeLevel.User => password == "1234",
            PrivilegeLevel.Guest => true,
            _ => false
        };

        if (ok && CurrentLevel != level)
        {
            CurrentLevel = level;
            CurrentLevelChanged?.Invoke(this, EventArgs.Empty);
        }
        return ok;
    }

    /// <summary>
    /// Reverts the current privilege level back to <see cref="PrivilegeLevel.Guest"/>.
    /// </summary>
    /// <remarks>
    /// If already at <see cref="PrivilegeLevel.Guest"/>, no action is taken and no event is raised.
    /// </remarks>
    public void SignOut()
    {
        if (CurrentLevel != PrivilegeLevel.Guest)
        {
            CurrentLevel = PrivilegeLevel.Guest;
            CurrentLevelChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Determines whether the current privilege level meets or exceeds
    /// the <paramref name="required"/> level.
    /// </summary>
    /// <param name="required">The minimum required privilege level.</param>
    /// <returns>
    /// <c>true</c> if access is permitted; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Access logic:
    ///  - Admin required: only Admin passes.
    ///  - User required: User or Admin pass.
    ///  - Guest required: always passes.
    /// </remarks>
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
using ShelfMarket.Domain.Enums;

namespace ShelfMarket.Application.Abstract.Services;

/// <summary>
/// Provides the current authenticated <see cref="PrivilegeLevel"/> and
/// helper methods to elevate or revoke privileges at runtime.
/// </summary>
/// <remarks>
/// Intended as a lightweight in-memory privilege / role gate used by UI
/// components (e.g. visibility converters, command guards) and application
/// services to check whether an operation is permitted.
/// This abstraction deliberately avoids persisting credentials or performing
/// complex authentication flows; those concerns should be handled by higher‑level
/// security / identity layers.
/// </remarks>
public interface IPrivilegeService
{
    /// <summary>
    /// Gets the current effective privilege level for the running session / context.
    /// Defaults typically to <see cref="PrivilegeLevel.Guest"/> until a successful
    /// call to <see cref="SignIn(PrivilegeLevel, string?)"/> elevates it.
    /// </summary>
    PrivilegeLevel CurrentLevel { get; }

    /// <summary>
    /// Attempts to sign in (elevate) to the specified <paramref name="level"/>.
    /// </summary>
    /// <param name="level">
    /// Target privilege level to acquire (e.g. <see cref="PrivilegeLevel.User"/> or <see cref="PrivilegeLevel.Admin"/>).
    /// </param>
    /// <param name="password">
    /// Optional password / secret associated with the requested level. Implementations may:
    ///  - Ignore for non-sensitive levels.
    ///  - Validate against configuration / secure store / external provider for higher levels.
    /// </param>
    /// <returns>
    /// <c>true</c> if elevation succeeds and <see cref="CurrentLevel"/> is updated;
    /// otherwise <c>false</c> (no change to <see cref="CurrentLevel"/>).
    /// </returns>
    /// <remarks>
    /// Implementations should raise <see cref="CurrentLevelChanged"/> only when a successful
    /// elevation occurs. They should avoid partial elevation (all-or-nothing).
    /// </remarks>
    bool SignIn(PrivilegeLevel level, string? password);

    /// <summary>
    /// Signs out (revokes) the current elevated privileges, restoring the baseline level
    /// (commonly <see cref="PrivilegeLevel.Guest"/>). If already at baseline, this is a no-op.
    /// </summary>
    /// <remarks>
    /// Implementations should raise <see cref="CurrentLevelChanged"/> only if the level actually changes.
    /// </remarks>
    void SignOut();

    /// <summary>
    /// Determines whether the current <see cref="CurrentLevel"/> satisfies or exceeds the
    /// <paramref name="required"/> level.
    /// </summary>
    /// <param name="required">The minimum privilege necessary to perform an operation.</param>
    /// <returns>
    /// <c>true</c> if <see cref="CurrentLevel"/> is greater than or equal to <paramref name="required"/>;
    /// otherwise <c>false</c>.
    /// </returns>
    bool CanAccess(PrivilegeLevel required);

    /// <summary>
    /// Occurs when <see cref="CurrentLevel"/> changes due to a successful sign-in or sign-out.
    /// </summary>
    event EventHandler? CurrentLevelChanged;
}

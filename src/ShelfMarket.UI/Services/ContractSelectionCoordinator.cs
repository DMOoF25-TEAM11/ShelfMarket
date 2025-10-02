using System;
using System.Collections.Generic;
using System.Linq;
using ShelfMarket.Domain.Entities;

namespace ShelfMarket.UI.Services;

/// <summary>
/// Coordinates the temporary hand-off between the contract creation popup
/// and the shelf overview when the user needs to pick shelves for a contract.
/// Acts as an in-memory mediator shared through DI so both views stay decoupled.
/// </summary>
public sealed class ContractSelectionCoordinator
{
    private readonly object _gate = new();

    /// <summary>
    /// Raised when a selection flow should start inside <see cref="ShelfView"/>.
    /// </summary>
    public event EventHandler<ContractShelfSelectionRequestedEventArgs>? SelectionRequested;

    /// <summary>
    /// Raised when ShelfView has collected the requested shelves.
    /// </summary>
    public event EventHandler<ContractShelfSelectionCompletedEventArgs>? SelectionCompleted;

    /// <summary>
    /// Raised if the selection is aborted (e.g. user cancels in ShelfView).
    /// </summary>
    public event EventHandler? SelectionCancelled;

    /// <summary>
    /// Gets the currently active selection request, if any.
    /// </summary>
    public ContractShelfSelectionContext? ActiveContext { get; private set; }

    public bool HasActiveRequest
    {
        get
        {
            lock (_gate)
            {
                return ActiveContext is not null;
            }
        }
    }

    /// <summary>
    /// Begins a new selection flow. Any existing request is overwritten.
    /// </summary>
    public void RequestSelection(ContractShelfSelectionContext context)
    {
        lock (_gate)
        {
            ActiveContext = context;
        }

        SelectionRequested?.Invoke(this, new ContractShelfSelectionRequestedEventArgs(context));
    }

    /// <summary>
    /// Completes the active selection and notifies listeners.
    /// </summary>
    public void CompleteSelection(IReadOnlyList<Shelf> shelves)
    {
        ContractShelfSelectionContext? context;
        lock (_gate)
        {
            context = ActiveContext;
            ActiveContext = null;
        }

        if (context is null)
            return;

        SelectionCompleted?.Invoke(
            this,
            new ContractShelfSelectionCompletedEventArgs(
                context,
                shelves.Select(s => new ContractSelectedShelf(s.Id ?? Guid.Empty, s.Number)).ToList()));
    }

    /// <summary>
    /// Cancels the active selection request, if one exists.
    /// </summary>
    public void CancelSelection()
    {
        var hadRequest = HasActiveRequest;
        lock (_gate)
        {
            ActiveContext = null;
        }

        if (hadRequest)
        {
            SelectionCancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}

public sealed record ContractShelfSelectionContext(
    int RequiredShelfCount,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyCollection<ContractSelectedShelf>? PreselectedShelves = null);

public sealed record ContractSelectedShelf(Guid ShelfId, int ShelfNumber, Guid ShelfTypeId = default);

public sealed class ContractShelfSelectionRequestedEventArgs : EventArgs
{
    public ContractShelfSelectionRequestedEventArgs(ContractShelfSelectionContext context)
    {
        Context = context;
    }

    public ContractShelfSelectionContext Context { get; }
}

public sealed class ContractShelfSelectionCompletedEventArgs : EventArgs
{
    public ContractShelfSelectionCompletedEventArgs(
        ContractShelfSelectionContext context,
        IReadOnlyList<ContractSelectedShelf> shelves)
    {
        Context = context;
        Shelves = shelves;
    }

    public ContractShelfSelectionContext Context { get; }

    public IReadOnlyList<ContractSelectedShelf> Shelves { get; }
}

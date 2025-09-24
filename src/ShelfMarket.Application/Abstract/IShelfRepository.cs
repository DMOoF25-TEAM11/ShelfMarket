using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract;

public interface IShelfRepository : IRepository<Shelf>
{
    /// <summary>
    /// Returns true if no shelf occupies the given (x,y).
    /// Any business rules about locked areas should also be honoured here.
    /// UI and services rely on this check to decide if a move is valid before persisting.
    /// </summary>
    Task<bool> IsLocationFree(int locationX, int locationY, CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class SalesLineRepository : Repository<SalesReceiptLine>, ISalesLineRepository
{
    public SalesLineRepository(ShelfMarketDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SalesReceiptLine>> GetLinesByReceiptIdAsync(Guid receiptId)
    {
        return await _context.SalesLines
            .AsNoTracking()
            .Where(sl => sl.SalesReceiptId == receiptId)
            .ToListAsync();
    }
}

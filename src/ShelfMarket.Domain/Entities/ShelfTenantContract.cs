namespace ShelfMarket.Domain.Entities;

public class ShelfTenantContract
{
    public Guid? Id { get; set; }
    public Guid ShelfTenantId { get; set; } = Guid.Empty;

    // DB-generated INT IDENTITY
    public int ContractNumber { get; private set; }

    public DateTime StartDate { get; set; } /* Year and month only */
    public DateTime EndDate { get; set; }   /* Year and month only */
    public DateTime? CancelledAt { get; set; }

    public ShelfTenantContract() { }

    public ShelfTenantContract(Guid shelfTenantId, DateTime startDate, DateTime endDate, DateTime? cancelledAt = null)
    {
        ShelfTenantId = shelfTenantId;
        StartDate = startDate;
        EndDate = endDate;
        CancelledAt = cancelledAt;
    }
}

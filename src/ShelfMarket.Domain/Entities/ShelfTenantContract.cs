namespace ShelfMarket.Domain.Entities;

public class ShelfTenantContract
{
    public Guid? Id { get; set; }
    public Guid ShelfTenantId { get; set; } = Guid.Empty;
    public uint ContractNumber { get; set; }
    public DateTime StartDate { get; set; } /* Year and month only */
    public DateTime EndDate { get; set; } /* Year and month only */
    public DateTime? CancelledAt { get; set; }

    public ShelfTenantContract()
    {

    }

    public ShelfTenantContract(Guid shelfTenantId, uint contractNumber, DateTime startDate, DateTime endDate, DateTime? cancelledAt = null)
    {
        ShelfTenantId = shelfTenantId;
        ContractNumber = contractNumber;
        StartDate = startDate;
        EndDate = endDate;
        CancelledAt = cancelledAt;
    }
}

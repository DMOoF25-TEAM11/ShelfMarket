using ShelfMarket.Domain.Entities;

namespace ShelfMarket.UI.ViewModels;

public class ManagesShelfTenantContractListItemViewModel
{
    public Guid? Id { get; }
    public string ShelfTenantIdDisplayName { get; }
    public uint ContractNumber { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public ManagesShelfTenantContractListItemViewModel(ShelfTenantContract contact)
    {
        //ShelfTenant shelfTenant = 
        Id = contact.Id;
        ContractNumber = contact.ContractNumber;
        StartDate = contact.StartDate;
        EndDate = contact.EndDate;
        ShelfTenantIdDisplayName = "Name showing not implemented";
    }
}

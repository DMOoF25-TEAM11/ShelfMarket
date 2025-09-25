using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI;
using ShelfMarket.UI.ViewModels.Abstracts;

public class ManagesShelfTanentContractLineViewModel : ViewModelBase<IShelfTenantContractLineRepository, ShelfTenantContractLine>
{
    public ManagesShelfTanentContractLineViewModel(IShelfTenantContractLineRepository? selected = null)
        : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTenantContractLineRepository>())
    {
    }

    public int ContractId { get; set; }

    protected override Task<ShelfTenantContractLine> OnAddFormAsync()
    {
        throw new NotImplementedException();
    }

    protected override Task OnLoadFormAsync(ShelfTenantContractLine entity)
    {
        throw new NotImplementedException();
    }

    protected override Task OnResetFormAsync()
    {
        throw new NotImplementedException();
    }

    protected override Task OnSaveFormAsync()
    {
        throw new NotImplementedException();
    }

}
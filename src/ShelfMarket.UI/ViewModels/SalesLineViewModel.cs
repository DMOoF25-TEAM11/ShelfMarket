using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class SalesLineViewModel : ViewModelBase<ISalesLineRepository, SalesLine>
{
    public SalesLineViewModel(ISalesLineRepository? selected = null) : base(selected ?? App.HostInstance.Services.GetRequiredService<ISalesLineRepository>())
    {
    }

    protected override Task<SalesLine> OnAddFormAsync()
    {
        throw new NotImplementedException();
    }

    protected override Task OnLoadFormAsync(SalesLine entity)
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

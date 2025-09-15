using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class ManagesShelfTanentContractViewModel : ViewModelBase<IShelfTenantContractRepository, ShelfTenantContract>
{
    private static DateTime ToYearMonth(DateTime value) => new DateTime(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

    #region Form Fields
    private DateTime _startDate;

    public DateTime StartDate
    {
        get { return _startDate; }
        set
        {
            var normalized = ToYearMonth(value);
            if (_startDate == normalized) return;
            _startDate = normalized;
            OnPropertyChanged();
            RefreshCommandStates();
        }
    }

    private DateTime _endDate;
    public DateTime EndDate
    {
        get { return _endDate; }
        set
        {
            var normalized = ToYearMonth(value);
            if (_endDate == normalized) return;
            _endDate = normalized;
            OnPropertyChanged();
            RefreshCommandStates();
        }
    }

    #endregion

    public ManagesShelfTanentContractViewModel(IShelfTenantContractRepository? selected = null) : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTenantContractRepository>())
    {
    }

    #region Load handler
    #endregion

    #region CanXXX Command States
    #endregion

    #region Command Handlers
    protected override async Task<ShelfTenantContract> OnAddFormAsync()
    {
        var entity = new ShelfTenantContract
        {
            StartDate = StartDate,
            EndDate = EndDate
        };
        return await Task.FromResult(entity);
    }

    protected override async Task OnLoadFormAsync(ShelfTenantContract entity)
    {
        StartDate = entity.StartDate;
        EndDate = entity.EndDate;
        await Task.CompletedTask;
    }

    protected override async Task OnResetFormAsync()
    {
        StartDate = DateTime.Now;
        EndDate = DateTime.Now.AddMonths(1);
        await Task.CompletedTask;
    }

    protected override async Task OnSaveFormAsync()
    {
        var entity = CurrentEntity ?? throw new InvalidOperationException("No entity loaded to save.");
        entity.StartDate = StartDate;
        entity.EndDate = EndDate;
        await Task.CompletedTask;
    }
    #endregion
}

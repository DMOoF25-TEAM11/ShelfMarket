using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views.Windows;

/// <summary>
/// Contract creation popup.
/// Uses a per-popup IServiceScope to resolve its ViewModel so the DbContext lifetime
/// is isolated from other screens, avoiding cross-thread DbContext reuse.
/// The scope is disposed on Unloaded.
/// </summary>
public partial class AddContractWindow : UserControl
{
    private readonly IServiceScope _scope;

    public AddContractWindow()
    {
        InitializeComponent();
        
        // Per-popup scope to isolate DbContext lifetime
        _scope = App.HostInstance.Services.CreateScope();
        DataContext = _scope.ServiceProvider.GetRequiredService<ManagesShelfTenantContractViewModel>();

        Unloaded += (_, __) => _scope?.Dispose();
    }

    // Click handlers are no longer needed - using Commands instead
}

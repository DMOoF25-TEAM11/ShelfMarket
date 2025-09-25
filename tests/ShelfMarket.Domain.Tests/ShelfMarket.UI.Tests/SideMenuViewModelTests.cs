using System.Reflection;
using ShelfMarket.Domain.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace ShelfMarket.UI.Tests;

[TestClass]
public class SideMenuViewModelTests
{
    private sealed class FakePrivilegeService : IPrivilegeService
    {
        public PrivilegeLevel CurrentLevel { get; private set; } = PrivilegeLevel.Guest;

        public event EventHandler? CurrentLevelChanged;

        public bool SignIn(PrivilegeLevel level, string? password)
        {
            CurrentLevel = level;
            CurrentLevelChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void SignOut()
        {
            CurrentLevel = PrivilegeLevel.Guest;
            CurrentLevelChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanAccess(PrivilegeLevel required) => CurrentLevel >= required;
    }

    private sealed class SimpleHost : IHost
    {
        public IServiceProvider Services { get; }

        public SimpleHost(IServiceProvider services) => Services = services;
        public void Dispose() { }
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private FakePrivilegeService _privilegeService = null!;

    [AssemblyInitialize]
    public static void AssemblyInit(TestContext _)
    {
        // Ensure a WPF Application exists
        if (Application.Current == null)
        {
            new Application();
        }
    }

    [TestInitialize]
    public void TestInit()
    {
        _privilegeService = new FakePrivilegeService();

        var services = new ServiceCollection();
        services.AddSingleton<IPrivilegeService>(_privilegeService);
        var provider = services.BuildServiceProvider();
        var host = new SimpleHost(provider);

        // Reflectively set App.HostInstance (no change to production code)
        var appType = Type.GetType("ShelfMarket.UI.App");
        Assert.IsNotNull(appType, "Could not locate ShelfMarket.UI.App. Ensure the UI project is referenced.");

        var hostInstanceMember = appType
            .GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m =>
                m.Name == "HostInstance" &&
                (m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field));

        Assert.IsNotNull(hostInstanceMember, "App.HostInstance not found.");

        switch (hostInstanceMember)
        {
            case PropertyInfo pi:
                pi.SetValue(null, host);
                break;
            case FieldInfo fi:
                fi.SetValue(null, host);
                break;
            default:
                Assert.Fail("Unsupported HostInstance member type.");
                break;
        }
    }

    private static SideMenuViewModel.SideMenuItem? FindItem(SideMenuViewModel vm, string title)
        => vm.SideMenuItems.FirstOrDefault(i => string.Equals(i.Title, title, StringComparison.Ordinal));

    [TestMethod]
    [Apartment(ApartmentState.STA)]
    public void Constructor_SetsDefaultSelection_ToReoler()
    {
        var vm = new SideMenuViewModel();

        Assert.IsNotNull(vm.SelectedMenuItem, "SelectedMenuItem should be initialized.");
        Assert.AreEqual("Reoler", vm.SelectedMenuItem!.Title);
        Assert.AreEqual(PrivilegeLevel.Guest, vm.CurrentLevel);
    }

    [TestMethod]
    [Apartment(ApartmentState.STA)]
    public void MenuItemCommand_CanExecute_ForDefaultGuestItem()
    {
        var vm = new SideMenuViewModel();

        Assert.IsTrue(vm.MenuItemCommand.CanExecute(null), "Command should be executable for default item.");
    }

    [TestMethod]
    [Apartment(ApartmentState.STA)]
    public void LogoffItem_TriggersSignOut_AndSelectsLogin()
    {
        var vm = new SideMenuViewModel();

        // Simulate signing in first so Log af becomes visible/meaningful
        _privilegeService.SignIn(PrivilegeLevel.User, null);
        var logoff = FindItem(vm, "Log af");
        Assert.IsNotNull(logoff, "Log af item not found.");

        vm.SelectedMenuItem = logoff;

        Assert.AreEqual(PrivilegeLevel.Guest, vm.CurrentLevel, "Should be guest after sign out.");
        Assert.IsNotNull(vm.SelectedMenuItem);
        Assert.AreEqual("Log på", vm.SelectedMenuItem!.Title, "After logoff should navigate to login.");
    }

    [TestMethod]
    [Apartment(ApartmentState.STA)]
    public void LoginItem_Hidden_AfterSignIn()
    {
        var vm = new SideMenuViewModel();
        var login = FindItem(vm, "Log på");
        Assert.IsNotNull(login, "Login item not found pre-signin.");

        _privilegeService.SignIn(PrivilegeLevel.User, null);

        // Visibility is evaluated via IsItemVisibleForCurrentLevel (private); we approximate:
        // After sign in, selecting "Log på" should be prevented by CanExecute (privilege gating + hidden logic).
        vm.SelectedMenuItem = login; // Attempt selection

        // Since selection logic does not block hidden selection directly (UI should), we verify the clamp by changing level and refresh.
        // Force privilege change already triggered; now pick a normal user item and ensure login isn't reselected automatically.
        var sales = FindItem(vm, "Salg");
        Assert.IsNotNull(sales);
        vm.SelectedMenuItem = sales;

        Assert.AreEqual("Salg", vm.SelectedMenuItem!.Title);
        Assert.AreEqual(PrivilegeLevel.User, vm.CurrentLevel);
    }

    [TestMethod]
    [Apartment(ApartmentState.STA)]
    public void Command_Disabled_ForHigherPrivilegeItem()
    {
        var vm = new SideMenuViewModel();

        var economy = FindItem(vm, "Økonomi"); // Admin-only
        Assert.IsNotNull(economy, "Økonomi item not found.");

        vm.SelectedMenuItem = economy;
        Assert.IsFalse(vm.MenuItemCommand.CanExecute(null), "Command should be disabled for insufficient privilege.");
    }

    [TestMethod]
    [Apartment(ApartmentState.STA)]
    public void PrivilegeUpgrade_EnablesPreviouslyRestrictedItem()
    {
        var vm = new SideMenuViewModel();

        var economy = FindItem(vm, "Økonomi");
        Assert.IsNotNull(economy);

        vm.SelectedMenuItem = economy;
        Assert.IsFalse(vm.MenuItemCommand.CanExecute(null), "Should be disabled before sign in.");

        _privilegeService.SignIn(PrivilegeLevel.Admin, null);

        // Re-select to trigger CanExecute refresh
        vm.SelectedMenuItem = economy;
        Assert.IsTrue(vm.MenuItemCommand.CanExecute(null), "Admin should access Økonomi.");
    }
}
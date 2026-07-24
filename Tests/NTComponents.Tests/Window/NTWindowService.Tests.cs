using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Windowing;

namespace NTComponents.Tests.Window;

/// <summary>
///     Tests for <see cref="NTWindowService" /> and <see cref="NTWindowHost" />.
/// </summary>
public class NTWindowService_Tests : BunitContext {

    [Fact]
    public void AddNTServices_RegistersScopedWindowService() {
        var services = new ServiceCollection();
        services.AddNTServices();
        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<INTWindowService>();
        var firstAgain = firstScope.ServiceProvider.GetRequiredService<INTWindowService>();
        var second = secondScope.ServiceProvider.GetRequiredService<INTWindowService>();

        firstAgain.Should().BeSameAs(first);
        second.Should().NotBeSameAs(first);
    }

    [Fact]
    public void OpenSetStateAndClose_ManagePersistentWindowState() {
        var service = new NTWindowService();
        var changedCount = 0;
        service.Changed += () => changedCount++;

        var window = service.Open("Report", Fragment("Quarterly results"), dockPosition: NTWindowDockPosition.BottomLeft);

        service.Windows.Should().ContainSingle().Which.Should().BeSameAs(window);
        window.Title.Should().Be("Report");
        window.State.Should().Be(NTWindowState.Normal);
        window.DockPosition.Should().Be(NTWindowDockPosition.BottomLeft);
        window.Id.Should().NotBeNullOrWhiteSpace();

        service.SetState(window, NTWindowState.Fullscreen);

        window.State.Should().Be(NTWindowState.Fullscreen);
        service.Windows.Should().ContainSingle().Which.State.Should().Be(NTWindowState.Fullscreen);

        service.Close(window);

        service.Windows.Should().BeEmpty();
        changedCount.Should().Be(3);
    }

    [Fact]
    public void SetState_WithSameState_DoesNotRaiseChanged() {
        var service = new NTWindowService();
        var window = service.Open("Report", Fragment("Content"));
        var changedCount = 0;
        service.Changed += () => changedCount++;

        service.SetState(window, NTWindowState.Normal);

        changedCount.Should().Be(0);
    }

    [Fact]
    public void ForeignWindow_CannotBeClosedOrMutatedAndDoesNotRaiseChanged() {
        var service = new NTWindowService();
        var foreignService = new NTWindowService();
        var ownedWindow = service.Open("Owned", Fragment("Owned content"));
        var foreignWindow = foreignService.Open("Foreign", Fragment("Foreign content"));
        var changedCount = 0;
        service.Changed += () => changedCount++;

        service.SetState(foreignWindow, NTWindowState.Fullscreen);
        service.Close(foreignWindow);

        service.Windows.Should().ContainSingle().Which.Should().BeSameAs(ownedWindow);
        ownedWindow.State.Should().Be(NTWindowState.Normal);
        foreignService.Windows.Should().ContainSingle().Which.Should().BeSameAs(foreignWindow);
        foreignWindow.State.Should().Be(NTWindowState.Normal);
        changedCount.Should().Be(0);
    }

    [Fact]
    public void Close_WhenCalledTwice_RemovesOnceAndRaisesChangedOnce() {
        var service = new NTWindowService();
        var window = service.Open("Report", Fragment("Content"));
        var changedCount = 0;
        service.Changed += () => changedCount++;

        service.Close(window);
        service.Close(window);

        service.Windows.Should().BeEmpty();
        changedCount.Should().Be(1);
    }

    [Fact]
    public async Task Host_RendersServiceWindowsAndKeepsStateAcrossRerenders() {
        Services.AddNTServices();
        var service = Services.GetRequiredService<INTWindowService>();
        var cut = Render<NTWindowHost>();

        service.Open("Report", Fragment("Quarterly results"), dockPosition: NTWindowDockPosition.TopRight);

        cut.Find("[data-nt-window-host]").GetAttribute("role").Should().Be("region");
        cut.Find("[data-nt-window-host]").GetAttribute("aria-label").Should().Be("Open windows");
        cut.Find("[data-nt-window-host]").HasAttribute("data-permanent").Should().BeTrue();
        cut.Find(".nt-window-title").TextContent.Should().Be("Report");
        cut.Find("section.nt-window").GetAttribute("data-nt-window-dock-position").Should().Be("top-right");
        cut.Find(".nt-window-content").TextContent.Should().Contain("Quarterly results");

        await cut.Find("button[aria-label='Minimize Report']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.Render();

        service.Windows.Should().ContainSingle().Which.State.Should().Be(NTWindowState.Minimized);
        cut.Find("section.nt-window").ClassList.Should().Contain("nt-window-minimized");
        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task HostCloseControl_RemovesWindowFromService() {
        Services.AddNTServices();
        var service = Services.GetRequiredService<INTWindowService>();
        var cut = Render<NTWindowHost>();
        service.Open("Report", Fragment("Content"));

        await cut.Find("button[aria-label='Close Report']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        service.Windows.Should().BeEmpty();
        cut.FindAll("[data-nt-window]").Should().BeEmpty();
    }

    [Fact]
    public void Host_WhenDisposed_UnsubscribesFromWindowServiceChanges() {
        var service = new TrackingWindowService();
        Services.AddSingleton<INTWindowService>(service);
        var cut = Render<NTWindowHost>();

        service.ActiveSubscriptions.Should().Be(1);
        service.SubscriptionsAdded.Should().Be(1);

        ((IDisposable)cut.Instance).Dispose();

        service.ActiveSubscriptions.Should().Be(0);
        service.SubscriptionsRemoved.Should().Be(1);
    }

    [Fact]
    public void Open_RejectsInvalidInput() {
        var service = new NTWindowService();

        var emptyTitle = () => service.Open(" ", Fragment("Content"));
        var nullContent = () => service.Open("Report", null!);
        var invalidState = () => service.Open("Report", Fragment("Content"), (NTWindowState)999);
        var invalidDockPosition = () => service.Open("Report", Fragment("Content"), dockPosition: (NTWindowDockPosition)999);

        emptyTitle.Should().Throw<ArgumentException>();
        nullContent.Should().Throw<ArgumentNullException>();
        invalidState.Should().Throw<ArgumentOutOfRangeException>();
        invalidDockPosition.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static RenderFragment Fragment(string content) => builder => builder.AddMarkupContent(0, content);

    private sealed class TrackingWindowService : INTWindowService {
        private Action? _changed;

        public int ActiveSubscriptions { get; private set; }
        public int SubscriptionsAdded { get; private set; }
        public int SubscriptionsRemoved { get; private set; }
        public IReadOnlyList<INTWindow> Windows { get; } = [];

        public event Action? Changed {
            add {
                _changed += value;
                ActiveSubscriptions++;
                SubscriptionsAdded++;
            }
            remove {
                var subscriptionsBeforeRemoval = _changed?.GetInvocationList().Length ?? 0;
                _changed -= value;
                if ((_changed?.GetInvocationList().Length ?? 0) < subscriptionsBeforeRemoval) {
                    ActiveSubscriptions--;
                    SubscriptionsRemoved++;
                }
            }
        }

        public void Close(INTWindow window) => throw new NotSupportedException();

        public INTWindow Open(string title, RenderFragment content, NTWindowState initialState = NTWindowState.Normal, NTWindowDockPosition dockPosition = NTWindowDockPosition.BottomRight) => throw new NotSupportedException();

        public void SetState(INTWindow window, NTWindowState state) => throw new NotSupportedException();
    }
}

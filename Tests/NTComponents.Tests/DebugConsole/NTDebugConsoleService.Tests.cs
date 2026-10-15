using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NTComponents.DebugConsole;
using NTComponents.Snackbar;
using NTComponents.Toast;
using System.Security.Claims;

namespace NTComponents.Tests.DebugConsole;

public class NTDebugConsoleService_Tests {

    [Fact]
    public void WhenFull_EvictsTheOldestEntries() {
        // Arrange
        using var console = CreateEnabled(options => options.MaxEntries = 3);

        // Act
        for (var i = 1; i <= 5; i++) {
            console.Write(LogLevel.Information, $"Step {i}");
        }

        // Assert
        console.Entries.Select(entry => entry.Message).Should().Equal("Step 3", "Step 4", "Step 5");
    }

    [Fact]
    public void Write_AddsAnAppEntryWithTheDefaultCategory() {
        using var console = CreateEnabled();

        console.Write(LogLevel.Warning, "Payment declined");

        console.Entries.Should().ContainSingle().Which.Should().Match<NTDebugConsoleEntry>(entry =>
            entry.Level == LogLevel.Warning && entry.Message == "Payment declined" && entry.Category == "App" && entry.Source == NTDebugConsoleEntrySource.App && entry.Exception == null);
    }

    [Fact]
    public void Write_UsesTheGivenCategory() {
        using var console = CreateEnabled();

        console.Write(LogLevel.Information, "Opened cart", "Checkout");

        console.Entries.Should().ContainSingle().Which.Category.Should().Be("Checkout");
    }

    [Fact]
    public void Write_IgnoresLevelNone() {
        using var console = CreateEnabled();

        console.Write(LogLevel.None, "Hidden");

        console.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Write_IgnoresTheMinimumLevel() {
        using var console = CreateEnabled(options => options.MinimumLevel = LogLevel.Error);

        console.Write(LogLevel.Debug, "Tapped retry");

        console.Entries.Should().ContainSingle();
    }

    [Fact]
    public void Write_RaisesChanged() {
        using var console = CreateEnabled();
        var changes = 0;
        console.Changed += () => changes++;

        console.Write(LogLevel.Information, "Saved");

        changes.Should().Be(1);
    }

    [Fact]
    public void Clear_RemovesEntriesAndRaisesChanged() {
        using var console = CreateEnabled();
        console.Write(LogLevel.Information, "Saved");
        var changes = 0;
        console.Changed += () => changes++;

        console.Clear();

        console.Entries.Should().BeEmpty();
        changes.Should().Be(1);
    }

    [Fact]
    public void Clear_KeepsCapturingInOrderAfterwards() {
        using var console = CreateEnabled(options => options.MaxEntries = 2);
        console.Write(LogLevel.Information, "First");
        console.Write(LogLevel.Information, "Second");
        console.Write(LogLevel.Information, "Third");
        console.Clear();

        console.Write(LogLevel.Information, "Fourth");
        console.Write(LogLevel.Information, "Fifth");
        console.Write(LogLevel.Information, "Sixth");

        console.Entries.Select(entry => entry.Message).Should().Equal("Fifth", "Sixth");
    }

    [Fact]
    public void AFailingChangedSubscriber_DoesNotStopTheWriteOrOtherSubscribers() {
        using var console = CreateEnabled();
        var notified = false;
        console.Changed += () => throw new InvalidOperationException("Subscriber failed");
        console.Changed += () => notified = true;

        var write = () => console.Write(LogLevel.Information, "Saved");

        write.Should().NotThrow();
        notified.Should().BeTrue();
        console.Entries.Should().ContainSingle();
    }

    [Fact]
    public void WithoutAuthorize_IsDisabledAndCapturesNothing() {
        // Arrange: a single-session host authorizes itself, so only the missing rule keeps it disabled.
        using var console = Create(singleSession: true);

        // Act
        console.Write(LogLevel.Error, "Checkout failed");

        // Assert
        console.IsEnabled.Should().BeFalse();
        console.Entries.Should().BeEmpty();
    }

    [Fact]
    public void WithAllowAll_OnASingleSessionHost_IsEnabled() {
        using var console = Create(options => options.AllowAll(), singleSession: true);

        console.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void OnAMultiSessionHost_StaysDisabledUntilACircuitAuthorizesIt() {
        using var console = Create(options => options.AllowAll(), singleSession: false);

        console.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void WithoutAnAuthenticationStateProvider_AuthorizesAnUnauthenticatedUser() {
        bool? authenticated = null;
        using var console = Create(options => options.Authorize = state => {
            authenticated = state.User.Identity?.IsAuthenticated;
            return true;
        }, singleSession: true);

        authenticated.Should().BeFalse();
        console.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void AnAuthorizeRuleThatThrows_Denies() {
        using var console = Create(options => options.Authorize = _ => throw new InvalidOperationException("No claims"), singleSession: true);

        console.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void AProviderThatThrowsBeforeItHasAUser_DeniesWithoutThrowing() {
        var authentication = new TestAuthenticationStateProvider { ThrowUntilSignedIn = true };

        var create = () => Create(options => options.AllowAll(), singleSession: true, configure: services => services.AddSingleton<AuthenticationStateProvider>(authentication));

        create.Should().NotThrow().Which.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void AuthenticationChanges_ReevaluateAuthorize() {
        // Arrange
        var authentication = new TestAuthenticationStateProvider();
        using var console = Create(options => options.Authorize = state => state.User.IsInRole("Developer"), singleSession: true, configure: services => services.AddSingleton<AuthenticationStateProvider>(authentication));
        var deniedAtStart = console.IsEnabled;

        // Act
        authentication.SignIn("Developer");
        var allowedAfterSignIn = console.IsEnabled;
        console.Write(LogLevel.Information, "Signed in");
        authentication.SignIn("Customer");

        // Assert
        deniedAtStart.Should().BeFalse();
        allowedAfterSignIn.Should().BeTrue();
        console.IsEnabled.Should().BeFalse();
        console.Entries.Should().BeEmpty("disabling the console clears what the previous user could see");
    }

    [Fact]
    public void Redact_RewritesEntries() {
        using var console = CreateEnabled(options => options.Redact = entry => entry with { Message = entry.Message.Replace("4111-1111", "****", StringComparison.Ordinal) });

        console.Write(LogLevel.Information, "Card 4111-1111 charged");

        console.Entries.Should().ContainSingle().Which.Message.Should().Be("Card **** charged");
    }

    [Fact]
    public void Redact_DropsEntriesItReturnsNullFor() {
        using var console = CreateEnabled(options => options.Redact = entry => entry.Message.Contains("token", StringComparison.Ordinal) ? null : entry);

        console.Write(LogLevel.Information, "Refreshed token abc");
        console.Write(LogLevel.Information, "Loaded profile");

        console.Entries.Should().ContainSingle().Which.Message.Should().Be("Loaded profile");
    }

    [Fact]
    public void ARedactRuleThatThrows_DropsTheEntry() {
        using var console = CreateEnabled(options => options.Redact = _ => throw new InvalidOperationException("Bad rule"));

        var write = () => console.Write(LogLevel.Information, "Saved");

        write.Should().NotThrow();
        console.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(NTToastVariant.Error, LogLevel.Error)]
    [InlineData(NTToastVariant.Warning, LogLevel.Warning)]
    [InlineData(NTToastVariant.Success, LogLevel.Information)]
    public void ShownToasts_AreMirrored(NTToastVariant variant, LogLevel expectedLevel) {
        // Arrange
        var toasts = Substitute.For<INTToastService>();
        using var console = CreateEnabled(configure: services => services.AddSingleton(toasts));
        var toast = Substitute.For<INTToast>();
        toast.Title.Returns("Order saved");
        toast.Message.Returns("Order 1042");
        toast.Variant.Returns(variant);

        // Act
        toasts.OnOpen += Raise.Event<INTToastService.OnOpenCallback>(toast);

        // Assert
        console.Entries.Should().ContainSingle().Which.Should().Match<NTDebugConsoleEntry>(entry =>
            entry.Source == NTDebugConsoleEntrySource.Toast && entry.Level == expectedLevel && entry.Message == "Order saved: Order 1042");
    }

    [Fact]
    public void ShownSnackbars_AreMirrored() {
        var snackbars = Substitute.For<INTSnackbarService>();
        using var console = CreateEnabled(configure: services => services.AddSingleton(snackbars));
        var snackbar = Substitute.For<INTSnackbar>();
        snackbar.Message.Returns("Message archived");

        snackbars.OnOpen += Raise.Event<INTSnackbarService.OnOpenCallback>(snackbar);

        console.Entries.Should().ContainSingle().Which.Should().Match<NTDebugConsoleEntry>(entry =>
            entry.Source == NTDebugConsoleEntrySource.Snackbar && entry.Level == LogLevel.Information && entry.Message == "Message archived");
    }

    [Fact]
    public void WithMirroringOff_IgnoresToasts() {
        var toasts = Substitute.For<INTToastService>();
        using var console = CreateEnabled(options => options.MirrorToastsAndSnackbars = false, configure: services => services.AddSingleton(toasts));

        toasts.OnOpen += Raise.Event<INTToastService.OnOpenCallback>(Substitute.For<INTToast>());

        console.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_UnsubscribesFromToastsAndSnackbars() {
        var toasts = Substitute.For<INTToastService>();
        var snackbars = Substitute.For<INTSnackbarService>();
        var console = CreateEnabled(configure: services => services.AddSingleton(toasts).AddSingleton(snackbars));

        console.Dispose();

        toasts.Received(1).OnOpen -= Arg.Any<INTToastService.OnOpenCallback>();
        snackbars.Received(1).OnOpen -= Arg.Any<INTSnackbarService.OnOpenCallback>();
        console.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void AddNTDebugConsole_RejectsAnEmptyBuffer() {
        var register = () => new ServiceCollection().AddNTDebugConsole(options => options.MaxEntries = 0);

        register.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddNTDebugConsole_ResolvesOneSessionPerScope() {
        using var provider = new ServiceCollection().AddNTDebugConsole(options => options.AllowAll()).BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var console = first.ServiceProvider.GetRequiredService<INTDebugConsole>();

        console.Should().BeSameAs(first.ServiceProvider.GetRequiredService<NTDebugConsoleService>());
        console.Should().NotBeSameAs(second.ServiceProvider.GetRequiredService<INTDebugConsole>());
    }

    private static NTDebugConsoleService Create(Action<NTDebugConsoleOptions>? options = null, bool singleSession = false, Action<IServiceCollection>? configure = null) {
        var configured = new NTDebugConsoleOptions();
        options?.Invoke(configured);
        var services = new ServiceCollection();
        configure?.Invoke(services);
        return new NTDebugConsoleService(configured, new NTDebugConsoleSessionAccessor(singleSession), services.BuildServiceProvider());
    }

    private static NTDebugConsoleService CreateEnabled(Action<NTDebugConsoleOptions>? options = null, Action<IServiceCollection>? configure = null) {
        var console = Create(configured => {
            configured.AllowAll();
            options?.Invoke(configured);
        }, configure: configure);
        console.SetEnabled(true);
        return console;
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider {
        private AuthenticationState _state = new(new ClaimsPrincipal(new ClaimsIdentity()));

        public bool ThrowUntilSignedIn { get; init; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => ThrowUntilSignedIn ? throw new InvalidOperationException("No user yet.") : Task.FromResult(_state);

        public void SignIn(string role) {
            _state = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test")));
            NotifyAuthenticationStateChanged(Task.FromResult(_state));
        }
    }
}

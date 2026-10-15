using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NTComponents.DebugConsole;

namespace NTComponents.Tests.DebugConsole;

public class NTDebugConsoleLoggerProvider_Tests {
    private const string Category = "Shop.Checkout";

    [Fact]
    public async Task WithASession_RoutesLogsToIt() {
        // Arrange
        var (accessor, provider) = CreateProvider();
        var session = CreateSession(accessor);

        // Act
        await RunInSessionAsync(accessor, session, () => provider.CreateLogger(Category).LogInformation("Order {OrderId} placed", 1042));

        // Assert
        session.Entries.Should().ContainSingle().Which.Should().Match<NTDebugConsoleEntry>(entry =>
            entry.Message == "Order 1042 placed" && entry.Category == Category && entry.Level == LogLevel.Information && entry.Source == NTDebugConsoleEntrySource.Log);
    }

    [Fact]
    public void WithoutASession_OnAMultiSessionHost_DropsLogs() {
        var (accessor, provider) = CreateProvider();
        var session = CreateSession(accessor);
        var logger = provider.CreateLogger(Category);

        logger.LogError("Background job failed");

        logger.IsEnabled(LogLevel.Error).Should().BeFalse();
        session.Entries.Should().BeEmpty();
    }

    [Fact]
    public void OnASingleSessionHost_RoutesLogsToItsOneSession() {
        var (accessor, provider) = CreateProvider(singleSession: true);
        var session = CreateSession(accessor);

        provider.CreateLogger(Category).LogWarning("Slow response");

        session.Entries.Should().ContainSingle().Which.Message.Should().Be("Slow response");
    }

    [Fact]
    public async Task BelowTheMinimumLevel_DropsLogsWithoutFormattingThem() {
        // Arrange
        var (accessor, provider) = CreateProvider(options => options.MinimumLevel = LogLevel.Warning);
        var session = CreateSession(accessor);
        var formatted = false;
        var logger = provider.CreateLogger(Category);

        // Act
        await RunInSessionAsync(accessor, session, () => {
            logger.Log(LogLevel.Information, default, "state", null, (_, _) => {
                formatted = true;
                return "Formatted";
            });
            logger.LogWarning("Retrying payment");
        });

        // Assert
        formatted.Should().BeFalse();
        session.Entries.Should().ContainSingle().Which.Message.Should().Be("Retrying payment");
    }

    [Theory]
    [InlineData("NTComponents.DebugConsole.NTDebugConsoleService")]
    [InlineData("NTComponents.NTDebugConsole")]
    [InlineData("Microsoft.AspNetCore.Components.RenderTree.Renderer")]
    [InlineData("Microsoft.AspNetCore.Components.Server.Circuits.RemoteRenderer")]
    public async Task ExcludedCategories_AreNotCapturedByDefault(string category) {
        var (accessor, provider) = CreateProvider();
        var session = CreateSession(accessor);

        await RunInSessionAsync(accessor, session, () => provider.CreateLogger(category).LogError("Rendering failed"));

        session.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfiguredExclusions_AreNotCaptured() {
        var (accessor, provider) = CreateProvider(options => options.ExcludedCategoryPrefixes.Add("Shop."));
        var session = CreateSession(accessor);

        await RunInSessionAsync(accessor, session, () => {
            provider.CreateLogger(Category).LogInformation("Excluded");
            provider.CreateLogger("Billing.Invoices").LogInformation("Included");
        });

        session.Entries.Should().ContainSingle().Which.Message.Should().Be("Included");
    }

    [Fact]
    public async Task ADisabledSession_CapturesNothing() {
        var (accessor, provider) = CreateProvider();
        var session = CreateSession(accessor);
        session.SetEnabled(false);

        await RunInSessionAsync(accessor, session, () => provider.CreateLogger(Category).LogError("Hidden"));

        session.Entries.Should().BeEmpty();
    }

    [Fact]
    public void WithoutAuthorize_TheLoggerIsNeverEnabled() {
        var (accessor, provider) = CreateProvider(options => options.Authorize = null, singleSession: true);
        CreateSession(accessor).SetEnabled(true);

        provider.CreateLogger(Category).IsEnabled(LogLevel.Critical).Should().BeFalse();
    }

    [Fact]
    public async Task Exceptions_KeepTheirFullTextByDefault() {
        var (accessor, provider) = CreateProvider();
        var session = CreateSession(accessor);
        var exception = CreateThrownException();

        await RunInSessionAsync(accessor, session, () => provider.CreateLogger(Category).LogError(exception, "Checkout failed"));

        session.Entries.Should().ContainSingle().Which.Exception.Should().Be(exception.ToString());
    }

    [Fact]
    public async Task WithoutExceptionDetails_KeepsOnlyTheTypeAndMessage() {
        var (accessor, provider) = CreateProvider(options => options.IncludeExceptionDetails = false);
        var session = CreateSession(accessor);

        await RunInSessionAsync(accessor, session, () => provider.CreateLogger(Category).LogError(CreateThrownException(), "Checkout failed"));

        session.Entries.Should().ContainSingle().Which.Exception.Should().Be("System.InvalidOperationException: Card declined");
    }

    [Fact]
    public async Task WorkOnARegisteredDispatcher_BelongsToThatDispatchersSession() {
        // Arrange
        var (accessor, provider) = CreateProvider();
        var owner = CreateSession(accessor);
        var other = CreateSession(accessor);
        var dispatcher = Dispatcher.CreateDefault();
        var registered = await dispatcher.InvokeAsync(() => accessor.RegisterDispatcher(SynchronizationContext.Current, owner));

        // Act: the other session queues the work, so the work's execution context carries the other session.
        await RunInSessionAsync(accessor, other, () => dispatcher.InvokeAsync(() => provider.CreateLogger(Category).LogInformation("Rendered cart")));

        // Assert
        registered.Should().BeTrue();
        owner.Entries.Should().ContainSingle().Which.Message.Should().Be("Rendered cart");
        other.Entries.Should().BeEmpty();
    }

    [Fact]
    public void RegisterDispatcher_IgnoresContextsThatAreNotBlazorRenderers() {
        var (accessor, _) = CreateProvider();

        accessor.RegisterDispatcher(new SynchronizationContext(), CreateSession(accessor)).Should().BeFalse();
    }

    [Fact]
    public void AddNTDebugConsole_RegistersTheProviderWithLogging() {
        using var services = new ServiceCollection().AddNTDebugConsole(options => options.AllowAll()).BuildServiceProvider();

        services.GetServices<ILoggerProvider>().Should().ContainSingle(provider => provider is NTDebugConsoleLoggerProvider);
    }

    private static Exception CreateThrownException() {
        try {
            throw new InvalidOperationException("Card declined");
        }
        catch (InvalidOperationException exception) {
            return exception;
        }
    }

    private static (NTDebugConsoleSessionAccessor Accessor, NTDebugConsoleLoggerProvider Provider) CreateProvider(Action<NTDebugConsoleOptions>? configure = null, bool singleSession = false) {
        var options = new NTDebugConsoleOptions().AllowAll();
        configure?.Invoke(options);
        var accessor = new NTDebugConsoleSessionAccessor(singleSession);
        return (accessor, new NTDebugConsoleLoggerProvider(accessor, options));
    }

    private static NTDebugConsoleService CreateSession(NTDebugConsoleSessionAccessor accessor) {
        var session = new NTDebugConsoleService(new NTDebugConsoleOptions().AllowAll(), accessor, new ServiceCollection().BuildServiceProvider());
        session.SetEnabled(true);
        return session;
    }

    // Entering a session inside an async method keeps it out of the test's own execution context.
    private static async Task RunInSessionAsync(NTDebugConsoleSessionAccessor accessor, NTDebugConsoleService session, Func<Task> work) {
        accessor.Enter(session);
        await work();
    }

    private static Task RunInSessionAsync(NTDebugConsoleSessionAccessor accessor, NTDebugConsoleService session, Action work) => RunInSessionAsync(accessor, session, () => {
        work();
        return Task.CompletedTask;
    });
}

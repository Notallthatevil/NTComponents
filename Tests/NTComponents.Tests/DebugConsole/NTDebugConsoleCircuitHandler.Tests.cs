using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NTComponents.AspNetCore.DebugConsole;
using NTComponents.DebugConsole;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace NTComponents.Tests.DebugConsole;

public sealed class NTDebugConsoleCircuitHandler_Tests : IDisposable {
    private readonly List<IDisposable> _disposables = [];

    public void Dispose() {
        foreach (var disposable in Enumerable.Reverse(_disposables)) {
            disposable.Dispose();
        }
    }

    [Fact]
    public async Task AnInboundActivity_SetsTheSessionForTheActivityIncludingAcrossAwaits() {
        // Arrange
        var host = CreateHost();
        var circuit = await host.OpenCircuitAsync();

        // Act
        await circuit.RunActivityAsync(async () => {
            host.Logger.LogInformation("Before await");
            await Task.Delay(10, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(false);
            host.Logger.LogInformation("After await on the thread pool");
            await Task.Yield();
            host.Logger.LogInformation("After yield");
        });
        host.Logger.LogInformation("After the activity");

        // Assert
        circuit.Messages.Should().Equal("Before await", "After await on the thread pool", "After yield");
    }

    [Fact]
    public async Task WorkOutsideAnyCircuit_IsNotCaptured() {
        var host = CreateHost();
        var circuit = await host.OpenCircuitAsync();

        await Task.Run(() => host.Logger.LogError("Background service failed"), Xunit.TestContext.Current.CancellationToken);

        circuit.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentCircuits_NeverSeeEachOthersEntries() {
        // Arrange
        const int activitiesPerCircuit = 40;
        var host = CreateHost(options => options.MaxEntries = 10_000);
        var circuits = new[] { await host.OpenCircuitAsync(), await host.OpenCircuitAsync(), await host.OpenCircuitAsync() };
        using var start = new ManualResetEventSlim();

        // Act: every circuit runs its activities at once on the thread pool, logging through the same singleton logger before, between, and after awaits.
        var work = circuits.SelectMany((circuit, circuitIndex) => Enumerable.Range(0, activitiesPerCircuit).Select(activity => Task.Run(async () => {
            start.Wait(Xunit.TestContext.Current.CancellationToken);
            await circuit.RunActivityAsync(async () => {
                host.Logger.LogInformation("circuit {Circuit} activity {Activity} start", circuitIndex, activity);
                await Task.Delay(Random.Shared.Next(0, 3), Xunit.TestContext.Current.CancellationToken).ConfigureAwait(false);
                host.Logger.LogInformation("circuit {Circuit} activity {Activity} middle", circuitIndex, activity);
                await Task.Yield();
                host.Logger.LogInformation("circuit {Circuit} activity {Activity} end", circuitIndex, activity);
            });
        }, Xunit.TestContext.Current.CancellationToken))).ToArray();
        start.Set();
        await Task.WhenAll(work);

        // Assert
        for (var circuitIndex = 0; circuitIndex < circuits.Length; circuitIndex++) {
            circuits[circuitIndex].Messages.Should().HaveCount(activitiesPerCircuit * 3)
                .And.OnlyContain(message => message.StartsWith($"circuit {circuitIndex} ", StringComparison.Ordinal), $"circuit {circuitIndex} must only see its own entries");
        }
    }

    [Fact]
    public async Task WorkOneCircuitQueuesOnAnotherCircuitsDispatcher_BelongsToTheOtherCircuit() {
        // Arrange: a shared service raises an event during the sender's activity, and the receiver's component handles it on its own dispatcher.
        var host = CreateHost();
        var sender = await host.OpenCircuitAsync();
        var receiver = await host.OpenCircuitAsync();

        // Act
        await sender.RunActivityAsync(async () => {
            host.Logger.LogInformation("Sent message");
            await receiver.Dispatcher.InvokeAsync(() => host.Logger.LogInformation("Received message for user 2"));
        });

        // Assert
        sender.Messages.Should().Equal("Sent message");
        receiver.Messages.Should().Equal("Received message for user 2");
    }

    [Fact]
    public async Task OpeningACircuit_EvaluatesAuthorizeAgainstTheCircuitsUser() {
        var host = CreateHost(options => options.Authorize = state => state.User.IsInRole("Developer"), withAuthentication: true);

        var developer = await host.OpenCircuitAsync("Developer");
        var customer = await host.OpenCircuitAsync("Customer");

        developer.Session.IsEnabled.Should().BeTrue();
        customer.Session.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticationChanges_ReevaluateIsEnabled() {
        // Arrange
        var host = CreateHost(options => options.Authorize = state => state.User.IsInRole("Developer"), withAuthentication: true);
        var circuit = await host.OpenCircuitAsync("Customer");
        var enabledBefore = circuit.Session.IsEnabled;

        // Act
        circuit.Authentication!.SignIn("Developer");

        // Assert
        enabledBefore.Should().BeFalse();
        circuit.Session.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ADeniedCircuit_CapturesNothing() {
        var host = CreateHost(options => options.Authorize = state => state.User.IsInRole("Developer"), withAuthentication: true);
        var circuit = await host.OpenCircuitAsync("Customer");

        await circuit.RunActivityAsync(() => {
            host.Logger.LogError("Checkout failed");
            return Task.CompletedTask;
        });

        circuit.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task WithoutAnAuthenticationStateProvider_AuthorizeReceivesAnUnauthenticatedUser() {
        var host = CreateHost(options => options.Authorize = state => state.User.Identity?.IsAuthenticated != true);

        var circuit = await host.OpenCircuitAsync();

        circuit.Session.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task WithoutAuthorize_TheCircuitStaysDisabled() {
        var host = CreateHost(options => options.Authorize = null);

        var circuit = await host.OpenCircuitAsync();

        circuit.Session.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task ClosingTheCircuit_DisablesAndClearsItsSession() {
        var host = CreateHost();
        var circuit = await host.OpenCircuitAsync();
        await circuit.RunActivityAsync(() => {
            host.Logger.LogInformation("Saved");
            return Task.CompletedTask;
        });

        await circuit.Dispatcher.InvokeAsync(() => circuit.Handler.OnCircuitClosedAsync(CreateCircuit(), CancellationToken.None));

        circuit.Session.IsEnabled.Should().BeFalse();
        circuit.Messages.Should().BeEmpty();
    }

    [Fact]
    public void AddNTDebugConsoleForServer_RegistersTheCircuitHandlerFirst() {
        using var services = new ServiceCollection().AddNTDebugConsoleForServer(options => options.AllowAll()).BuildServiceProvider();
        using var scope = services.CreateScope();

        var handler = scope.ServiceProvider.GetServices<CircuitHandler>().Should().ContainSingle().Which;

        handler.Should().BeOfType<NTDebugConsoleCircuitHandler>();
        handler.Order.Should().Be(int.MinValue);
    }

    private static Circuit CreateCircuit() => (Circuit)RuntimeHelpers.GetUninitializedObject(typeof(Circuit));

    private TestHost CreateHost(Action<NTDebugConsoleOptions>? configure = null, bool withAuthentication = false) {
        var services = new ServiceCollection();
        services.AddNTDebugConsoleForServer(options => {
            options.AllowAll();
            configure?.Invoke(options);
        });
        if (withAuthentication) {
            services.AddScoped<TestAuthenticationStateProvider>();
            services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<TestAuthenticationStateProvider>());
        }

        var provider = services.BuildServiceProvider();
        _disposables.Add(provider);
        return new TestHost(provider, _disposables);
    }

    private sealed class TestHost(ServiceProvider _provider, List<IDisposable> _disposables) {
        public ILogger Logger { get; } = _provider.GetRequiredService<ILogger<TestHost>>();

        // Mirrors Blazor Server: each circuit has its own scope and renderer dispatcher, and opens on that dispatcher.
        public async Task<TestCircuit> OpenCircuitAsync(string? role = null) {
            var scope = _provider.CreateScope();
            _disposables.Add(scope);
            var authentication = scope.ServiceProvider.GetService<TestAuthenticationStateProvider>();
            if (role is not null) {
                authentication?.SetUser(role);
            }

            var handler = scope.ServiceProvider.GetServices<CircuitHandler>().OfType<NTDebugConsoleCircuitHandler>().Single();
            var dispatcher = Dispatcher.CreateDefault();
            await dispatcher.InvokeAsync(() => handler.OnCircuitOpenedAsync(CreateCircuit(), CancellationToken.None));
            return new TestCircuit(handler, dispatcher, scope.ServiceProvider.GetRequiredService<NTDebugConsoleService>(), authentication);
        }
    }

    private sealed class TestCircuit(NTDebugConsoleCircuitHandler handler, Dispatcher dispatcher, NTDebugConsoleService session, TestAuthenticationStateProvider? authentication) {
        public TestAuthenticationStateProvider? Authentication => authentication;

        public Dispatcher Dispatcher => dispatcher;

        public NTDebugConsoleCircuitHandler Handler => handler;

        public IReadOnlyList<string> Messages => [.. session.Entries.Select(entry => entry.Message)];

        public NTDebugConsoleService Session => session;

        // Like CircuitHost, the inbound activity pipeline wraps work that is then dispatched to the circuit's renderer.
        public Task RunActivityAsync(Func<Task> work) {
            var activity = handler.CreateInboundActivityHandler(_ => dispatcher.InvokeAsync(work));
            return activity((CircuitInboundActivityContext)RuntimeHelpers.GetUninitializedObject(typeof(CircuitInboundActivityContext)));
        }
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider {
        private AuthenticationState _state = new(new ClaimsPrincipal(new ClaimsIdentity()));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);

        public void SetUser(string role) => _state = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test")));

        public void SignIn(string role) {
            SetUser(role);
            NotifyAuthenticationStateChanged(Task.FromResult(_state));
        }
    }
}

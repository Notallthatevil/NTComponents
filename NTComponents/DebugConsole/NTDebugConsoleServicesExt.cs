using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NTComponents;
using NTComponents.DebugConsole;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Registers the NTComponents debug console.
/// </summary>
public static class NTDebugConsoleServicesExt {

    /// <summary>
    ///     Adds the debug console: its options, the scoped <see cref="INTDebugConsole" /> session, and the logger provider that captures <c>ILogger</c> output.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use this registration for WebAssembly, where the whole app is one user's session and all <c>ILogger</c> output is captured. For Blazor Server, call
    ///         <c>AddNTDebugConsoleForServer</c> from the NTComponents.AspNetCore package instead, which captures only the current circuit's output; without it, a server app captures nothing.
    ///     </para>
    ///     <para>The console stays disabled until <see cref="NTDebugConsoleOptions.Authorize" /> is set, for example with <see cref="NTDebugConsoleOptions.AllowAll" />.</para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the console options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNTDebugConsole(this IServiceCollection services, Action<NTDebugConsoleOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);
        var options = new NTDebugConsoleOptions();
        configure?.Invoke(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxEntries, 1, nameof(NTDebugConsoleOptions.MaxEntries));

        services.AddLogging();
        services.AddSingleton(options);
        services.TryAddSingleton(new NTDebugConsoleSessionAccessor(OperatingSystem.IsBrowser()));
        services.TryAddScoped<NTDebugConsoleService>();
        services.TryAddScoped<INTDebugConsole>(provider => provider.GetRequiredService<NTDebugConsoleService>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, NTDebugConsoleLoggerProvider>());
        return services;
    }
}

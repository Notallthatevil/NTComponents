using System.Diagnostics.CodeAnalysis;
using NTComponents;
using NTComponents.Core;
using NTComponents.Dialog;
using NTComponents.Popover;
using NTComponents.Snackbar;
using NTComponents.Storage;
using NTComponents.Toast;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for adding TnT services to the service collection.
/// </summary>
[ExcludeFromCodeCoverage]
public static class TnTServicesExt {

    /// <summary>
    /// Adds TnT services to the service collection.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="options">Action to configure NTComponents default options</param>
    /// <returns>The IServiceCollection instance</returns>
    public static IServiceCollection AddTnTServices(this IServiceCollection services, Action<NTComponentsDefaultOptions>? options = null) {
        //#if DEBUG
        //        services.AddSassCompiler();
        //#endif
        var o = new NTComponentsDefaultOptions();
        options?.Invoke(o);
        return services.AddScoped<ITnTDialogService, TnTDialogService>()
             .AddScoped<ITnTPopoverService, TnTPopoverService>()
             .AddScoped<INTSnackbarService, NTSnackbarService>()
             .AddScoped<ITnTToastService, TnTToastService>()
             .AddScoped<ISessionStorageService, SessionStorageService>()
             .AddScoped<ILocalStorageService, LocalStorageService>()
             .AddSingleton(o);
    }
}

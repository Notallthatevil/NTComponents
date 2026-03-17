using System.Diagnostics.CodeAnalysis;
using NTComponents;
using NTComponents.Dialog;
using NTComponents.Snackbar;
using NTComponents.Storage;
using NTComponents.Toast;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Extensions for adding TnT services to the service collection.
/// </summary>
[ExcludeFromCodeCoverage]
public static class TnTServicesExt {

    /// <summary>
    ///     Adds TnT services to the service collection.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>The IServiceCollection instance</returns>
    public static IServiceCollection AddTnTServices(this IServiceCollection services) {
//#if DEBUG
//        services.AddSassCompiler();
//#endif
        return services.AddScoped<ITnTDialogService, TnTDialogService>()
             .AddScoped<INTSnackbarService, NTSnackbarService>()
             .AddScoped<ITnTToastService, TnTToastService>()
             .AddScoped<ISessionStorageService, SessionStorageService>()
             .AddScoped<ILocalStorageService, LocalStorageService>();
    }
}

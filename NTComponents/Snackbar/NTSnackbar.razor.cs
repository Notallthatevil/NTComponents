using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Snackbar;

namespace NTComponents;

/// <summary>
///     Renders the snackbar host used by the snackbar JavaScript module.
/// </summary>
public partial class NTSnackbar {

    /// <summary>
    ///     The static web asset path for the snackbar JavaScript module.
    /// </summary>
    public const string JsModulePathValue = "./_content/NTComponents/Snackbar/NTSnackbar.razor.js";

    /// <summary>
    ///     Controls where the snackbar container is placed in the viewport.
    /// </summary>
    [Parameter]
    public NTSnackbarPosition Position { get; set; } = NTSnackbarPosition.BottomCenter;

    private string ContainerClass => CssClassBuilder.Create()
        .AddClass("nt-snackbar-container")
        .AddClass(Position switch {
            NTSnackbarPosition.TopLeftCorner => "nt-snackbar-top-left-corner",
            NTSnackbarPosition.CenterTop => "nt-snackbar-center-top",
            NTSnackbarPosition.TopRightCorner => "nt-snackbar-top-right-corner",
            NTSnackbarPosition.BottomLeftCorner => "nt-snackbar-bottom-left-corner",
            NTSnackbarPosition.BottomRightCorner => "nt-snackbar-bottom-right-corner",
            _ => "nt-snackbar-bottom-center"
        })
        .Build() ?? string.Empty;
}

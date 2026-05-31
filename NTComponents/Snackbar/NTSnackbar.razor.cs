using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Snackbar;

namespace NTComponents;

/// <summary>
///     Renders the snackbar host used by the snackbar JavaScript module.
/// </summary>
/// <remarks>
///     <para>Place one
///         <code> NTSnackbar</code>
///         near the route or layout level. The component loads the snackbar JavaScript module and registers
///         <code> window.NTSnackbar</code>
///         , so static server-rendered markup and interactive components in any render mode can trigger snackbars through the same browser bridge.
///     </para>
///     <para>From JavaScript, call
///         <code> window.NTSnackbar.queueSnackbar('Saved')</code>
///         for a default snackbar. To include an action or other options, call
///         <code>
/// window.NTSnackbar.queueSnackbar({ message: 'Photos deleted', actionLabel: 'Undo', timeout: 0
///})
///         </code>
///         .
///     </para>
///     <para>Snackbars without actions default to a four-second timeout. Snackbars with actions default to
///         <code> timeout: 0</code>
///         and remain visible until dismissed or acted on.
///     </para>
/// </remarks>
public partial class NTSnackbar {

    /// <summary>
    ///     Controls where the snackbar container is placed in the viewport.
    /// </summary>
    [Parameter]
    public NTSnackbarPosition Position { get; set; } = NTSnackbarPosition.BottomCenter;

    /// <summary>
    ///     The static web asset path for the snackbar JavaScript module.
    /// </summary>
    public const string JsModulePathValue = "./_content/NTComponents/Snackbar/NTSnackbar.razor.js";

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
using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Toast;

namespace NTComponents;

/// <summary>
///     Renders the toast host used by the toast JavaScript module.
/// </summary>
/// <remarks>
///     <para>
///         Place one <code>NTToast</code> near the route or layout level. The component loads the toast JavaScript module and registers
///         <code>window.NTToast</code>, so static server-rendered markup and interactive components in any render mode can trigger toasts through the same browser bridge.
///     </para>
///     <para>
///         <see cref="INTToastService" /> uses Blazor JavaScript interop and should be called after an interactive render context exists, such as from event handlers or after first interactive render.
///         Static SSR markup should use guarded browser JavaScript, for example <code>window.NTToast?.queueToast({ title: 'Saved' })</code>.
///     </para>
///     <para>
///         <code>NTToast</code> is the host for <see cref="INTToastService" />. The legacy <code>TnTToast</code> host uses <code>ITnTToastService</code>; the two host/service pairs are not interchangeable.
///     </para>
///     <para>
///         From JavaScript, prefer a guarded call so early clicks before the module loads do not throw:
///         <code>window.NTToast?.queueToast({ title: 'Saved', message: 'Your changes are stored.', variant: 'success' })</code>.
///         Supported JavaScript options include <code>title</code>, <code>message</code>, <code>variant</code>, <code>timeout</code>, <code>showClose</code>, <code>icon</code>,
///         <code>backgroundColor</code>, <code>textColor</code>, and <code>iconColor</code>.
///     </para>
///     <para>
///         Toasts stack up to five visible messages, queue overflow behind the stack, and auto-dismiss after four seconds unless <code>timeout: 0</code> is provided.
///         Prefer semantic variants and short status text. Use <code>timeout: 0</code> only when the user must explicitly dismiss the message.
///     </para>
/// </remarks>
public partial class NTToast {
    /// <summary>
    ///     The static web asset path for the toast JavaScript module.
    /// </summary>
    public const string JsModulePathValue = "./_content/NTComponents/Toast/NTToast.razor.js";

    /// <summary>
    ///     Controls where the toast stack is placed in the viewport.
    /// </summary>
    [Parameter]
    public NTToastPosition Position { get; set; } = NTToastPosition.BottomRightCorner;

    private string ContainerClass => CssClassBuilder.Create()
        .AddClass("nt-toast-container")
        .AddClass(Position switch {
            NTToastPosition.TopLeftCorner => "nt-toast-top-left-corner",
            NTToastPosition.TopRightCorner => "nt-toast-top-right-corner",
            NTToastPosition.BottomLeftCorner => "nt-toast-bottom-left-corner",
            _ => "nt-toast-bottom-right-corner"
        })
        .Build() ?? string.Empty;
}

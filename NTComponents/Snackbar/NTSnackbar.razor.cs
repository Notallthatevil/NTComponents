using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Snackbar;

using NTComponents.CodeDocumentation;
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
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a snackbar host and registers the browser snackbar bridge.",
    CompatibilityDetails = "Static SSR emits the host container and page script. Displaying queued snackbars requires the browser module or the interactive snackbar service after the page reaches the browser.")]
public partial class NTSnackbar {
    private const string _queueScriptFormat = """
    (() => {{
        const snackbar = {{
    {0}
        }};
        if (window.NTSnackbar?.queueSnackbar) {{
            window.NTSnackbar.queueSnackbar(snackbar);
            return;
        }}

        (window.__ntSnackbarPendingQueue ??= []).push(snackbar);
    }})();
    """;

    /// <summary>
    ///     Returns a static helper script that queues a snackbar after the <c>window.NTSnackbar</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderQueueScript(string message) {
        ArgumentNullException.ThrowIfNull(message);
        return RenderQueueScript(new NTSnackbarQueueScriptOptions { Message = message });
    }

    /// <summary>
    ///     Returns a static helper script that queues a snackbar after the <c>window.NTSnackbar</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderQueueScript(NTSnackbarQueueScriptOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        var message = options.Message;
        ArgumentNullException.ThrowIfNull(message);
        var properties = new StringBuilder();
        var hasPreviousProperty = false;
        AppendStringProperty(properties, ref hasPreviousProperty, "message", message);
        AppendStringProperty(properties, ref hasPreviousProperty, "actionLabel", options.ActionLabel);
        AppendNumberProperty(properties, ref hasPreviousProperty, "timeout", options.Timeout);
        AppendBooleanProperty(properties, ref hasPreviousProperty, "showClose", options.ShowClose);
        AppendColorProperty(properties, ref hasPreviousProperty, "backgroundColor", options.BackgroundColor);
        AppendColorProperty(properties, ref hasPreviousProperty, "textColor", options.TextColor);
        AppendColorProperty(properties, ref hasPreviousProperty, "actionColor", options.ActionColor);
        AppendStringProperty(properties, ref hasPreviousProperty, "id", options.Id);
        AppendStringProperty(properties, ref hasPreviousProperty, "host", options.Host);

        var script = string.Format(CultureInfo.InvariantCulture, _queueScriptFormat, properties);
        return builder => {
            builder.OpenElement(0, "script");
            builder.AddMarkupContent(1, script);
            builder.CloseElement();
        };
    }

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

    private static void AppendBooleanProperty(StringBuilder builder, ref bool hasPreviousProperty, string name, bool? value) {
        if (value.HasValue) {
            AppendPropertyName(builder, ref hasPreviousProperty, name);
            builder.Append(value.Value ? "true" : "false");
        }
    }

    private static void AppendColorProperty(StringBuilder builder, ref bool hasPreviousProperty, string name, TnTColor? value) {
        if (value.HasValue) {
            AppendStringProperty(builder, ref hasPreviousProperty, name, value.Value.ToCssTnTColorVariable());
        }
    }

    private static void AppendNumberProperty(StringBuilder builder, ref bool hasPreviousProperty, string name, int? value) {
        if (value.HasValue) {
            AppendPropertyName(builder, ref hasPreviousProperty, name);
            builder.Append(value.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void AppendPropertyName(StringBuilder builder, ref bool hasPreviousProperty, string name) {
        if (hasPreviousProperty) {
            builder.AppendLine(",");
        }

        hasPreviousProperty = true;
        builder.Append("        ");
        builder.Append(name);
        builder.Append(": ");
    }

    private static void AppendStringProperty(StringBuilder builder, ref bool hasPreviousProperty, string name, string? value) {
        if (value is not null) {
            AppendPropertyName(builder, ref hasPreviousProperty, name);
            builder.Append('"');
            builder.Append(JavaScriptEncoder.Default.Encode(value));
            builder.Append('"');
        }
    }
}

/// <summary>
///     Options for rendering an <see cref="NTSnackbar" /> static queue script.
/// </summary>
public sealed class NTSnackbarQueueScriptOptions {
    /// <summary>
    ///     Gets or sets the action text color.
    /// </summary>
    public TnTColor? ActionColor { get; set; }

    /// <summary>
    ///     Gets or sets the action label, when an action is available.
    /// </summary>
    public string? ActionLabel { get; set; }

    /// <summary>
    ///     Gets or sets the snackbar container background color.
    /// </summary>
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the host element id to target, when a specific host should receive the snackbar.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    ///     Gets or sets a caller-provided snackbar id. Explicit ids are used to prevent duplicate pending, queued, or active snackbars.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     Gets or sets the supporting text shown in the snackbar.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the dismiss affordance should be shown.
    /// </summary>
    public bool? ShowClose { get; set; }

    /// <summary>
    ///     Gets or sets the supporting text color.
    /// </summary>
    public TnTColor? TextColor { get; set; }

    /// <summary>
    ///     Gets or sets the timeout in seconds before auto-dismiss. Zero or less disables auto-dismiss.
    /// </summary>
    public int? Timeout { get; set; }
}

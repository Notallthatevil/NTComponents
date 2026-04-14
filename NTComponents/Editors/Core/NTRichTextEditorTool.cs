using Microsoft.AspNetCore.Components;

namespace NTComponents;

/// <summary>
///     Represents a rich text editor tool plugin that can provide panel markup and a client-side module.
/// </summary>
public interface INTRichTextEditorTool {

    /// <summary>
    ///     Gets the action name handled by the tool plugin.
    /// </summary>
    string Action { get; }

    /// <summary>
    ///     Gets the optional JavaScript module path that registers the tool plugin in the browser.
    /// </summary>
    string? JsModulePath { get; }

    /// <summary>
    ///     Gets the optional panel template rendered by the editor for this tool.
    /// </summary>
    RenderFragment<bool>? PanelTemplate { get; }
}

/// <summary>
///     Represents a rich text editor tool plugin definition.
/// </summary>
public sealed class NTRichTextEditorTool : INTRichTextEditorTool {

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTRichTextEditorTool" /> class.
    /// </summary>
    /// <param name="action">The command action handled by the tool plugin.</param>
    /// <param name="panelTemplate">The optional panel template rendered for the tool.</param>
    /// <param name="jsModulePath">The optional JavaScript module path that registers the tool.</param>
    public NTRichTextEditorTool(string action, RenderFragment<bool>? panelTemplate = null, string? jsModulePath = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        Action = action;
        PanelTemplate = panelTemplate;
        JsModulePath = jsModulePath;
    }

    /// <inheritdoc />
    public string Action { get; }

    /// <inheritdoc />
    public string? JsModulePath { get; }

    /// <inheritdoc />
    public RenderFragment<bool>? PanelTemplate { get; }
}

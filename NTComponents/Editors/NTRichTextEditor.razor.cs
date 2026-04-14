using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
/// Rich text editor shell. Rendering, formatting, SSR enhancement, and tool behavior live in TypeScript; this class
/// only supplies parameters, markup metadata, and optional interactive callbacks.
/// </summary>
public partial class NTRichTextEditor : ITnTPageScriptComponent<NTRichTextEditor> {

    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Gets the .NET object reference used for JavaScript interop.
    /// </summary>
    public DotNetObjectReference<NTRichTextEditor>? DotNetObjectRef { get; private set; }

    /// <summary>
    /// Gets the isolated JavaScript module used by the component.
    /// </summary>
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <inheritdoc />
    public string? JsModulePath => "./_content/NTComponents/Editors/NTRichTextEditor.razor.js";

    /// <inheritdoc />
    public override InputType Type => InputType.TextArea;

    private MarkupString _markupValue;

    /// <summary>
    /// Gets the current editor HTML markup produced by the TypeScript editor surface.
    /// </summary>
    public MarkupString MarkupValue => _markupValue;

    /// <summary>
    /// Invoked when <see cref="MarkupValue" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<MarkupString> MarkupValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the toolbar items rendered by the editor.
    /// </summary>
    /// <remarks>
    /// When not provided, <see cref="DefaultToolbarButtons" /> is used.
    /// </remarks>
    [Parameter]
    public IReadOnlyList<INTRichTextEditorButton>? ToolbarButtons { get; set; }

    /// <summary>
    /// Gets or sets the tool plugins available to the editor.
    /// </summary>
    /// <remarks>
    /// When not provided, <see cref="DefaultTools" /> is used.
    /// </remarks>
    [Parameter]
    public IReadOnlyList<INTRichTextEditorTool>? Tools { get; set; }

    /// <summary>
    /// Gets the default toolbar items used by the editor.
    /// </summary>
    public static readonly IReadOnlyList<INTRichTextEditorButton> DefaultToolbarButtons = [
            new NTRichTextEditorButton("undo", "Undo", "Undo", icon: MaterialIcon.Undo, shortcut: "Ctrl+Z"),
            new NTRichTextEditorButton("redo", "Redo", "Redo", icon: MaterialIcon.Redo, shortcut: "Ctrl+Y"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("paragraph", "Paragraph", "Paragraph", icon: MaterialIcon.Subject, shortcut: "Ctrl+Alt+0"),
            new NTRichTextEditorButton("heading", "Heading 1", "Heading 1", icon: MaterialIcon.LooksOne, value: "1", shortcut: "Ctrl+Alt+1"),
            new NTRichTextEditorButton("heading", "Heading 2", "Heading 2", icon: MaterialIcon.LooksTwo, value: "2", shortcut: "Ctrl+Alt+2"),
            new NTRichTextEditorButton("heading", "Heading 3", "Heading 3", icon: MaterialIcon.Looks3, value: "3", shortcut: "Ctrl+Alt+3"),
            new NTRichTextEditorButton("heading", "Heading 4", "Heading 4", icon: MaterialIcon.Looks4, value: "4", shortcut: "Ctrl+Alt+4"),
            new NTRichTextEditorButton("heading", "Heading 5", "Heading 5", icon: MaterialIcon.Looks5, value: "5", shortcut: "Ctrl+Alt+5"),
            new NTRichTextEditorButton("heading", "Heading 6", "Heading 6", icon: MaterialIcon.Looks6, value: "6", shortcut: "Ctrl+Alt+6"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("alignLeft", "Align Left", "Align left", icon: MaterialIcon.FormatAlignLeft, shortcut: "Ctrl+Shift+L"),
            new NTRichTextEditorButton("alignCenter", "Align Center", "Align center", icon: MaterialIcon.FormatAlignCenter, shortcut: "Ctrl+Shift+E"),
            new NTRichTextEditorButton("alignRight", "Align Right", "Align right", icon: MaterialIcon.FormatAlignRight, shortcut: "Ctrl+Shift+R"),
            new NTRichTextEditorButton("alignJustify", "Justify", "Justify", icon: MaterialIcon.FormatAlignJustify, shortcut: "Ctrl+Shift+J"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("unorderedList", "Bulleted List", "Bulleted list", icon: MaterialIcon.FormatListBulleted, shortcut: "Ctrl+Alt+7"),
            new NTRichTextEditorButton("orderedList", "Numbered List", "Numbered list", icon: MaterialIcon.FormatListNumbered, shortcut: "Ctrl+Alt+8"),
            new NTRichTextEditorButton("blockquote", "Quote Block", "Quote block", icon: MaterialIcon.FormatQuote, shortcut: "Ctrl+Alt+Q"),
            new NTRichTextEditorButton("codeBlock", "Code Block", "Code block", icon: MaterialIcon.Code, shortcut: "Ctrl+Alt+C"),
            new NTRichTextEditorButton("table", "Insert Table", "Insert table", icon: MaterialIcon.TableChart, shortcut: "Ctrl+Alt+T"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("bold", "Bold", "Bold", icon: MaterialIcon.FormatBold, shortcut: "Ctrl+B"),
            new NTRichTextEditorButton("italic", "Italic", "Italic", icon: MaterialIcon.FormatItalic, shortcut: "Ctrl+I"),
            new NTRichTextEditorButton("underline", "Underline", "Underline", icon: MaterialIcon.FormatUnderlined, shortcut: "Ctrl+U"),
            new NTRichTextEditorButton("strikeThrough", "Strikethrough", "Strikethrough", icon: MaterialIcon.StrikethroughS, shortcut: "Ctrl+Shift+S"),
            new NTRichTextEditorButton("textColor", "Text Color", "Text color", icon: MaterialIcon.FormatColorText, shortcut: "Ctrl+Alt+X"),
            new NTRichTextEditorButton("link", "Insert Link", "Insert link", icon: MaterialIcon.InsertLink, shortcut: "Ctrl+K"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("image", "Insert Image", "Insert image", icon: MaterialIcon.Image, shortcut: "Ctrl+Alt+M"),
            new NTRichTextEditorButton("iframe", "Insert Iframe", "Insert iframe", icon: MaterialIcon.Web, shortcut: "Ctrl+Alt+F")
        ];

    /// <summary>
    /// Gets the default tool plugins used by the editor.
    /// </summary>
    public static readonly IReadOnlyList<INTRichTextEditorTool> DefaultTools = [
            new NTRichTextEditorTool("image", CreateToolPanelTemplate<EditorToolImagePanel>(), "./_content/NTComponents/Editors/Tool/EditorToolImageButton.razor.js"),
            new NTRichTextEditorTool("table", CreateToolPanelTemplate<EditorToolTablePanel>(), "./_content/NTComponents/Editors/Tool/EditorToolTableButton.razor.js"),
            new NTRichTextEditorTool("textColor", CreateToolPanelTemplate<EditorToolTextColorPanel>(), "./_content/NTComponents/Editors/Tool/EditorToolTextColorButton.razor.js"),
            new NTRichTextEditorTool("link", CreateToolPanelTemplate<EditorToolLinkPanel>(), "./_content/NTComponents/Editors/Tool/EditorToolLinkButton.razor.js"),
            new NTRichTextEditorTool("iframe", CreateToolPanelTemplate<EditorToolIframePanel>(), "./_content/NTComponents/Editors/Tool/EditorToolIframeButton.razor.js")
        ];

    /// <summary>
    /// Gets the page script fragment that ensures the editor module is available during SSR rendering.
    /// </summary>
    protected RenderFragment PageScript { get; private set; } = _ => { };

    private IReadOnlyList<INTRichTextEditorTool> _activeToolsToRender = Array.Empty<INTRichTextEditorTool>();

    private IReadOnlyDictionary<string, object>? _editorAttributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="NTRichTextEditor" /> class.
    /// </summary>
    public NTRichTextEditor(IJSRuntime jsRuntime) {
        _jsRuntime = jsRuntime;
        DotNetObjectRef = DotNetObjectReference.Create(this);
    }

    private static RenderFragment<bool> CreateToolPanelTemplate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TPanel>() where TPanel : IComponent =>
        disabled => builder => {
            builder.OpenComponent<TPanel>(0);
            builder.AddAttribute(1, "Disabled", disabled);
            builder.CloseComponent();
        };


    private bool HasToolbarAction(string action) => (ToolbarButtons ?? DefaultToolbarButtons).Any(button =>
        !button.IsDivider && string.Equals(button.Action, action, StringComparison.Ordinal));

    private static string GetToolbarButtonAriaLabel(INTRichTextEditorButton toolbarButton) =>
        !string.IsNullOrWhiteSpace(toolbarButton.AriaLabel)
            ? toolbarButton.AriaLabel
            : !string.IsNullOrWhiteSpace(toolbarButton.Text)
                ? toolbarButton.Text
                : toolbarButton.Title ?? toolbarButton.Action ?? "Rich text editor action";

    internal static string GetToolbarButtonTitle(INTRichTextEditorButton toolbarButton) {
        if (!string.IsNullOrWhiteSpace(toolbarButton.Shortcut)) {
            return toolbarButton.Shortcut;
        }

        return !string.IsNullOrWhiteSpace(toolbarButton.Title)
            ? toolbarButton.Title
            : GetToolbarButtonAriaLabel(toolbarButton);
    }

    internal static string? GetToolbarButtonAriaKeyShortcuts(INTRichTextEditorButton toolbarButton) {
        if (string.IsNullOrWhiteSpace(toolbarButton.Shortcut)) {
            return null;
        }

        return toolbarButton.Shortcut
            .Replace("Ctrl", "Control", StringComparison.OrdinalIgnoreCase)
            .Replace("Cmd", "Meta", StringComparison.OrdinalIgnoreCase)
            .Replace("Option", "Alt", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetToolbarButtonClass(INTRichTextEditorButton toolbarButton) => CssClassBuilder.Create()
        .AddClass("tnt-rich-text-editor-toolbar-button")
        .AddClass(toolbarButton.CssClass, !string.IsNullOrWhiteSpace(toolbarButton.CssClass))
        .Build();

    private static string GetToolbarDividerClass(INTRichTextEditorButton toolbarButton) => CssClassBuilder.Create()
        .AddClass("tnt-rich-text-editor-toolbar-divider")
        .AddClass(toolbarButton.CssClass, !string.IsNullOrWhiteSpace(toolbarButton.CssClass))
        .Build();

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        _activeToolsToRender = (Tools ?? DefaultTools)
            .Where(tool => HasToolbarAction(tool.Action))
            .ToArray();

        _editorAttributes = AdditionalAttributes?
            .Where(kvp => kvp.Key is not ("autocomplete" or "autofocus" or "class" or "disabled" or "id" or "lang" or "maxlength" or "minlength" or "name" or "placeholder" or "readonly" or "required" or "style" or "title" or "type" or "value"))
            .ToDictionary();

        PageScript = CreatePageScript(_activeToolsToRender);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override async ValueTask SetFocusAsync() {
        if (!RendererInfo.IsInteractive || IsolatedJsModule is null) {
            await base.SetFocusAsync();
            return;
        }

        try {
            await IsolatedJsModule.InvokeVoidAsync("focusEditor", Element);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore.
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        // Static SSR enhancement is handled by TnTPageScript in the browser. The .NET callback bridge is only available once the renderer becomes interactive.
        if (!RendererInfo.IsInteractive) {
            return;
        }

        try {
            if (firstRender) {
                IsolatedJsModule = await _jsRuntime.ImportIsolatedJs(this, JsModulePath);
                await (IsolatedJsModule?.InvokeVoidAsync("onLoad", Element, DotNetObjectRef) ?? ValueTask.CompletedTask);
            }

            await (IsolatedJsModule?.InvokeVoidAsync("onUpdate", Element, DotNetObjectRef) ?? ValueTask.CompletedTask);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during render.
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    /// <summary>
    /// Synchronizes the editor value from JavaScript while the user is typing.
    /// </summary>
    /// <param name="value">The current Markdown value.</param>
    /// <param name="html">The current HTML value.</param>
    [JSInvokable]
    public async Task UpdateValueFromJs(string value, string html) {
        CurrentValueAsString = value;
        await SetMarkupValueAsync(html);

        if (BindOnInput) {
            await BindAfter.InvokeAsync(CurrentValue);
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Commits the editor value and notifies the form that the field changed.
    /// </summary>
    /// <param name="value">The current Markdown value.</param>
    /// <param name="html">The current HTML value.</param>
    [JSInvokable]
    public async Task CommitValueFromJs(string value, string html) {
        var previousValue = CurrentValueAsString ?? string.Empty;

        CurrentValueAsString = value;
        await SetMarkupValueAsync(html);
        EditContext?.NotifyFieldChanged(FieldIdentifier);

        if (!BindOnInput || !string.Equals(previousValue, value, StringComparison.Ordinal)) {
            await BindAfter.InvokeAsync(CurrentValue);
        }

        await OnBlurCallback.InvokeAsync(new FocusEventArgs());
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Synchronizes the editor HTML value from JavaScript without changing the bound Markdown value.
    /// </summary>
    /// <param name="html">The current HTML value.</param>
    [JSInvokable]
    public Task UpdateMarkupValueFromJs(string html) => SetMarkupValueAsync(html);

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Releases the JavaScript module asynchronously.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore() {
        if (IsolatedJsModule is not null) {
            try {
                await IsolatedJsModule.InvokeVoidAsync("onDispose", Element, DotNetObjectRef);
                await IsolatedJsModule.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during disposal.
            }

            IsolatedJsModule = null;
        }

        DotNetObjectRef?.Dispose();
        DotNetObjectRef = null;
    }

    private async Task SetMarkupValueAsync(string? html) {
        var nextMarkupValue = new MarkupString(html ?? string.Empty);
        if (string.Equals(_markupValue.Value, nextMarkupValue.Value, StringComparison.Ordinal)) {
            return;
        }

        _markupValue = nextMarkupValue;
        await MarkupValueChanged.InvokeAsync(_markupValue);
    }

    private RenderFragment CreatePageScript(IReadOnlyList<INTRichTextEditorTool> activeToolsToRender) => builder => {
        builder.OpenComponent<TnTPageScript>(0);
        builder.AddAttribute(1, nameof(TnTPageScript.Src), JsModulePath);
        builder.CloseComponent();

        var sequence = 2;
        foreach (var jsModulePath in activeToolsToRender
                     .Select(tool => tool.JsModulePath)
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.Ordinal)) {
            builder.OpenComponent<TnTPageScript>(sequence++);
            builder.AddAttribute(sequence++, nameof(TnTPageScript.Src), jsModulePath);
            builder.CloseComponent();
        }
    };
}

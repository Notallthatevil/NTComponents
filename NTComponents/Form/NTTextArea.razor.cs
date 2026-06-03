using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.Ext;
using NTComponents.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace NTComponents;

/// <summary>
///     A Material 3 aligned multiline text field.
/// </summary>
/// <remarks>
///     Use <see cref="NTTextArea" /> for free-form multiline text such as notes, descriptions, comments, and messages.
///     Prefer <see cref="NTInputText" /> for short single-line values, search, email, telephone, URL, and password entry.
/// </remarks>
/// <remarks>
///     Keep labels concise and use supporting text for limits, formatting expectations, or privacy context. Add native
///     <c>maxlength</c> with the built-in character counter for bounded fields. Use <see cref="MinVisibleLines" /> for
///     the initial native <c>rows</c> value, and pass native <c>cols</c>, <c>wrap</c>, and <c>spellcheck</c> attributes
///     through <c>AdditionalAttributes</c> when the browser behavior should be explicit.
/// </remarks>
public partial class NTTextArea : ITnTPageScriptComponent<NTTextArea> {
    private const string TextAreaJsModulePath = "./_content/NTComponents/Form/NTTextArea.razor.js";

    private static readonly HashSet<string> TextAreaExplicitControlAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "name",
        "title",
        "autofocus",
        "autocomplete",
        "readonly",
        "disabled",
        "required",
        "min",
        "max",
        "type",
        "placeholder",
        "aria-describedby",
        "aria-invalid",
        "aria-errormessage",
        "oninput",
        "value"
    };

    /// <summary>
    ///     Gets the .NET object reference used by JavaScript interop after an interactive renderer is attached.
    /// </summary>
    public DotNetObjectReference<NTTextArea>? DotNetObjectRef { get; private set; }

    /// <summary>
    ///     Gets the isolated JavaScript module used by the interactive renderer.
    /// </summary>
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <inheritdoc />
    public string? JsModulePath => TextAreaJsModulePath;

    string? ITnTComponentBase.ElementClass => GetRootClass(!string.IsNullOrWhiteSpace(CurrentErrorText));

    string? ITnTComponentBase.ElementStyle => ElementStyle;

    /// <summary>
    ///     Gets the JavaScript runtime for interactive module loading.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTTextArea" /> class.
    /// </summary>
    public NTTextArea() => DotNetObjectRef = DotNetObjectReference.Create(this);

    /// <summary>
    ///     Gets or sets a value indicating whether the textarea should grow vertically as the user types.
    /// </summary>
    /// <remarks>
    ///     This is enabled by default so multiline text remains visible without forcing users into an internal scrollbar.
    ///     Set <see cref="MaxVisibleLines" /> when the field should stop growing after a known number of visible lines.
    /// </remarks>
    [Parameter]
    public bool AutoGrow { get; set; } = true;

    /// <summary>
    ///     Gets or sets the minimum number of visible text lines.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>2</c>, which is also the initial rendered row count. Autogrow textareas will not shrink below
    ///     this number of visible lines.
    /// </remarks>
    [Parameter]
    public int MinVisibleLines { get; set; } = 2;

    /// <summary>
    ///     Gets or sets the maximum number of visible text lines before the textarea starts scrolling.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>5</c> so autogrow textareas expand for short content, then use internal scrolling for longer
    ///     content. Set to <see langword="null" /> only when unbounded vertical growth is intended.
    /// </remarks>
    [Parameter]
    public int? MaxVisibleLines { get; set; } = 5;

    /// <summary>
    ///     Gets or sets a value indicating whether the browser should size the textarea to its content when supported.
    /// </summary>
    /// <remarks>
    ///     Use this for compact comments or notes where content-driven height is useful. For long-form fields, prefer a
    ///     stable row count so surrounding layouts do not shift while users type.
    /// </remarks>
    [Parameter]
    public bool SizeByContent { get; set; }

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitControlAttributeNames => TextAreaExplicitControlAttributeNames;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-textarea";

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?>? BuildAdditionalInputAttributes() {
        var attributes = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        attributes["rows"] = MinVisibleLines.ToString(CultureInfo.InvariantCulture);

        if (ShouldAutoGrow) {
            attributes["data-nt-textarea-autogrow"] = "true";
            attributes["data-nt-textarea-min-visible-lines"] = MinVisibleLines.ToString(CultureInfo.InvariantCulture);

            if (MaxVisibleLines is int maxVisibleLines) {
                attributes["data-nt-textarea-max-visible-lines"] = maxVisibleLines.ToString(CultureInfo.InvariantCulture);
            }
        }

        var style = BuildTextAreaStyle();
        if (style is not null) {
            attributes["style"] = style;
        }

        return attributes.Count == 0 ? null : attributes;
    }

    /// <inheritdoc />
    protected override void BuildAdditionalRootClasses(StringBuilder builder) {
        builder.Append(" nt-textarea");
        if (ShouldAutoGrow) {
            builder.Append(" nt-textarea-autogrow");
        }

        if (SizeByContent) {
            builder.Append(" nt-textarea-size-by-content");
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (MinVisibleLines < 1) {
            throw new InvalidOperationException($"{nameof(MinVisibleLines)} must be a positive number.");
        }

        if (MaxVisibleLines is < 1) {
            throw new InvalidOperationException($"{nameof(MaxVisibleLines)} must be a positive number when specified.");
        }

        if (MaxVisibleLines is int maxVisibleLines && maxVisibleLines < MinVisibleLines) {
            throw new InvalidOperationException($"{nameof(MaxVisibleLines)} must be greater than or equal to {nameof(MinVisibleLines)}.");
        }

        base.OnParametersSet();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        await DisposeJsModuleAsync();
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        if (!RendererInfo.IsInteractive) {
            return;
        }

        try {
            if (!ShouldAutoGrow) {
                await DisposeJsModuleAsync();
                return;
            }

            if (IsolatedJsModule is null) {
                IsolatedJsModule = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                await IsolatedJsModule.InvokeVoidAsync("onLoad", Element, DotNetObjectRef);
            }

            await IsolatedJsModule.InvokeVoidAsync("onUpdate", Element, DotNetObjectRef);
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

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
        }

        base.Dispose(disposing);
    }

    private bool ShouldAutoGrow => AutoGrow && !SizeByContent;

    private async ValueTask DisposeJsModuleAsync() {
        if (IsolatedJsModule is null) {
            return;
        }

        try {
            await IsolatedJsModule.InvokeVoidAsync("onDispose", Element, DotNetObjectRef);
            await IsolatedJsModule.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during disposal.
        }

        IsolatedJsModule = null;
    }

    private string? BuildTextAreaStyle() {
        var style = GetAdditionalAttributeString("style");
        if (!SizeByContent) {
            return style;
        }

        const string FieldSizingStyle = "field-sizing:content;";
        return string.IsNullOrWhiteSpace(style)
            ? FieldSizingStyle
            : $"{style.TrimEnd().TrimEnd(';')};{FieldSizingStyle}";
    }
}

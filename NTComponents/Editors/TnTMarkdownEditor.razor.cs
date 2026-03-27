using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     A Blazor component for editing Markdown content.
/// </summary>
public partial class TnTMarkdownEditor {

    /// <inheritdoc />
    public override string? ElementClass => string.Empty;

    /// <inheritdoc />
    public override string? ElementStyle => string.Empty;

    /// <summary>
    ///     The initial value of the Markdown content.
    /// </summary>
    [Parameter]
    public string? InitialValue { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Editors/TnTMarkdownEditor.razor.js";

    /// <summary>
    ///     Gets or sets the rendered HTML content.
    /// </summary>
    [Parameter]
    public MarkupString? RenderedHtml { get; set; }

    /// <summary>
    ///     Event callback for when the rendered HTML content changes.
    /// </summary>
    [Parameter]
    public EventCallback<MarkupString?> RenderedHtmlChanged { get; set; }

    /// <summary>
    ///     An expression that identifies the rendered HTML value for validation.
    /// </summary>
    [Parameter]
    public Expression<Func<MarkupString?>>? RenderedHtmlExpression { get; set; }

    /// <summary>
    ///     Gets or sets the Markdown content.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>
    ///     Event callback for when the Markdown content changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    ///     An expression that identifies the Markdown value for validation.
    /// </summary>
    [Parameter]
    public Expression<Func<string?>>? ValueExpression { get; set; }

    /// <summary>
    ///     The callback that is invoked when the editor loses focus.
    /// </summary>
    [Parameter]
    public EventCallback<FocusEventArgs> OnBlurCallback { get; set; }

    /// <summary>
    ///     Text that provides additional context or validation hints for the editor.
    /// </summary>
    [Parameter]
    public string? SupportingText { get; set; }

    /// <summary>
    ///     The cascading edit context used for validation.
    /// </summary>
    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    private FieldIdentifier? _renderedHtmlFieldIdentifier;
    private FieldIdentifier? _valueFieldIdentifier;
    private EditContext? _previousEditContext;

    private string EditorContainerClass => CssClassBuilder.Create("tnt-markdown-editor")
        .AddClass(GetValidationCssClass())
        .Build();

    private string CurrentEditorValue =>
        ValueChanged.HasDelegate || ValueExpression is not null
            ? Value ?? string.Empty
            : Value ?? InitialValue ?? string.Empty;

    private bool ShowValidationMessages => EditContext is not null && (ValueExpression is not null || RenderedHtmlExpression is not null);

    private bool ShowSupportingText => !string.IsNullOrWhiteSpace(SupportingText) || ShowValidationMessages;

    /// <summary>
    ///     Updates the Markdown content and the rendered HTML content.
    /// </summary>
    /// <param name="value">       The new Markdown content.</param>
    /// <param name="renderedText">The rendered HTML content.</param>
    [JSInvokable]
    public async Task UpdateValue(string value, string renderedText) {
        Value = value;
        await ValueChanged.InvokeAsync(Value);
        var body = renderedText;
        var match = BodyRegex().Match(renderedText);
        if (match.Success) {
            body = match.Groups[1].Value;
        }
        RenderedHtml = new MarkupString(body);
        await RenderedHtmlChanged.InvokeAsync(RenderedHtml);
        NotifyFieldChanged(_valueFieldIdentifier);
        NotifyFieldChanged(_renderedHtmlFieldIdentifier);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     Notifies the edit context and executes the blur callback when focus leaves the editor.
    /// </summary>
    [JSInvokable]
    public async Task HandleBlurAsync() {
        NotifyFieldChanged(_valueFieldIdentifier);
        NotifyFieldChanged(_renderedHtmlFieldIdentifier);
        await OnBlurCallback.InvokeAsync(new FocusEventArgs());
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        ElementId = TnTComponentIdentifier.NewId();
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (!ReferenceEquals(_previousEditContext, EditContext)) {
            _previousEditContext?.OnValidationStateChanged -= HandleValidationStateChanged;
            EditContext?.OnValidationStateChanged += HandleValidationStateChanged;

            _previousEditContext = EditContext;
        }

        _valueFieldIdentifier = EditContext is not null && ValueExpression is not null
            ? FieldIdentifier.Create(ValueExpression)
            : null;

        _renderedHtmlFieldIdentifier = EditContext is not null && RenderedHtmlExpression is not null
            ? FieldIdentifier.Create(RenderedHtmlExpression)
            : null;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing && _previousEditContext is not null) {
            _previousEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
        }
        base.Dispose(disposing);
    }

    private void AddFieldCssClass(FieldIdentifier? fieldIdentifier, ISet<string> classes) {
        if (EditContext is null || fieldIdentifier is null) {
            return;
        }

        var fieldClass = EditContext.FieldCssClass(fieldIdentifier.Value);
        if (string.IsNullOrWhiteSpace(fieldClass)) {
            return;
        }

        foreach (var token in fieldClass.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
            classes.Add(token);
        }
    }

    private string? GetValidationCssClass() {
        if (EditContext is null) {
            return null;
        }

        var classes = new HashSet<string>(StringComparer.Ordinal);
        AddFieldCssClass(_valueFieldIdentifier, classes);
        AddFieldCssClass(_renderedHtmlFieldIdentifier, classes);

        if (classes.Contains("invalid")) {
            classes.Remove("valid");
        }

        return classes.Count > 0 ? string.Join(' ', classes) : null;
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) {
        InvokeAsync(StateHasChanged);
    }

    private void NotifyFieldChanged(FieldIdentifier? fieldIdentifier) {
        if (EditContext is not null && fieldIdentifier.HasValue) {
            EditContext.NotifyFieldChanged(fieldIdentifier.Value);
        }
    }

    [GeneratedRegex(@"<body>((.|\r|\n)*)<\/body>")]
    private static partial Regex BodyRegex();
}

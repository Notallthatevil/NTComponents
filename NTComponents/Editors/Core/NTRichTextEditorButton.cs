namespace NTComponents;

/// <summary>
///     Represents a toolbar item that can be rendered by <see cref="NTRichTextEditor" />.
/// </summary>
public interface INTRichTextEditorButton {

    /// <summary>
    ///     Gets a value indicating whether this toolbar item should render as a divider instead of a button.
    /// </summary>
    bool IsDivider { get; }

    /// <summary>
    ///     Gets the action that will be sent to the editor JavaScript command handler.
    /// </summary>
    string? Action { get; }

    /// <summary>
    ///     Gets an optional command value used by actions such as heading level selection.
    /// </summary>
    string? Value { get; }

    /// <summary>
    ///     Gets the tooltip shown for the toolbar item.
    /// </summary>
    string? Title { get; }

    /// <summary>
    ///     Gets the optional keyboard shortcut shown for the toolbar item.
    /// </summary>
    string? Shortcut { get; }

    /// <summary>
    ///     Gets the accessible name applied to the toolbar item.
    /// </summary>
    string? AriaLabel { get; }

    /// <summary>
    ///     Gets the optional icon rendered inside the toolbar item.
    /// </summary>
    TnTIcon? Icon { get; }

    /// <summary>
    ///     Gets the optional text rendered inside the toolbar item.
    /// </summary>
    string? Text { get; }

    /// <summary>
    ///     Gets the optional CSS class applied to the toolbar item.
    /// </summary>
    string? CssClass { get; }
}

/// <summary>
///     Represents a clickable rich text editor toolbar button.
/// </summary>
public sealed class NTRichTextEditorButton : INTRichTextEditorButton {

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTRichTextEditorButton" /> class.
    /// </summary>
    /// <param name="action">The action sent to the editor JavaScript command handler.</param>
    /// <param name="title">The tooltip shown for the toolbar item.</param>
    /// <param name="ariaLabel">The accessible name for the toolbar item.</param>
    /// <param name="icon">The optional icon rendered for the toolbar item.</param>
    /// <param name="text">The optional text rendered for the toolbar item.</param>
    /// <param name="value">The optional command value used by the action.</param>
    /// <param name="shortcut">The optional keyboard shortcut shown for the toolbar item.</param>
    /// <param name="cssClass">Optional additional CSS classes applied to the toolbar item.</param>
    public NTRichTextEditorButton(string action, string title, string? ariaLabel = null, TnTIcon? icon = null, string? text = null, string? value = null, string? shortcut = null, string? cssClass = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (icon is null && string.IsNullOrWhiteSpace(text)) {
            throw new ArgumentException("A rich text editor button must include an icon or text.", nameof(text));
        }

        Action = action;
        Title = title;
        AriaLabel = string.IsNullOrWhiteSpace(ariaLabel) ? title : ariaLabel;
        Icon = icon;
        Text = text;
        Value = value;
        Shortcut = shortcut;
        CssClass = cssClass;
    }

    /// <inheritdoc />
    public bool IsDivider => false;

    /// <inheritdoc />
    public string Action { get; }

    /// <inheritdoc />
    public string? Value { get; }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public string? Shortcut { get; }

    /// <inheritdoc />
    public string AriaLabel { get; }

    /// <inheritdoc />
    public TnTIcon? Icon { get; }

    /// <inheritdoc />
    public string? Text { get; }

    /// <inheritdoc />
    public string? CssClass { get; }
}

/// <summary>
///     Represents a visual divider in the rich text editor toolbar.
/// </summary>
public sealed class NTRichTextEditorButtonDivider : INTRichTextEditorButton {

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTRichTextEditorButtonDivider" /> class.
    /// </summary>
    /// <param name="cssClass">Optional additional CSS classes applied to the divider.</param>
    public NTRichTextEditorButtonDivider(string? cssClass = null) => CssClass = cssClass;

    /// <inheritdoc />
    public bool IsDivider => true;

    /// <inheritdoc />
    public string? Action => null;

    /// <inheritdoc />
    public string? Value => null;

    /// <inheritdoc />
    public string? Title => null;

    /// <inheritdoc />
    public string? Shortcut => null;

    /// <inheritdoc />
    public string? AriaLabel => null;

    /// <inheritdoc />
    public TnTIcon? Icon => null;

    /// <inheritdoc />
    public string? Text => null;

    /// <inheritdoc />
    public string? CssClass { get; }
}

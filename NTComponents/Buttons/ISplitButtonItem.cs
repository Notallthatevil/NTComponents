using Microsoft.AspNetCore.Components;

namespace NTComponents;

/// <summary>
///     Describes a constrained item that can be rendered inside an <see cref="NTSplitButton" /> menu.
/// </summary>
public interface ISplitButtonItem {

    /// <summary>
    ///     Captures unmatched attributes passed to the item.
    /// </summary>
    IReadOnlyDictionary<string, object?>? AdditionalAttributes { get; }

    /// <summary>
    ///     Provides an accessible label when the visible label needs extra context.
    /// </summary>
    string? AriaLabel { get; }

    /// <summary>
    ///     Marks the item as disabled.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    ///     Optional icon rendered before the label.
    /// </summary>
    TnTIcon? Icon { get; }

    /// <summary>
    ///     Visible menu item text when the menu entry has text content.
    /// </summary>
    string Label { get; }

    /// <summary>
    ///     Renders the item inside the owning split button menu.
    /// </summary>
    RenderFragment Render(NTSplitButton owner);
}

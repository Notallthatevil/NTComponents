using Microsoft.AspNetCore.Components;

namespace NTComponents;

/// <summary>
///     Describes a constrained item that can be rendered inside an <see cref="NTFabMenu" />.
/// </summary>
public interface IFabMenuItem {

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
    ///     Icon rendered before the label.
    /// </summary>
    TnTIcon? Icon { get; }

    /// <summary>
    ///     Visible menu item text.
    /// </summary>
    string Label { get; }

    /// <summary>
    ///     Renders the item inside the owning FAB menu.
    /// </summary>
    RenderFragment Render(NTFabMenu owner);
}

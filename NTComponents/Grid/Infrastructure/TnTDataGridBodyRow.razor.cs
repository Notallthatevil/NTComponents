using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Core;

namespace NTComponents.Grid.Infrastructure;

/// <summary>
///     Represents a row in the data grid body for a specific grid item.
/// </summary>
/// <typeparam name="TGridItem">The type of the grid item displayed in this row.</typeparam>
public partial class TnTDataGridBodyRow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem> {

    /// <summary>
    ///     Additional attributes to be applied to the row element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     The grid item associated with this row.
    /// </summary>
    [Parameter, EditorRequired]
    public TGridItem Item { get; set; }

    /// <summary>
    ///     Gets or sets a delegate that returns the CSS class name for a given data grid row.
    /// </summary>
    /// <remarks>
    ///     Use this property to customize the appearance of individual rows based on their data or state. The delegate receives the row data as a parameter and should return a string containing one
    ///     or more CSS class names to apply to that row. If the delegate returns null or an empty string, no additional class is applied.
    /// </remarks>
    [Parameter]
    public Func<TGridItem, string>? RowClass { get; set; }

    /// <summary>
    ///     Gets the CSS class string for the row element, including state-based classes.
    /// </summary>
    private string? _elementClass => CssClassBuilder.Create()
        .AddClass("tnt-data-grid-body-row")
        .AddClass("tnt-interactable", Context.RowClickCallback.HasDelegate)
        .AddClass("tnt-stripped", Context.DataGridAppearance.HasFlag(DataGridAppearance.Stripped))
        .AddClass(RowClass is not null ? RowClass(Item) : null!, RowClass is not null)
        .Build();

    private Task HandleKeyDownAsync(KeyboardEventArgs args) {
        if (Context.RowClickCallback.HasDelegate && (args.Key == "Enter" || args.Key == " " || args.Key == "Spacebar")) {
            return Context.RowClickCallback.InvokeAsync(Item);
        }

        return Task.CompletedTask;
    }
}

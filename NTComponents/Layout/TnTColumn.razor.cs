using Microsoft.AspNetCore.Components;
using System.Reflection;
using System.Text;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a column component in a grid layout system. This component follows a 12-column grid system with responsive behavior across different screen sizes.
/// </summary>
[Obsolete("TnTColumn is obsolete.")]
public partial class TnTColumn {

    /// <summary>
    ///     Gets or sets the content to be rendered inside the column.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     Gets the CSS class for the column element, including grid sizing classes.
    /// </summary>
    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-col")
        .AddClass(GetGridClass())
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Gets or sets the column size for large screens (L). Value must be between 0 and 12, where 0 means the column is not used for this screen size.
    /// </summary>
    [Parameter, ColSize(SizeClass = "l", PropertyName = nameof(ColSize.Size))]
    public int L { get; set; }

    /// <summary>
    ///     Gets or sets the offset value for large screens (L). Offset moves the column to the right by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "l", PropertyName = nameof(ColSize.Offset))]
    public int LOffset { get; set; }

    /// <summary>
    ///     Gets or sets the pull value for large screens (L). Pull moves the column to the left by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "l", PropertyName = nameof(ColSize.Pull))]
    public int LPull { get; set; }

    /// <summary>
    ///     Gets or sets the push value for large screens (L). Push moves the column to the right by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "l", PropertyName = nameof(ColSize.Push))]
    public int LPush { get; set; }

    /// <summary>
    ///     Gets or sets the column size for medium screens (M). Value must be between 0 and 12, where 0 means the column is not used for this screen size.
    /// </summary>
    [Parameter, ColSize(SizeClass = "m", PropertyName = nameof(ColSize.Size))]
    public int M { get; set; }

    /// <summary>
    ///     Gets or sets the offset value for medium screens (M). Offset moves the column to the right by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "m", PropertyName = nameof(ColSize.Offset))]
    public int MOffset { get; set; }

    /// <summary>
    ///     Gets or sets the pull value for medium screens (M). Pull moves the column to the left by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "m", PropertyName = nameof(ColSize.Pull))]
    public int MPull { get; set; }

    /// <summary>
    ///     Gets or sets the push value for medium screens (M). Push moves the column to the right by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "m", PropertyName = nameof(ColSize.Push))]
    public int MPush { get; set; }

    /// <summary>
    ///     Gets or sets the column size for small screens (S). Value must be between 0 and 12, where 0 means the column is not used for this screen size.
    /// </summary>
    [Parameter, ColSize(SizeClass = "s", PropertyName = nameof(ColSize.Size))]
    public int S { get; set; }

    /// <summary>
    ///     Gets or sets the offset value for small screens (S). Offset moves the column to the right by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "s", PropertyName = nameof(ColSize.Offset))]
    public int SOffset { get; set; }

    /// <summary>
    ///     Gets or sets the pull value for small screens (S). Pull moves the column to the left by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "s", PropertyName = nameof(ColSize.Pull))]
    public int SPull { get; set; }

    /// <summary>
    ///     Gets or sets the push value for small screens (S). Push moves the column to the right by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "s", PropertyName = nameof(ColSize.Push))]
    public int SPush { get; set; }

    /// <summary>
    ///     Gets or sets the column size for extra large screens (XL). Value must be between 0 and 12, where 0 means the column is not used for this screen size.
    /// </summary>
    [Parameter, ColSize(SizeClass = "xl", PropertyName = nameof(ColSize.Size))]
    public int XL { get; set; }

    /// <summary>
    ///     Gets or sets the offset value for extra large screens (XL). Offset moves the column to the right by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "xl", PropertyName = nameof(ColSize.Offset))]
    public int XLOffset { get; set; }

    /// <summary>
    ///     Gets or sets the pull value for extra large screens (XL). Pull moves the column to the left by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "xl", PropertyName = nameof(ColSize.Pull))]
    public int XLPull { get; set; }

    /// <summary>
    ///     Gets or sets the push value for extra large screens (XL). Push moves the column to the right by the specified number of columns.
    /// </summary>
    [Parameter, ColSize(SizeClass = "xl", PropertyName = nameof(ColSize.Push))]
    public int XLPush { get; set; }

    /// <summary>
    ///     Cached dictionary of grid property information with their corresponding ColSize attribute.
    /// </summary>
    private static readonly IReadOnlyDictionary<PropertyInfo, ColSizeAttribute> _sizeValues = GetSizeProperties();

    /// <summary>
    ///     Dictionary of size classes and their corresponding values.
    /// </summary>
    private readonly Dictionary<string, ColSize> _sizes = [];

    /// <summary>
    ///     Initializes the column component by processing the size properties.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a size value is outside the valid range of 0 to 12.</exception>
    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        foreach (var sizeValuePair in _sizeValues) {
            var prop = sizeValuePair.Key;
            var colSizeAttr = sizeValuePair.Value;
            var value = (int)(prop.GetValue(this) ?? 0);

            if (value is < 0 or > 12) {
                throw new ArgumentOutOfRangeException(prop.Name, "Value must be within 0 and 12 inclusively.");
            }
            if (value > 0) {
                if (!_sizes.TryGetValue(colSizeAttr.SizeClass, out var colSize)) {
                    colSize = new ColSize();
                    _sizes.Add(colSizeAttr.SizeClass, colSize);
                }
                typeof(ColSize).GetProperty(colSizeAttr.PropertyName)!.SetValue(colSize, value);
            }
        }
    }

    /// <summary>
    ///     Gets a dictionary of properties with the ColSize attribute.
    /// </summary>
    /// <returns>A dictionary mapping PropertyInfo objects to their corresponding ColSizeAttribute.</returns>
    private static Dictionary<PropertyInfo, ColSizeAttribute> GetSizeProperties() {
        var sizeValues = new Dictionary<PropertyInfo, ColSizeAttribute>();
        foreach (var prop in typeof(TnTColumn).GetProperties()) {
            var colSize = prop.GetCustomAttribute<ColSizeAttribute>();
            if (colSize is not null) {
                sizeValues.Add(prop, colSize);
            }
        }
        return sizeValues;
    }

    /// <summary>
    ///     Builds the grid class string based on the configured size properties.
    /// </summary>
    /// <returns>A string containing the CSS classes for the grid column.</returns>
    private string GetGridClass() {
        var strBuilder = new StringBuilder();

        if (_sizes.Count == 0) {
            strBuilder.Append(' ').Append("s12");
        }
        else {
            foreach (var sizePair in _sizes) {
                var sizeClass = sizePair.Key;
                var value = sizePair.Value;

                if (value.Size > 0) {
                    strBuilder.Append(' ').Append($"{sizeClass}{value.Size}");

                    if (value.Offset > 0) {
                        strBuilder.Append(' ').Append($"{sizeClass}{value.Offset}-offset");
                    }

                    if (value.Push > 0) {
                        strBuilder.Append(' ').Append($"{sizeClass}{value.Push}-push");
                    }

                    if (value.Pull > 0) {
                        strBuilder.Append(' ').Append($"{sizeClass}{value.Pull}-pull");
                    }
                }
            }
        }

        return strBuilder.ToString();
    }

    /// <summary>
    ///     Represents the size configuration for a column at a specific breakpoint.
    /// </summary>
    private class ColSize {

        /// <summary>
        ///     Gets or sets the column offset value which moves the column right by the specified number of columns.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        ///     Gets or sets the column pull value which moves the column left by the specified number of columns.
        /// </summary>
        public int Pull { get; set; }

        /// <summary>
        ///     Gets or sets the column push value which moves the column right by the specified number of columns.
        /// </summary>
        public int Push { get; set; }

        /// <summary>
        ///     Gets or sets the column size value (1-12) which determines the width of the column.
        /// </summary>
        public int Size { get; set; }
    }

    /// <summary>
    ///     Attribute used to mark properties that represent column size settings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    private class ColSizeAttribute : Attribute {

        /// <summary>
        ///     Gets or sets the property name in the ColSize class that this property maps to.
        /// </summary>
        public string PropertyName { get; set; } = default!;

        /// <summary>
        ///     Gets or sets the CSS class prefix for the screen size (e.g., "s", "m", "l", "xl").
        /// </summary>
        public string SizeClass { get; set; } = default!;
    }
}

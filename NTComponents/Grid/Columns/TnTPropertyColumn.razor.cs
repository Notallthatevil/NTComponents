using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using NTComponents.Ext;
using NTComponents.Grid;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;

namespace NTComponents;

/// <summary>
///     Represents a grid column that displays and sorts a property of <typeparamref name="TGridItem" />. Supports formatting and custom comparers for the property value.
/// </summary>
/// <typeparam name="TGridItem">The type of items in the grid.</typeparam>
/// <typeparam name="TProp">The type of the property displayed in the column.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
public partial class TnTPropertyColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem, TProp> {

    /// <summary>
    ///     Specifies a custom comparer to use for sorting the property values.
    /// </summary>
    [Parameter]
    public IComparer<TProp>? Comparer { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => throw new NotImplementedException();

    /// <summary>
    ///     Specifies a format string to use when displaying the property value. The property type must implement <see cref="IFormattable" /> if this is set.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>
    ///     Specifies the culture to use for formatting the property value. Defaults to <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    [Parameter]
    public CultureInfo FormatCulture { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>
    ///     An expression selecting the property to display in the column.
    /// </summary>
    [Parameter, EditorRequired]
    public Expression<Func<TGridItem, TProp>> Property { get; set; } = default!;

    /// <summary>
    ///     The <see cref="PropertyInfo" /> for the property displayed in the column, if available.
    /// </summary>
    public PropertyInfo? PropertyInfo { get; private set; }

    /// <inheritdoc />
    public override TnTGridSort<TGridItem>? SortBy {
        get => _sortBuilder;
        set => throw new NotSupportedException($"{nameof(TnTPropertyColumn<TGridItem, TProp>)} generates this member internally. For custom sorting rules, see '{typeof(TnTTemplateColumn<TGridItem>)}'.");
    }

    private Func<TGridItem, string>? _cellTextFunc;

    private Expression<Func<TGridItem, TProp>>? _lastAssignedProperty;
    private TnTGridSort<TGridItem>? _sortBuilder;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        // We have to do a bit of pre-processing on the lambda expression. Only do that if it's new or changed.
        if (_lastAssignedProperty != Property) {
            _lastAssignedProperty = Property;
            var compiledPropertyExpression = Property.Compile();

            if (!string.IsNullOrEmpty(Format)) {
                var nullableUnderlyingTypeOrNull = Nullable.GetUnderlyingType(typeof(TProp));
                if (!typeof(IFormattable).IsAssignableFrom(nullableUnderlyingTypeOrNull ?? typeof(TProp))) {
                    throw new InvalidOperationException($"A '{nameof(Format)}' parameter was supplied, but the type '{typeof(TProp)}' does not implement '{typeof(IFormattable)}'.");
                }

                _cellTextFunc = item => {
                    if (item is not null) {
                        var result = ((IFormattable?)compiledPropertyExpression!(item))?.ToString(Format, FormatCulture);
                        return !string.IsNullOrEmpty(result) ? result : " ";
                    }
                    return string.Empty;
                };
            }
            else {
                _cellTextFunc = item => {
                    if (item is not null) {
                        var result = compiledPropertyExpression!(item)?.ToString();
                        return !string.IsNullOrEmpty(result) ? result : " ";
                    }
                    return string.Empty;
                };
            }
            if (InitialSortDirection == SortDirection.Ascending) {
                _sortBuilder ??= Comparer is not null ? TnTGridSort<TGridItem>.ByAscending(Property, Comparer) : TnTGridSort<TGridItem>.ByAscending(Property);
            }
            else {
                _sortBuilder ??= Comparer is not null ? TnTGridSort<TGridItem>.ByDescending(Property, Comparer) : TnTGridSort<TGridItem>.ByDescending(Property);
            }
        }

        if (Title is null && Property.Body is MemberExpression memberExpression) {
            PropertyInfo = memberExpression.Member as PropertyInfo;
            var displayAttribute = Attribute.GetCustomAttribute(memberExpression.Member, typeof(DisplayAttribute)) as DisplayAttribute;

            var daText = displayAttribute?.Name;
            Title = !string.IsNullOrEmpty(daText) ? daText : memberExpression.Member.Name.SplitPascalCase();
        }

        // Call after to register sorting if needed.
        base.OnParametersSet();
    }
}
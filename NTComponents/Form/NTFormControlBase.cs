using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using NTComponents.CodeDocumentation;
using NTComponents.Core;
namespace NTComponents;

/// <summary>
///     Base class for Form controls that participate in an <see cref="NTForm" />.
/// </summary>
/// <remarks>
///     This type owns only shared form-control behavior: form cascade inheritance, stable ids and names, common label and
///     supporting/error text parameters, focus/blur behavior, and additional-attribute helpers. Visual shells, native
///     control semantics, required behavior, and component-specific color tokens belong in derived classes.
/// </remarks>
/// <typeparam name="TValue">The bound value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders useful static HTML and adds Blazor behavior when interactive.",
    CompatibilityDetails = "Static SSR preserves the rendered markup and native browser behavior. EventCallback handlers, bound state updates, and live validation require an interactive render mode.")]
public abstract class NTFormControlBaseCore<TValue> : NTValidationInputBase<TValue> {
    private string _generatedInputId = "nt-form-control-field";
    private string? _generatedInputIdPrefix;
    private bool _generatedInputIdResolved;
    private string? _generatedInputIdSource;
    private string? _resolvedElementName;
    private string? _derivedIdBase;
    private string? _errorTextId;
    private string? _labelId;
    private string? _supportingTextId;
    private string? _fieldCssClassInput;
    private string? _fieldCssClass;
    private string? _fieldCssClassResult;
    private Func<FocusEventArgs, Task>? _blurHandler;

    /// <inheritdoc />
    [Parameter]
    public bool? AutoFocus { get; set; }

    /// <summary>
    ///     Gets or sets the density for this control. When null, the containing <see cref="NTForm" /> density is used.
    /// </summary>
    [Parameter]
    public NTFormDensity? Density { get; set; }

    /// <summary>
    ///     Gets or sets whether this control is disabled. When null, the containing <see cref="NTForm" /> state is used.
    /// </summary>
    [Parameter]
    public bool? Disabled { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether generated validation messages should be hidden.
    /// </summary>
    [Parameter]
    public bool DisableValidationMessage { get; set; }

    /// <summary>
    ///     Gets or sets explicit error text. Explicit error text takes precedence over validation messages.
    /// </summary>
    [Parameter]
    public string? ErrorText { get; set; }

    /// <inheritdoc />
    public ElementReference Element { get; protected set; }

    /// <inheritdoc />
    [Parameter]
    public string? ElementId { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? ElementLang { get; set; }

    /// <inheritdoc />
    public string? ElementName => SubmitValue ? _resolvedElementName : null;

    /// <inheritdoc />
    [Parameter]
    public string? ElementTitle { get; set; }

    /// <summary>
    ///     Gets or sets the visible label.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Gets or sets a callback invoked when the control loses focus.
    /// </summary>
    [Parameter]
    public EventCallback<FocusEventArgs> OnBlurCallback { get; set; }

    /// <summary>
    ///     Gets or sets whether this control is read-only. When null, the containing <see cref="NTForm" /> state is used.
    /// </summary>
    [Parameter]
    public bool? ReadOnly { get; set; }

    /// <summary>
    ///     Gets or sets supporting text shown when the control is not invalid.
    /// </summary>
    [Parameter]
    public string? SupportingText { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the control value is submitted by native HTML form posts.
    /// </summary>
    /// <remarks>
    ///     Keep this enabled for normal form fields. Disable it for transient helper controls whose values are consumed by
    ///     client-side behavior and should not become separate form values during static SSR posts.
    /// </remarks>
    [Parameter]
    public bool SubmitValue { get; set; } = true;

    /// <summary>
    ///     Gets the containing <see cref="NTForm" />.
    /// </summary>
    [CascadingParameter]
    protected NTForm? Form { get; set; }

    /// <summary>
    ///     Gets the current effective density inherited from this control or the containing form.
    /// </summary>
    protected NTFormDensity EffectiveDensity { get; private set; } = NTFormDensity.Standard;

    /// <summary>
    ///     Gets a value indicating whether this control is currently disabled.
    /// </summary>
    protected bool FieldDisabled { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether this control is currently read-only.
    /// </summary>
    protected bool FieldReadOnly { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether visible label text is present.
    /// </summary>
    protected bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    /// <summary>
    ///     Gets the id for error text.
    /// </summary>
    protected string ErrorTextId => GetDerivedId(ref _errorTextId, "-error");

    /// <summary>
    ///     Gets the id for the native control.
    /// </summary>
    protected string InputId => string.IsNullOrWhiteSpace(ElementId) ? _generatedInputId : ElementId;

    /// <summary>
    ///     Gets the stable id used by the rendered native control.
    /// </summary>
    protected string EffectiveInputId => InputId;

    /// <summary>
    ///     Gets the id for label text.
    /// </summary>
    protected string LabelId => GetDerivedId(ref _labelId, "-label");

    /// <summary>
    ///     Gets the id for supporting text.
    /// </summary>
    protected string SupportingTextId => GetDerivedId(ref _supportingTextId, "-supporting");

    /// <summary>
    ///     Gets the current error text.
    /// </summary>
    protected string? CurrentErrorText {
        get {
            if (!string.IsNullOrWhiteSpace(ErrorText)) {
                return ErrorText;
            }

            if (EditContext is null || DisableValidationMessage) {
                return null;
            }

            return EditContext.GetValidationMessages(FieldIdentifier).FirstOrDefault();
        }
    }

    /// <summary>
    ///     Gets the current non-error supporting text.
    /// </summary>
    protected string? CurrentSupportingText {
        get {
            if (!string.IsNullOrWhiteSpace(SupportingText)) {
                return SupportingText;
            }

            if (Form?.ShowRequiredSupportingText == true && HasRequiredSupportingText) {
                return Form.RequiredSupportingText;
            }

            return null;
        }
    }

    /// <summary>
    ///     Gets the generated id prefix for the concrete control.
    /// </summary>
    protected virtual string InputIdPrefix => "nt-form-control";

    /// <summary>
    ///     Gets a value indicating whether the form-level required supporting text should be used.
    /// </summary>
    protected virtual bool HasRequiredSupportingText => false;

    /// <summary>
    ///     Gets the <see cref="OnBlurAsync" /> delegate, created once instead of on every render.
    /// </summary>
    private protected Func<FocusEventArgs, Task> BlurHandler => _blurHandler ??= OnBlurAsync;

    /// <summary>
    ///     Sets focus to the native control.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual ValueTask SetFocusAsync() => Element.FocusAsync();

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        EffectiveDensity = Density ?? Form?.Density ?? NTFormDensity.Standard;
        FieldDisabled = Disabled ?? Form?.Disabled ?? false;
        FieldReadOnly = ReadOnly ?? Form?.ReadOnly ?? false;
        _resolvedElementName = ResolveElementName();
        _generatedInputId = ResolveGeneratedInputId();
        if (EditContext is not null) {
            NTFieldCssClassProvider.Configure(EditContext);
        }
    }

    /// <summary>
    ///     Handles the native blur event.
    /// </summary>
    /// <param name="args">The focus event arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual async Task OnBlurAsync(FocusEventArgs args) {
        if (EditContext is not null && !EditContext.IsModified(FieldIdentifier)) {
            EditContext.NotifyFieldChanged(FieldIdentifier);
        }

        await OnBlurCallback.InvokeAsync(args);
    }

    /// <summary>
    ///     Appends EditContext field CSS classes to a root class value.
    /// </summary>
    /// <param name="className">The root class value.</param>
    /// <returns>The updated root class value.</returns>
    protected string AppendFieldCssClass(string className) => AppendFieldCssClass(className, EditContext?.FieldCssClass(FieldIdentifier));

    /// <summary>
    ///     Appends an already resolved field CSS class, reusing the previous result while both inputs are unchanged.
    /// </summary>
    private protected string AppendFieldCssClass(string className, string? fieldCssClass) {
        if (_fieldCssClassResult is null || !ReferenceEquals(className, _fieldCssClassInput) || !ReferenceEquals(fieldCssClass, _fieldCssClass)) {
            var hasClass = !string.IsNullOrWhiteSpace(className);
            var hasFieldClass = !string.IsNullOrWhiteSpace(fieldCssClass) && !string.Equals(fieldCssClass, className, StringComparison.Ordinal);
            _fieldCssClassResult = (hasClass && hasFieldClass ? $"{className} {fieldCssClass}" : hasClass ? className : hasFieldClass ? fieldCssClass! : string.Empty).Trim();
            _fieldCssClassInput = className;
            _fieldCssClass = fieldCssClass;
        }

        return _fieldCssClassResult;
    }

    /// <summary>
    ///     Appends a CSS custom property that references an NT theme color when the color is set.
    /// </summary>
    private protected static string? AppendColorVariable(string? style, string variableName, NTColor? color) => color.HasValue ? $"{style}--{variableName}:{color.Value.ToCssNTColorVariable()};" : style;

    /// <summary>
    ///     Gets an id derived from <see cref="InputId" />, rebuilding it only after the input id changes.
    /// </summary>
    private protected string GetDerivedId(ref string? cache, string suffix) {
        var inputId = InputId;
        if (!ReferenceEquals(inputId, _derivedIdBase)) {
            _derivedIdBase = inputId;
            ClearDerivedIds();
        }

        return cache ??= inputId + suffix;
    }

    /// <summary>
    ///     Clears the ids cached through <see cref="GetDerivedId" />.
    /// </summary>
    private protected virtual void ClearDerivedIds() {
        _errorTextId = null;
        _labelId = null;
        _supportingTextId = null;
    }

    /// <summary>
    ///     Builds the aria-describedby value from existing additional attributes and internal text ids.
    /// </summary>
    /// <param name="hasErrorText">Whether the control has active error text.</param>
    /// <param name="hasSupportingText">Whether the control has active supporting text.</param>
    /// <returns>The aria-describedby value.</returns>
    protected virtual string? BuildDescribedBy(bool hasErrorText, bool hasSupportingText) {
        var internalDescriptionId = hasErrorText ? ErrorTextId : hasSupportingText ? SupportingTextId : null;
        var externalDescriptionIds = GetAdditionalAttributeString("aria-describedby");

        return (externalDescriptionIds, internalDescriptionId) switch {
            ({ Length: > 0 }, { Length: > 0 }) => $"{externalDescriptionIds} {internalDescriptionId}",
            ({ Length: > 0 }, _) => externalDescriptionIds,
            (_, { Length: > 0 }) => internalDescriptionId,
            _ => null
        };
    }

    /// <summary>
    ///     Appends a described-by id.
    /// </summary>
    /// <param name="current">The current described-by value.</param>
    /// <param name="id">The id to append.</param>
    /// <returns>The updated described-by value.</returns>
    protected static string AppendDescribedById(string? current, string id) => string.IsNullOrEmpty(current) ? id : $"{current} {id}";

    /// <summary>
    ///     Attempts to get an additional attribute by name using case-insensitive lookup.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The resolved value.</param>
    /// <returns><see langword="true" /> when the value exists; otherwise, <see langword="false" />.</returns>
    protected bool TryGetAdditionalAttribute(string name, out object? value) {
        var attributes = AdditionalAttributes;
        if (attributes is null) {
            value = null;
            return false;
        }

        if (attributes.TryGetValue(name, out value)) {
            return true;
        }

        // A case-insensitive dictionary has already answered; otherwise scan, avoiding a boxed enumerator for concrete dictionaries.
        if (attributes is Dictionary<string, object> dictionary) {
            if (dictionary.Comparer != StringComparer.OrdinalIgnoreCase) {
                foreach (var (key, attributeValue) in dictionary) {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) {
                        value = attributeValue;
                        return true;
                    }
                }
            }
        }
        else {
            foreach (var (key, attributeValue) in attributes) {
                if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) {
                    value = attributeValue;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    ///     Builds a filtered additional-attribute dictionary for a native control.
    /// </summary>
    /// <param name="explicitAttributeNames">Attribute names owned by the component.</param>
    /// <param name="additionalControlAttributes">Additional attributes supplied by a derived type.</param>
    /// <returns>The filtered attributes, or null when no attributes remain.</returns>
    protected IReadOnlyDictionary<string, object?>? BuildFilteredAttributes(IEnumerable<string> explicitAttributeNames, IReadOnlyDictionary<string, object?>? additionalControlAttributes = null) {
        // Copy only the attributes that survive filtering so the common all-explicit case allocates nothing.
        Dictionary<string, object?>? attributes = null;
        CopyUnownedAttributes((IReadOnlyDictionary<string, object?>?)AdditionalAttributes, explicitAttributeNames, ref attributes);
        CopyUnownedAttributes(additionalControlAttributes, explicitAttributeNames, ref attributes);
        return attributes;
    }

    /// <summary>
    ///     Gets an additional attribute string value by name.
    /// </summary>
    /// <param name="attributeName">The attribute name.</param>
    /// <returns>The attribute value, or null when missing or blank.</returns>
    protected string? GetAdditionalAttributeString(string attributeName) {
        if (!TryGetAdditionalAttribute(attributeName, out var attributeValue)) {
            return null;
        }

        var stringValue = attributeValue?.ToString();
        return string.IsNullOrWhiteSpace(stringValue) ? null : stringValue;
    }

    private static void CopyUnownedAttributes(IReadOnlyDictionary<string, object?>? source, IEnumerable<string> explicitAttributeNames, ref Dictionary<string, object?>? target) {
        if (source is Dictionary<string, object?> dictionary) {
            foreach (var (key, value) in dictionary) {
                Copy(key, value, explicitAttributeNames, ref target);
            }
        }
        else if (source is not null) {
            foreach (var (key, value) in source) {
                Copy(key, value, explicitAttributeNames, ref target);
            }
        }

        static void Copy(string key, object? value, IEnumerable<string> explicitAttributeNames, ref Dictionary<string, object?>? target) {
            var isExplicit = string.Equals(key, "class", StringComparison.OrdinalIgnoreCase)
                || (explicitAttributeNames is HashSet<string> set && set.Comparer == StringComparer.OrdinalIgnoreCase
                    ? set.Contains(key)
                    : explicitAttributeNames.Contains(key, StringComparer.OrdinalIgnoreCase));
            if (!isExplicit) {
                (target ??= new(StringComparer.OrdinalIgnoreCase))[key] = value;
            }
        }
    }

    private string? ResolveElementName() {
        if (TryGetAdditionalAttribute("name", out var explicitNameValue) && !string.IsNullOrWhiteSpace(explicitNameValue?.ToString())) {
            return explicitNameValue.ToString();
        }

        var name = TryGetNameAttributeValue();
        if (!string.IsNullOrWhiteSpace(name)) {
            return name;
        }

        return BuildElementNameFromExpression(ValueExpression?.Body) ?? FieldIdentifier.FieldName;
    }

    private string ResolveGeneratedInputId() {
        var source = !string.IsNullOrWhiteSpace(_resolvedElementName) ? _resolvedElementName : FieldIdentifier.FieldName;
        var prefix = InputIdPrefix;
        if (!_generatedInputIdResolved || !string.Equals(source, _generatedInputIdSource, StringComparison.Ordinal) || !string.Equals(prefix, _generatedInputIdPrefix, StringComparison.Ordinal)) {
            _generatedInputIdResolved = true;
            _generatedInputIdSource = source;
            _generatedInputIdPrefix = prefix;
            _generatedInputId = string.IsNullOrWhiteSpace(source) ? $"{prefix}-field" : $"{prefix}-{CreateIdSegment(source)}-{ComputeStableHash(source)}";
        }

        return _generatedInputId;
    }

    private string? BuildElementNameFromExpression(Expression? expression) {
        expression = UnwrapConvert(expression);
        if (expression is null) {
            return null;
        }

        return expression switch {
            BinaryExpression { NodeType: ExpressionType.ArrayIndex } arrayIndex => CombineIndexedPath(BuildElementNameFromExpression(arrayIndex.Left), FormatIndexArgument(arrayIndex.Right)),
            ConstantExpression => null,
            IndexExpression indexExpression => CombineIndexedPath(BuildElementNameFromExpression(indexExpression.Object), FormatIndexArguments(indexExpression.Arguments)),
            MemberExpression memberExpression => BuildMemberPath(memberExpression),
            MethodCallExpression methodCallExpression when methodCallExpression.Method.Name == "get_Item" =>
                CombineIndexedPath(BuildElementNameFromExpression(methodCallExpression.Object), FormatIndexArguments(methodCallExpression.Arguments)),
            ParameterExpression parameterExpression => parameterExpression.Name,
            _ => null
        };
    }

    private string? BuildMemberPath(MemberExpression memberExpression) {
        var parentExpression = UnwrapConvert(memberExpression.Expression);
        if (parentExpression is null or ConstantExpression) {
            return null;
        }

        if (parentExpression is MemberExpression parentMember && UnwrapConvert(parentMember.Expression) is ConstantExpression) {
            return memberExpression.Member.Name;
        }

        var parentPath = BuildElementNameFromExpression(parentExpression);
        return string.IsNullOrWhiteSpace(parentPath)
            ? memberExpression.Member.Name
            : $"{parentPath}.{memberExpression.Member.Name}";
    }

    private static string? CombineIndexedPath(string? path, string? indexArgument) {
        if (string.IsNullOrWhiteSpace(path)) {
            return null;
        }

        return string.IsNullOrWhiteSpace(indexArgument)
            ? path
            : $"{path}[{indexArgument}]";
    }

    private string? FormatIndexArgument(Expression expression) {
        var unwrappedExpression = UnwrapConvert(expression);
        if (unwrappedExpression is null) {
            return null;
        }

        if (TryEvaluateExpression(unwrappedExpression, out var value)) {
            return value switch {
                null => null,
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        return BuildElementNameFromExpression(unwrappedExpression);
    }

    private string? FormatIndexArguments(IEnumerable<Expression> arguments) {
        var formattedArguments = arguments
            .Select(FormatIndexArgument)
            .Where(argument => !string.IsNullOrWhiteSpace(argument))
            .ToArray();

        return formattedArguments.Length == 0 ? null : string.Join(",", formattedArguments);
    }

    private string? TryGetNameAttributeValue() {
        try {
            return NameAttributeValue;
        }
        catch (InvalidOperationException) {
            return null;
        }
    }

    private static bool TryEvaluateExpression(Expression expression, out object? value) {
        try {
            value = Evaluate(expression);
            return true;
        }
        catch {
            value = null;
            return false;
        }

        // Index arguments are nearly always constants or captured locals; read those directly instead of compiling a lambda.
        static object? Evaluate(Expression expression) => expression switch {
            ConstantExpression constant => constant.Value,
            MemberExpression { Member: FieldInfo field } member => field.GetValue(member.Expression is null ? null : Evaluate(member.Expression)),
            _ => Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile().Invoke()
        };
    }

    private static Expression? UnwrapConvert(Expression? expression) {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unaryExpression) {
            expression = unaryExpression.Operand;
        }

        return expression;
    }

    private static string CreateIdSegment(string value) {
        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value) {
            if (char.IsLetterOrDigit(character)) {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (previousWasSeparator) {
                continue;
            }

            builder.Append('-');
            previousWasSeparator = true;
        }

        var segment = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(segment) ? "field" : segment;
    }

    private static string ComputeStableHash(string value) {
        const uint FnvOffsetBasis = 2166136261;
        const uint FnvPrime = 16777619;

        var hash = FnvOffsetBasis;
        foreach (var character in value) {
            hash ^= character;
            hash *= FnvPrime;
        }

        return hash.ToString("x8", CultureInfo.InvariantCulture);
    }
}

/// <summary>
///     Base class for Form controls that participate in an <see cref="NTForm" /> and support leading or trailing icons.
/// </summary>
/// <typeparam name="TValue">The bound value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders useful static HTML and adds Blazor behavior when interactive.",
    CompatibilityDetails = "Static SSR preserves the rendered markup and native browser behavior. EventCallback handlers, bound state updates, and live validation require an interactive render mode.")]
public abstract class NTFormControlBase<TValue> : NTFormControlBaseCore<TValue> {
    /// <summary>
    ///     Gets or sets the icon shown before the control content.
    /// </summary>
    [Parameter]
    public NTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Gets or sets the icon shown after the control content.
    /// </summary>
    [Parameter]
    public NTIcon? TrailingIcon { get; set; }
}

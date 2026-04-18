using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace NTComponents.Site.Documentation;

/// <summary>
/// Describes one component parameter that can be edited in the documentation playground.
/// </summary>
public sealed record ComponentParameterDocumentation(
    string Name,
    Type ParameterType,
    string TypeName,
    string Summary,
    string DeclaringTypeName,
    bool IsRequired,
    bool IsInherited,
    ComponentParameterEditorKind EditorKind,
    IReadOnlyList<string> Values);

/// <summary>
/// Describes the editor to use for a component parameter.
/// </summary>
public enum ComponentParameterEditorKind {
    Text,
    MultilineText,
    Boolean,
    Number,
    Select,
    Color,
    Icon,
    RenderFragment,
    EventCallback,
    Expression,
}

/// <summary>
/// Reflects Blazor component parameters from the NTComponents assembly.
/// </summary>
public static class ComponentParameterInspector {
    private static readonly Type _parameterAttributeType = typeof(ParameterAttribute);
    private static readonly Type _editorRequiredAttributeType = typeof(EditorRequiredAttribute);
    private static readonly Type _componentMarkerType = typeof(IComponent);
    private static readonly Type _componentAssemblyMarkerType = typeof(TnTButton);

    /// <summary>
    /// Gets editable parameter documentation for a component entry.
    /// </summary>
    /// <param name="component">The component documentation entry.</param>
    /// <returns>Editable parameter metadata.</returns>
    public static IReadOnlyList<ComponentParameterDocumentation> GetParameters(ComponentDocumentationEntry component) {
        var type = ResolveComponentType(component.TypeName);
        if (type is null) {
            return [];
        }

        var generatedSummaries = component.ApiType?.Properties
            .GroupBy(static property => property.Name, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First().Summary, StringComparer.Ordinal)
            ?? [];

        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(static property => property.GetCustomAttribute(_parameterAttributeType, true) is not null)
            .OrderBy(static property => property.DeclaringType == property.ReflectedType ? 0 : 1)
            .ThenBy(static property => property.Name, StringComparer.Ordinal)
            .Select(property => CreateParameter(component, type, property, generatedSummaries))
            .ToArray();
    }

    private static ComponentParameterDocumentation CreateParameter(
        ComponentDocumentationEntry component,
        Type componentType,
        PropertyInfo property,
        IReadOnlyDictionary<string, string> generatedSummaries) {
        var propertyType = property.PropertyType;
        var editorKind = GetEditorKind(propertyType);
        var summary = generatedSummaries.TryGetValue(property.Name, out var generatedSummary) ? generatedSummary : string.Empty;
        var declaringType = property.DeclaringType is null ? string.Empty : GetSimpleTypeName(property.DeclaringType);

        return new ComponentParameterDocumentation(
            property.Name,
            propertyType,
            FormatTypeName(propertyType),
            summary,
            declaringType,
            property.GetCustomAttribute(_editorRequiredAttributeType, true) is not null,
            property.DeclaringType != componentType,
            editorKind,
            GetKnownValues(propertyType, editorKind));
    }

    public static Type? ResolveComponentType(string typeName) =>
        _componentAssemblyMarkerType.Assembly
            .GetTypes()
            .Where(type => !type.IsNested && _componentMarkerType.IsAssignableFrom(type))
            .FirstOrDefault(type => string.Equals(GetSimpleTypeName(type), typeName, StringComparison.Ordinal));

    private static ComponentParameterEditorKind GetEditorKind(Type type) {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(bool)) {
            return ComponentParameterEditorKind.Boolean;
        }

        if (underlyingType.IsEnum) {
            return string.Equals(underlyingType.Name, nameof(TnTColor), StringComparison.Ordinal)
                ? ComponentParameterEditorKind.Color
                : ComponentParameterEditorKind.Select;
        }

        if (underlyingType == typeof(string)) {
            return ComponentParameterEditorKind.Text;
        }

        if (underlyingType == typeof(int) ||
            underlyingType == typeof(long) ||
            underlyingType == typeof(short) ||
            underlyingType == typeof(float) ||
            underlyingType == typeof(double) ||
            underlyingType == typeof(decimal)) {
            return ComponentParameterEditorKind.Number;
        }

        if (typeof(RenderFragment).IsAssignableFrom(underlyingType) ||
            underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(RenderFragment<>)) {
            return ComponentParameterEditorKind.RenderFragment;
        }

        if (underlyingType == typeof(EventCallback) ||
            underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(EventCallback<>)) {
            return ComponentParameterEditorKind.EventCallback;
        }

        if (typeof(TnTIcon).IsAssignableFrom(underlyingType)) {
            return ComponentParameterEditorKind.Icon;
        }

        if (typeof(Delegate).IsAssignableFrom(underlyingType) ||
            underlyingType.FullName?.StartsWith("System.Func`", StringComparison.Ordinal) == true ||
            underlyingType.FullName?.StartsWith("System.Action`", StringComparison.Ordinal) == true ||
            underlyingType.Namespace == "System.Linq.Expressions") {
            return ComponentParameterEditorKind.Expression;
        }

        return ComponentParameterEditorKind.Expression;
    }

    private static IReadOnlyList<string> GetKnownValues(Type type, ComponentParameterEditorKind editorKind) {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if ((editorKind == ComponentParameterEditorKind.Select || editorKind == ComponentParameterEditorKind.Color) && underlyingType.IsEnum) {
            return Enum.GetNames(underlyingType);
        }

        return [];
    }

    private static string FormatTypeName(Type type) {
        if (type.IsGenericType) {
            var genericName = type.Name;
            var tickIndex = genericName.IndexOf('`', StringComparison.Ordinal);
            var simpleName = tickIndex >= 0 ? genericName[..tickIndex] : genericName;
            return $"{simpleName}<{string.Join(", ", type.GetGenericArguments().Select(FormatTypeName))}>";
        }

        if (type.IsArray) {
            return $"{FormatTypeName(type.GetElementType()!)}[]";
        }

        return GetSimpleTypeName(type);
    }

    private static string GetSimpleTypeName(Type type) {
        var typeName = type.Name;
        var tickIndex = typeName.IndexOf('`', StringComparison.Ordinal);
        return tickIndex >= 0 ? typeName[..tickIndex] : typeName;
    }
}

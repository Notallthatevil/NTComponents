using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using NTComponents.GeneratedDocumentation;
using NTComponents.Scheduler;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NTComponents.Site.Documentation;

public sealed partial class DocumentationCatalog {
    private static readonly Assembly ComponentsAssembly = typeof(NTButton).Assembly;
    private static readonly IReadOnlyDictionary<string, string> DependentComponentParentsByName = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["NTAccordionItem"] = "NTAccordion",
        ["NTAutocompleteOption"] = "NTAutocomplete",
        ["NTAutocompleteOptionGroup"] = "NTAutocomplete",
        ["NTButtonGroupItem"] = "NTButtonGroup",
        ["NTBody"] = "NTLayout",
        ["NTCarouselItem"] = "NTCarousel",
        ["NTFabMenuAnchorItem"] = "NTFabMenu",
        ["NTFabMenuButtonItem"] = "NTFabMenu",
        ["NTFileUploadItem"] = "NTFileUpload",
        ["NTFooter"] = "NTLayout",
        ["NTHeader"] = "NTLayout",
        ["NTInputRadio"] = "NTInputRadioGroup",
        ["NTInputSelectOption"] = "NTInputSelect",
        ["NTMenuAnchorItem"] = "NTMenu",
        ["NTMenuButtonItem"] = "NTMenu",
        ["NTMenuDividerItem"] = "NTMenu",
        ["NTMenuLabelItem"] = "NTMenu",
        ["NTMenuSubMenuItem"] = "NTMenu",
        ["NTNavigationRailGroup"] = "NTNavigationRail",
        ["NTNavigationRailItem"] = "NTNavigationRail",
        ["NTNavigationRailSectionHeader"] = "NTNavigationRail",
        ["NTPageScript"] = "NTHeadDependencies",
        ["NTPropertyColumn"] = "NTDataGrid",
        ["NTTab"] = "NTTabView",
        ["NTTemplateColumn"] = "NTDataGrid",
        ["NTWizardFormStep"] = "NTWizard",
        ["NTWizardStep"] = "NTWizard"
    };

    private static readonly IReadOnlyList<object> MaterialIconSandboxOptions = [
        new SandboxIconOption("None", null),
        new SandboxIconOption("MaterialIcon.Add", MaterialIcon.Add),
        new SandboxIconOption("MaterialIcon.ArrowDropDown", MaterialIcon.ArrowDropDown),
        new SandboxIconOption("MaterialIcon.CalendarToday", MaterialIcon.CalendarToday),
        new SandboxIconOption("MaterialIcon.Check", MaterialIcon.Check),
        new SandboxIconOption("MaterialIcon.Close", MaterialIcon.Close),
        new SandboxIconOption("MaterialIcon.Delete", MaterialIcon.Delete),
        new SandboxIconOption("MaterialIcon.Edit", MaterialIcon.Edit),
        new SandboxIconOption("MaterialIcon.Favorite", MaterialIcon.Favorite),
        new SandboxIconOption("MaterialIcon.Home", MaterialIcon.Home),
        new SandboxIconOption("MaterialIcon.Info", MaterialIcon.Info),
        new SandboxIconOption("MaterialIcon.Menu", MaterialIcon.Menu),
        new SandboxIconOption("MaterialIcon.Person", MaterialIcon.Person),
        new SandboxIconOption("MaterialIcon.Search", MaterialIcon.Search),
        new SandboxIconOption("MaterialIcon.Settings", MaterialIcon.Settings),
        new SandboxIconOption("MaterialIcon.Warning", MaterialIcon.Warning)
    ];

    private readonly IReadOnlyList<ComponentDocumentationEntry> _components;
    private readonly IReadOnlyDictionary<string, ComponentDocumentationEntry> _componentRoutes;
    private readonly IReadOnlyList<ReferenceDocumentationEntry> _references;

    public DocumentationCatalog() {
        var componentEntries = new List<ComponentDocumentationEntry>();
        var referencedTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in GeneratedCodeDocumentation.Model.Types) {
            var runtimeType = ResolveRuntimeType(type);
            if (runtimeType is null || !IsDocumentedNtComponent(type, runtimeType)) {
                continue;
            }

            var component = new ComponentDocumentationEntry(
                type,
                runtimeType,
                CreateSlug(type.Name),
                ResolveGroupName(type),
                BuildComponentDisplayName(type.Name),
                BuildMemberGroups(type),
                BuildSandbox(type, runtimeType));

            componentEntries.Add(component);
            foreach (var parameter in component.Parameters) {
                AddReferencedTypeNames(referencedTypeNames, parameter.TypeFullName);
            }
        }

        _components = [.. BuildComponentPages(componentEntries)
            .OrderBy(component => component.GroupName, StringComparer.Ordinal)
            .ThenBy(component => component.DisplayName, StringComparer.Ordinal)];

        _componentRoutes = BuildComponentRoutes(_components);

        _references = [.. GeneratedCodeDocumentation.Model.Types
            .Where(type => IsReferenceType(type, referencedTypeNames))
            .Select(type => new ReferenceDocumentationEntry(type, CreateSlug(type.FullName), BuildReferenceGroupName(type), ResolveGroupName(type)))
            .OrderBy(reference => reference.GroupName, StringComparer.Ordinal)
            .ThenBy(reference => reference.FamilyName, StringComparer.Ordinal)
            .ThenBy(reference => reference.Type.Name, StringComparer.Ordinal)];
    }

    public IReadOnlyList<ComponentDocumentationEntry> Components => _components;

    public int DocumentedComponentTypeCount => _components.Sum(component => 1 + component.DependentComponents.Count);

    public IReadOnlyList<ReferenceDocumentationEntry> References => _references;

    public IEnumerable<IGrouping<string, ComponentDocumentationEntry>> ComponentGroups =>
        _components.GroupBy(component => component.GroupName).OrderBy(group => group.Key, StringComparer.Ordinal);

    public IEnumerable<IGrouping<string, ReferenceDocumentationEntry>> ReferenceGroups =>
        _references.GroupBy(reference => reference.GroupName).OrderBy(group => group.Key, StringComparer.Ordinal);

    public IEnumerable<IGrouping<string, ReferenceDocumentationEntry>> EnumGroups =>
        GetReferenceGroups("Enums");

    public IEnumerable<IGrouping<string, ReferenceDocumentationEntry>> ConstantGroups =>
        GetReferenceGroups("Constants");

    public ComponentDocumentationEntry? GetComponent(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : _componentRoutes.GetValueOrDefault(slug);

    public ReferenceDocumentationEntry? GetReference(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : _references.FirstOrDefault(reference => string.Equals(reference.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IGrouping<string, ReferenceDocumentationEntry>> GetReferenceGroups(string groupName) =>
        _references
            .Where(reference => string.Equals(reference.GroupName, groupName, StringComparison.Ordinal))
            .GroupBy(reference => reference.FamilyName)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

    public string? GetTypeUrl(string? typeFullName) {
        if (string.IsNullOrWhiteSpace(typeFullName)) {
            return null;
        }

        var normalizedTypeFullName = NormalizeDocumentationReference(typeFullName);
        var componentMatch = FindComponentMatch(normalizedTypeFullName);
        if (componentMatch is not null) {
            var anchor = componentMatch.IsDependent ? $"#{componentMatch.Component.Slug}" : string.Empty;
            return $"/components/{componentMatch.Page.Slug}{anchor}";
        }

        var reference = _references.FirstOrDefault(reference => string.Equals(reference.Type.FullName, normalizedTypeFullName, StringComparison.Ordinal));
        if (reference is not null) {
            return $"/reference/{reference.RouteKind}/{reference.Slug}";
        }

        return null;
    }

    public DocumentationLink? GetDocumentationLink(string? cref) {
        if (string.IsNullOrWhiteSpace(cref)) {
            return null;
        }

        var normalizedReference = NormalizeDocumentationReference(cref);
        var directTypeUrl = GetTypeUrl(normalizedReference);
        if (directTypeUrl is not null) {
            return new DocumentationLink(directTypeUrl, ShortTypeName(normalizedReference));
        }

        var componentMatch = FindComponentMemberMatch(normalizedReference);
        if (componentMatch is not null) {
            var memberName = normalizedReference[(RemoveGenericArity(componentMatch.Component.Type.FullName).Length + 1)..];
            var anchor = componentMatch.IsDependent ? $"{componentMatch.Component.Slug}-{CreateSlug(memberName)}" : CreateSlug(memberName);
            return new DocumentationLink($"/components/{componentMatch.Page.Slug}#{anchor}", memberName);
        }

        var reference = _references
            .Where(reference => normalizedReference.StartsWith(reference.Type.FullName + ".", StringComparison.Ordinal))
            .OrderByDescending(reference => reference.Type.FullName.Length)
            .FirstOrDefault();
        if (reference is not null) {
            var memberName = normalizedReference[(reference.Type.FullName.Length + 1)..];
            return new DocumentationLink($"/reference/{reference.RouteKind}/{reference.Slug}#{CreateSlug(memberName)}", memberName);
        }

        return null;
    }

    public IEnumerable<ComponentDocumentationEntry> SearchComponents(string? query) {
        if (string.IsNullOrWhiteSpace(query)) {
            return _components;
        }

        var terms = query.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return _components.Where(component => terms.All(term => ComponentMatches(component, term) || component.DependentComponents.Any(child => ComponentMatches(child, term))));
    }

    private static IReadOnlyList<ComponentDocumentationEntry> BuildComponentPages(IReadOnlyList<ComponentDocumentationEntry> componentEntries) {
        var componentsByName = componentEntries.ToDictionary(component => component.Type.Name, StringComparer.Ordinal);
        var childrenByParentName = new Dictionary<string, List<ComponentDocumentationEntry>>(StringComparer.Ordinal);
        var dependentNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var component in componentEntries) {
            if (!DependentComponentParentsByName.TryGetValue(component.Type.Name, out var parentName) || !componentsByName.ContainsKey(parentName)) {
                continue;
            }

            dependentNames.Add(component.Type.Name);
            if (!childrenByParentName.TryGetValue(parentName, out var children)) {
                children = [];
                childrenByParentName[parentName] = children;
            }

            children.Add(component);
        }

        return [.. componentEntries
            .Where(component => !dependentNames.Contains(component.Type.Name))
            .Select(component => childrenByParentName.TryGetValue(component.Type.Name, out var children)
                ? component with {
                    DependentComponents = [.. children
                        .OrderBy(child => child.DisplayName, StringComparer.Ordinal)]
                }
                : component)];
    }

    private static IReadOnlyDictionary<string, ComponentDocumentationEntry> BuildComponentRoutes(IReadOnlyList<ComponentDocumentationEntry> componentPages) {
        var routes = new Dictionary<string, ComponentDocumentationEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in componentPages) {
            routes[page.Slug] = page;
            foreach (var child in page.DependentComponents) {
                routes[child.Slug] = page;
            }
        }

        return routes;
    }

    private ComponentDocumentationMatch? FindComponentMatch(string normalizedTypeFullName) {
        foreach (var page in _components) {
            if (IsTypeMatch(page.Type.FullName, normalizedTypeFullName)) {
                return new ComponentDocumentationMatch(page, page, false);
            }

            var child = page.DependentComponents.FirstOrDefault(child => IsTypeMatch(child.Type.FullName, normalizedTypeFullName));
            if (child is not null) {
                return new ComponentDocumentationMatch(page, child, true);
            }
        }

        return null;
    }

    private ComponentDocumentationMatch? FindComponentMemberMatch(string normalizedReference) =>
        _components
            .SelectMany(page => new[] { new ComponentDocumentationMatch(page, page, false) }
                .Concat(page.DependentComponents.Select(child => new ComponentDocumentationMatch(page, child, true))))
            .Where(match => normalizedReference.StartsWith(RemoveGenericArity(match.Component.Type.FullName) + ".", StringComparison.Ordinal))
            .OrderByDescending(match => RemoveGenericArity(match.Component.Type.FullName).Length)
            .FirstOrDefault();

    private static bool ComponentMatches(ComponentDocumentationEntry component, string term) =>
        Contains(component.DisplayName, term) ||
        Contains(component.Type.FullName, term) ||
        Contains(component.Type.Summary, term) ||
        Contains(component.Type.Remarks, term) ||
        component.Parameters.Any(parameter => Contains(parameter.Name, term) || Contains(parameter.Summary, term));

    private static bool Contains(string? value, string term) => value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;

    private static Type? ResolveRuntimeType(TypeDocumentation documentation) {
        var fullName = RemoveGenericArity(documentation.FullName);
        var genericArity = CountGenericArguments(documentation.FullName);
        var runtimeName = genericArity == 0 ? fullName : $"{fullName}`{genericArity}";
        return ComponentsAssembly.GetType(runtimeName, throwOnError: false, ignoreCase: false);
    }

    private static bool IsDocumentedNtComponent(TypeDocumentation documentation, Type runtimeType) =>
        documentation.Kind == "Class" &&
        documentation.Accessibility == "Public" &&
        documentation.Name.StartsWith("NT", StringComparison.Ordinal) &&
        !documentation.Name.StartsWith("TnT", StringComparison.Ordinal) &&
        !runtimeType.IsAbstract &&
        typeof(IComponent).IsAssignableFrom(runtimeType);

    private static bool IsReferenceType(TypeDocumentation type, HashSet<string> referencedTypeNames) {
        if (type.Name.StartsWith("TnT", StringComparison.Ordinal) && !referencedTypeNames.Contains(type.FullName)) {
            return false;
        }

        return type.Kind == "Enum" || HasPublicConstantFields(type) || referencedTypeNames.Contains(type.FullName);
    }

    private static bool HasPublicConstantFields(TypeDocumentation type) =>
        type.Fields.Any(field => IsPublicOrProtected(field.Accessibility) && !string.IsNullOrWhiteSpace(field.ConstantValue));

    private static void AddReferencedTypeNames(HashSet<string> referencedTypeNames, string typeFullName) {
        if (string.IsNullOrWhiteSpace(typeFullName)) {
            return;
        }

        var typeNames = GenericTypeNameRegex().Matches(typeFullName)
            .Select(match => match.Value.TrimEnd('?', '[', ']'))
            .Where(value => value.StartsWith("NTComponents.", StringComparison.Ordinal));

        foreach (var typeName in typeNames) {
            referencedTypeNames.Add(RemoveGenericArity(typeName));
        }
    }

    private static ComponentMemberGroups BuildMemberGroups(TypeDocumentation type) {
        var directProperties = SortProperties(type.Properties.Where(member => !member.IsFromBaseType && IsPublicOrProtected(member.Accessibility)));
        var inheritedProperties = SortProperties(type.Properties.Where(member => member.IsFromBaseType && IsPublicOrProtected(member.Accessibility)));
        var directFields = SortFields(type.Fields.Where(member => !member.IsFromBaseType && IsPublicOrProtected(member.Accessibility)));
        var inheritedFields = SortFields(type.Fields.Where(member => member.IsFromBaseType && IsPublicOrProtected(member.Accessibility)));

        return new ComponentMemberGroups(directProperties, inheritedProperties, directFields, inheritedFields);
    }

    private static SandboxDocumentation BuildSandbox(TypeDocumentation documentation, Type runtimeType) {
        var componentType = CloseGenericComponentType(runtimeType);
        if (componentType is null) {
            return SandboxDocumentation.Unsupported("Open generic component requires sample type data that cannot be inferred safely.");
        }

        var defaultComponent = componentType.GetConstructor(Type.EmptyTypes) is null ? null : Activator.CreateInstance(componentType);
        var parameters = documentation.Parameters
            .Where(parameter => !parameter.IsFromBaseType && IsPublicOrProtected(parameter.Accessibility))
            .OrderBy(parameter => parameter.Name, StringComparer.Ordinal)
            .Select(parameter => CreateSandboxParameter(documentation.Name, componentType, defaultComponent, parameter))
            .ToArray();

        var unsupportedRequiredParameters = parameters
            .Where(parameter => parameter.Property.IsEditorRequired && !parameter.IsSupported)
            .Select(parameter => parameter.Property.Name)
            .ToArray();
        if (unsupportedRequiredParameters.Length > 0) {
            return SandboxDocumentation.Unsupported($"Automatic sandbox cannot render this component because required parameters are not auto-controlled: {string.Join(", ", unsupportedRequiredParameters)}.");
        }

        return new SandboxDocumentation(componentType, parameters);
    }

    private static SandboxParameterDocumentation CreateSandboxParameter(string componentName, Type componentType, object? defaultComponent, PropertyDocumentation parameter) {
        var property = componentType.GetProperty(parameter.Name);
        if (property is null) {
            return SandboxParameterDocumentation.Unsupported(parameter, "Runtime parameter type could not be resolved.");
        }

        var propertyType = property.PropertyType;
        var componentDefault = defaultComponent is null ? null : property.GetValue(defaultComponent);
        var sample = CreateSampleParameter(componentName, parameter, propertyType);
        if (sample is not null) {
            return sample;
        }

        var valueType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (valueType == typeof(bool)) {
            return SandboxParameterDocumentation.Supported(parameter, SandboxParameterKind.Boolean, componentDefault ?? false);
        }

        if (valueType == typeof(string)) {
            var sampleText = (string)BuildStringDefault(parameter);
            return SandboxParameterDocumentation.Supported(parameter, SandboxParameterKind.Text, string.IsNullOrEmpty(sampleText) ? componentDefault ?? string.Empty : sampleText);
        }

        if (IsNumericType(valueType.FullName ?? valueType.Name)) {
            return SandboxParameterDocumentation.Supported(parameter, SandboxParameterKind.Number, componentDefault ?? Activator.CreateInstance(valueType));
        }

        if (valueType == typeof(DateTime) || valueType == typeof(DateOnly) || valueType == typeof(TimeOnly) || valueType == typeof(DateTimeOffset)) {
            return SandboxParameterDocumentation.Supported(parameter, SandboxParameterKind.Text, componentDefault ?? BuildTemporalDefault(valueType), valueType);
        }

        if (propertyType == typeof(EventCallback) || propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(EventCallback<>)) {
            return SandboxParameterDocumentation.Unsupported(parameter, "Event callback parameters are documented but not auto-controlled.", isControlVisible: false);
        }

        if (typeof(TnTIcon).IsAssignableFrom(valueType)) {
            var defaultIcon = ResolveDefaultIcon(parameter);
            return SandboxParameterDocumentation.Supported(parameter, SandboxParameterKind.Icon, defaultIcon, options: MaterialIconSandboxOptions);
        }

        if (valueType.IsEnum) {
            var values = GetDistinctEnumValues(valueType);
            var isNullable = Nullable.GetUnderlyingType(propertyType) is not null;
            var options = values
                .Select(value => new SandboxEnumOption(value.ToString() ?? string.Empty, value))
                .OrderBy(option => option.Label, StringComparer.Ordinal)
                .Cast<object>()
                .ToList();
            if (isNullable) {
                options.Insert(0, new SandboxEnumOption("None", null));
            }

            var defaultValue = componentDefault is null
                ? options[0]
                : options.OfType<SandboxEnumOption>().FirstOrDefault(option => Equals(option.Value, componentDefault)) ?? options[0];
            return SandboxParameterDocumentation.Supported(parameter, SandboxParameterKind.Enum, defaultValue, valueType, options, isNullable: isNullable);
        }

        if (propertyType == typeof(RenderFragment)) {
            return SandboxParameterDocumentation.Supported(parameter, SandboxParameterKind.ChildContent, BuildRenderFragment(parameter.Name), isControlVisible: false);
        }

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(RenderFragment<>)) {
            return SandboxParameterDocumentation.Supported(parameter, SandboxParameterKind.ChildContent, null, propertyType, isControlVisible: false);
        }

        return SandboxParameterDocumentation.Unsupported(parameter, "Complex parameter type is documented but not auto-controlled.");
    }

    private static object BuildStringDefault(PropertyDocumentation parameter) =>
        parameter.Name switch {
            "AriaLabel" => "Example action",
            "Label" => "Example",
            "Title" => "Example title",
            "Href" when parameter.IsEditorRequired => "#example",
            "Src" => "./_content/NTComponents/NTComponents.lib.module.js",
            "ElementTitle" => "Example title",
            "SupportingText" => "Supporting text",
            "Placeholder" => "Placeholder",
            _ => string.Empty
        };

    private static RenderFragment BuildRenderFragment(string parameterName) => builder => builder.AddContent(0, "Example");

    private static object BuildTemporalDefault(Type type) =>
        type == typeof(DateTime) ? DateTime.Today :
        type == typeof(DateOnly) ? DateOnly.FromDateTime(DateTime.Today) :
        type == typeof(TimeOnly) ? new TimeOnly(9, 0) :
        new DateTimeOffset(DateTime.Today.AddHours(9));

    private static SandboxParameterDocumentation? CreateSampleParameter(string componentName, PropertyDocumentation parameter, Type propertyType) {
        var markup = (componentName, parameter.Name) switch {
            ("NTCombobox", "Items") => "@SampleOptions",
            ("NTDataGrid", "Items") => "@SampleItems.AsQueryable()",
            ("NTPropertyColumn", "Property") => "@(item => item.Name)",
            ("NTScheduler", "Events") => "@SampleEvents",
            ("NTTypeahead", "ItemsLookupFunc") => "LookupItemsAsync",
            ("NTVirtualize", "ItemsProvider") => "LoadItemsAsync",
            _ => null
        };

        return markup is null
            ? null
            : SandboxParameterDocumentation.Supported(parameter, SandboxParameterKind.Sample, null, propertyType, isControlVisible: false, sampleMarkup: markup);
    }

    private static SandboxIconOption ResolveDefaultIcon(PropertyDocumentation parameter) {
        if (!parameter.IsEditorRequired) {
            return (SandboxIconOption)MaterialIconSandboxOptions[0];
        }

        return parameter.Name.Contains("Trailing", StringComparison.OrdinalIgnoreCase)
            ? FindMaterialIconOption("MaterialIcon.ArrowDropDown")
            : FindMaterialIconOption("MaterialIcon.Home");
    }

    private static SandboxIconOption FindMaterialIconOption(string label) =>
        MaterialIconSandboxOptions.OfType<SandboxIconOption>().First(option => string.Equals(option.Label, label, StringComparison.Ordinal));

    private static object[] GetDistinctEnumValues(Type enumType) =>
        [.. Enum.GetNames(enumType)
            .Select(name => Enum.Parse(enumType, name))
            .DistinctBy(value => GetEnumValueKey(enumType, value))];

    private static string GetEnumValueKey(Type enumType, object value) {
        var underlyingType = Enum.GetUnderlyingType(enumType);
        var underlyingValue = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        return Convert.ToString(underlyingValue, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static Type? CloseGenericComponentType(Type runtimeType) {
        if (!runtimeType.IsGenericTypeDefinition) {
            return runtimeType;
        }

        var sampleArguments = runtimeType.Name switch {
            "NTDataGrid`1" or "NTPropertyColumn`2" or "NTTemplateColumn`1" => runtimeType.GetGenericArguments().Length == 2 ? [typeof(DocsSampleItem), typeof(string)] : [typeof(DocsSampleItem)],
            "NTScheduler`1" => [typeof(DocsSampleEvent)],
            "NTInputNumeric`1" or "NTInputRangeSlider`1" or "NTInputSlider`1" => [typeof(decimal)],
            "NTInputDateTime`1" => [typeof(DateTime)],
            _ => runtimeType.GetGenericArguments().Select(ResolveSampleGenericArgument).ToArray()
        };
        return runtimeType.MakeGenericType(sampleArguments);
    }

    private static Type ResolveSampleGenericArgument(Type parameter) {
        var constraints = parameter.GetGenericParameterConstraints();
        if (constraints.Any(constraint => constraint == typeof(Enum))) {
            return typeof(NTButtonVariant);
        }

        if (constraints.Any(constraint => constraint == typeof(IComparable))) {
            return typeof(string);
        }

        var attributes = parameter.GenericParameterAttributes;
        if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) {
            return typeof(int);
        }

        return typeof(string);
    }

    private static int CountGenericArguments(string fullName) {
        var openIndex = fullName.IndexOf('<');
        if (openIndex < 0) {
            return 0;
        }

        var depth = 0;
        var count = 1;
        for (var index = openIndex + 1; index < fullName.Length; index++) {
            switch (fullName[index]) {
                case '<':
                    depth++;
                    break;
                case '>':
                    if (depth == 0) {
                        return count;
                    }
                    depth--;
                    break;
                case ',' when depth == 0:
                    count++;
                    break;
            }
        }

        return 0;
    }

    private static string BuildReferenceGroupName(TypeDocumentation type) =>
        type.Kind == "Enum" ? "Enums" : "Constants";

    private static string ResolveGroupName(TypeDocumentation type) =>
        string.IsNullOrWhiteSpace(type.SourceFolder) ? "Core" : type.SourceFolder switch {
            "NavRail" => "Navigation",
            "Views" => "Layout",
            _ => type.SourceFolder
        };

    private static string BuildComponentDisplayName(string name) => name.StartsWith("NT", StringComparison.Ordinal) && name.Length > 2 ? name[2..] : name;

    public static string CreateSlug(string value) {
        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;
        foreach (var character in value) {
            if (char.IsLetterOrDigit(character)) {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0) {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    public static IReadOnlyList<PropertyDocumentation> SortProperties(IEnumerable<PropertyDocumentation> members) =>
        [.. members.OrderBy(member => member.Name, StringComparer.Ordinal).ThenBy(member => member.DeclaringTypeFullName, StringComparer.Ordinal)];

    public static IReadOnlyList<FieldDocumentation> SortFields(IEnumerable<FieldDocumentation> members) =>
        [.. members.OrderBy(member => member.Name, StringComparer.Ordinal).ThenBy(member => member.DeclaringTypeFullName, StringComparer.Ordinal)];

    public static bool IsPublicOrProtected(string accessibility) =>
        accessibility is "Public" or "Protected" or "ProtectedOrInternal";

    private static string RemoveGenericArity(string value) {
        var index = value.IndexOf('<');
        return index < 0 ? value : value[..index];
    }

    private static string NormalizeDocumentationReference(string value) {
        value = value.Trim();
        if (value.Length > 2 && value[1] == ':') {
            value = value[2..];
        }

        value = value.TrimEnd('?');
        return RemoveGenericArity(value);
    }

    private static string ShortTypeName(string fullName) {
        var index = fullName.LastIndexOf('.');
        return index < 0 ? fullName : fullName[(index + 1)..];
    }

    private static string NormalizeTypeName(string typeFullName) => typeFullName.TrimEnd('?');

    private static bool IsNullableTypeName(string typeFullName) => typeFullName.EndsWith("?", StringComparison.Ordinal);

    private static bool IsTypeMatch(string candidateFullName, string normalizedTypeFullName) =>
        string.Equals(RemoveGenericArity(candidateFullName), normalizedTypeFullName, StringComparison.Ordinal);

    private static bool IsBoolType(string typeName) => typeName is "bool" or "System.Boolean";

    private static bool IsStringType(string typeName) => typeName is "string" or "System.String";

    private static bool IsDateTimeType(string typeName) => typeName is "System.DateTime" or "System.DateOnly" or "System.TimeOnly";

    private static bool IsRenderFragmentType(string typeName) =>
        typeName is "Microsoft.AspNetCore.Components.RenderFragment" or "RenderFragment";

    private static bool IsGenericRenderFragmentType(string typeName) =>
        typeName.StartsWith("Microsoft.AspNetCore.Components.RenderFragment<", StringComparison.Ordinal) ||
        typeName.StartsWith("RenderFragment<", StringComparison.Ordinal);

    private static bool IsEventCallbackType(string typeName) =>
        typeName is "Microsoft.AspNetCore.Components.EventCallback" or "EventCallback";

    private static bool IsGenericEventCallbackType(string typeName) =>
        typeName.StartsWith("Microsoft.AspNetCore.Components.EventCallback<", StringComparison.Ordinal) ||
        typeName.StartsWith("EventCallback<", StringComparison.Ordinal);

    private static bool IsIconType(string typeName) =>
        typeName is "NTComponents.TnTIcon" or "TnTIcon" or "NTComponents.MaterialIcon" or "MaterialIcon";

    private static bool IsNumericType(string typeName) =>
        typeName is "byte" or "sbyte" or "short" or "ushort" or "int" or "uint" or "long" or "ulong" or "float" or "double" or "decimal" or
            "System.Byte" or "System.SByte" or "System.Int16" or "System.UInt16" or "System.Int32" or "System.UInt32" or "System.Int64" or
            "System.UInt64" or "System.Single" or "System.Double" or "System.Decimal";

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_.]*(?:`[0-9]+)?")]
    private static partial Regex GenericTypeNameRegex();
}

public sealed record ComponentDocumentationEntry(
    TypeDocumentation Type,
    Type RuntimeType,
    string Slug,
    string GroupName,
    string DisplayName,
    ComponentMemberGroups MemberGroups,
    SandboxDocumentation Sandbox,
    IReadOnlyList<ComponentDocumentationEntry>? DependentComponents = null) {
    public IReadOnlyList<ComponentDocumentationEntry> DependentComponents { get; init; } = DependentComponents ?? [];

    public IReadOnlyList<PropertyDocumentation> Parameters => Type.Parameters
        .Where(parameter => DocumentationCatalog.IsPublicOrProtected(parameter.Accessibility))
        .OrderByDescending(parameter => parameter.IsEditorRequired)
        .ThenBy(parameter => parameter.Name, StringComparer.Ordinal)
        .ToArray();

    public int RequiredParameterCount => Parameters.Count(parameter => parameter.IsEditorRequired);

    public int DirectMemberCount => MemberGroups.DirectProperties.Count + MemberGroups.DirectFields.Count;

    public int InheritedMemberCount => MemberGroups.InheritedProperties.Count + MemberGroups.InheritedFields.Count;

    public int SupportedSandboxParameterCount => Sandbox.Parameters.Count(parameter => parameter.IsSupported);

    public int UnsupportedSandboxParameterCount => Sandbox.Parameters.Count(parameter => !parameter.IsSupported);
}

public sealed record ReferenceDocumentationEntry(TypeDocumentation Type, string Slug, string GroupName, string FamilyName) {
    public string RouteKind => GroupName == "Constants" ? "constants" : "enums";

    public int MemberCount => Type.Fields.Count(member => DocumentationCatalog.IsPublicOrProtected(member.Accessibility));
}

public sealed record DocumentationLink(string Href, string Label);

public sealed record SandboxEnumOption(string Label, object? Value);

public sealed record SandboxIconOption(string Label, TnTIcon? Icon);

public sealed record ComponentMemberGroups(
    IReadOnlyList<PropertyDocumentation> DirectProperties,
    IReadOnlyList<PropertyDocumentation> InheritedProperties,
    IReadOnlyList<FieldDocumentation> DirectFields,
    IReadOnlyList<FieldDocumentation> InheritedFields);

public sealed record SandboxDocumentation(Type? ComponentType, IReadOnlyList<SandboxParameterDocumentation> Parameters, string? UnsupportedReason = null) {
    public bool IsSupported => ComponentType is not null;

    public static SandboxDocumentation Unsupported(string reason) => new(null, [], reason);
}

public sealed record SandboxParameterDocumentation(PropertyDocumentation Property, SandboxParameterKind Kind, object? DefaultValue, Type? RuntimeType, IReadOnlyList<object> Options, string? UnsupportedReason, bool IsControlVisible = true, bool IsNullable = false, string? SampleMarkup = null) {
    public bool IsSupported => UnsupportedReason is null;

    public static SandboxParameterDocumentation Supported(PropertyDocumentation property, SandboxParameterKind kind, object? defaultValue, Type? runtimeType = null, IReadOnlyList<object>? options = null, bool isControlVisible = true, bool isNullable = false, string? sampleMarkup = null) =>
        new(property, kind, defaultValue, runtimeType, options ?? [], null, isControlVisible, isNullable, sampleMarkup);

    public static SandboxParameterDocumentation Unsupported(PropertyDocumentation property, string reason, bool isControlVisible = true) => new(property, SandboxParameterKind.Unsupported, null, null, [], reason, isControlVisible);
}

public enum SandboxParameterKind {
    Unsupported,
    Boolean,
    Text,
    Number,
    Enum,
    Icon,
    ChildContent,
    Sample
}

public sealed class DocsSampleItem {
    public string Name { get; set; } = "Sample item";

    public int Value { get; set; } = 1;
}

public sealed record DocsSampleEvent : TnTEvent;

sealed record ComponentDocumentationMatch(ComponentDocumentationEntry Page, ComponentDocumentationEntry Component, bool IsDependent);

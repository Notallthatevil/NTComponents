using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NTComponents.CodeDocumentation.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class CodeDocumentationGenerator : IIncrementalGenerator {

    private const string AttributeNamespace = "NTCodeDocumentation";
    private const string AttributeName = "GenerateCodeDocumentationAttribute";
    private const string FullyQualifiedAttributeName = AttributeNamespace + "." + AttributeName;

    private static readonly SymbolDisplayFormat TypeDisplayFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat MethodDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeOptionalBrackets,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    private static readonly SymbolDisplayFormat PropertyDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    private static readonly SymbolDisplayFormat FieldDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    private static readonly SymbolDisplayFormat MemberTypeDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(static postInitializationContext =>
            postInitializationContext.AddSource("GenerateCodeDocumentationAttribute.g.cs", SourceText.From(AttributeSourceText, Encoding.UTF8)));

        var modelProvider = context.CompilationProvider.Select(static (compilation, cancellationToken) => {
            if (!HasCodeDocumentationAttribute(compilation, cancellationToken)) {
                return null;
            }

            return BuildModel(compilation, cancellationToken);
        });

        context.RegisterSourceOutput(modelProvider, static (sourceProductionContext, model) => {
            if (model is null) {
                return;
            }

            var source = BuildGeneratedSource(model);
            sourceProductionContext.AddSource("GeneratedCodeDocumentation.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static AssemblyDocumentationModel BuildModel(Compilation compilation, CancellationToken cancellationToken) {
        var types = ImmutableArray.CreateBuilder<TypeDocumentationModel>();

        foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace)) {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ShouldIncludeType(type)) {
                continue;
            }

            types.Add(BuildTypeDocumentation(type, cancellationToken));
        }

        var orderedTypes = types
            .ToImmutable()
            .Sort(static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));

        return new AssemblyDocumentationModel(
            compilation.AssemblyName ?? "UnknownAssembly",
            CreateGeneratedNamespace(compilation.AssemblyName),
            orderedTypes);
    }

    private static TypeDocumentationModel BuildTypeDocumentation(INamedTypeSymbol type, CancellationToken cancellationToken) {
        var methods = ImmutableArray.CreateBuilder<MethodDocumentationModel>();
        var properties = ImmutableArray.CreateBuilder<PropertyDocumentationModel>();
        var fields = ImmutableArray.CreateBuilder<FieldDocumentationModel>();

        foreach (var documentedMember in EnumerateMembersIncludingBaseTypes(type)) {
            cancellationToken.ThrowIfCancellationRequested();

            switch (documentedMember.Member) {
                case IMethodSymbol method when ShouldIncludeMethod(method):
                    methods.Add(BuildMethodDocumentation(method, documentedMember.IsFromBaseType, cancellationToken));
                    break;
                case IPropertySymbol property when !property.IsImplicitlyDeclared:
                    properties.Add(BuildPropertyDocumentation(property, documentedMember.IsFromBaseType, cancellationToken));
                    break;
                case IFieldSymbol field when !field.IsImplicitlyDeclared:
                    fields.Add(BuildFieldDocumentation(field, documentedMember.IsFromBaseType, cancellationToken));
                    break;
            }
        }

        return new TypeDocumentationModel(
            type.Name,
            type.ToDisplayString(TypeDisplayFormat),
            type.BaseType?.ToDisplayString(TypeDisplayFormat) ?? string.Empty,
            type.TypeKind.ToString(),
            ExtractSummary(GetXmlDocumentation(type, cancellationToken)),
            GetXmlDocumentation(type, cancellationToken),
            methods.ToImmutable().Sort(static (left, right) => CompareBySignatureAndDeclaringType(left.Signature, left.DeclaringTypeFullName, right.Signature, right.DeclaringTypeFullName)),
            properties.ToImmutable().Sort(static (left, right) => CompareBySignatureAndDeclaringType(left.Signature, left.DeclaringTypeFullName, right.Signature, right.DeclaringTypeFullName)),
            fields.ToImmutable().Sort(static (left, right) => CompareBySignatureAndDeclaringType(left.Signature, left.DeclaringTypeFullName, right.Signature, right.DeclaringTypeFullName)));
    }

    private static MethodDocumentationModel BuildMethodDocumentation(IMethodSymbol method, bool isFromBaseType, CancellationToken cancellationToken) {
        var xmlDocumentation = GetXmlDocumentation(method, cancellationToken);
        return new MethodDocumentationModel(
            method.Name,
            method.ToDisplayString(MethodDisplayFormat),
            ExtractSummary(xmlDocumentation),
            xmlDocumentation,
            method.ContainingType?.ToDisplayString(TypeDisplayFormat) ?? string.Empty,
            isFromBaseType);
    }

    private static PropertyDocumentationModel BuildPropertyDocumentation(IPropertySymbol property, bool isFromBaseType, CancellationToken cancellationToken) {
        var xmlDocumentation = GetXmlDocumentation(property, cancellationToken);
        return new PropertyDocumentationModel(
            property.Name,
            property.ToDisplayString(PropertyDisplayFormat),
            property.Type.ToDisplayString(MemberTypeDisplayFormat),
            property.Type.ToDisplayString(TypeDisplayFormat),
            ExtractSummary(xmlDocumentation),
            xmlDocumentation,
            property.ContainingType?.ToDisplayString(TypeDisplayFormat) ?? string.Empty,
            isFromBaseType);
    }

    private static FieldDocumentationModel BuildFieldDocumentation(IFieldSymbol field, bool isFromBaseType, CancellationToken cancellationToken) {
        var xmlDocumentation = GetXmlDocumentation(field, cancellationToken);
        return new FieldDocumentationModel(
            field.Name,
            field.ToDisplayString(FieldDisplayFormat),
            field.Type.ToDisplayString(MemberTypeDisplayFormat),
            field.Type.ToDisplayString(TypeDisplayFormat),
            ExtractSummary(xmlDocumentation),
            xmlDocumentation,
            field.ContainingType?.ToDisplayString(TypeDisplayFormat) ?? string.Empty,
            isFromBaseType);
    }

    private static int CompareBySignatureAndDeclaringType(string leftSignature, string leftDeclaringType, string rightSignature, string rightDeclaringType) {
        var signatureComparison = string.CompareOrdinal(leftSignature, rightSignature);
        if (signatureComparison != 0) {
            return signatureComparison;
        }

        return string.CompareOrdinal(leftDeclaringType, rightDeclaringType);
    }

    private static IEnumerable<DocumentedMember> EnumerateMembersIncludingBaseTypes(INamedTypeSymbol type) {
        foreach (var member in type.GetMembers()) {
            yield return new DocumentedMember(member, false);
        }

        var baseType = type.BaseType;
        while (baseType is not null) {
            if (ShouldIncludeBaseTypeMembers(baseType)) {
                foreach (var member in baseType.GetMembers()) {
                    yield return new DocumentedMember(member, true);
                }
            }

            baseType = baseType.BaseType;
        }
    }

    private static bool ShouldIncludeBaseTypeMembers(INamedTypeSymbol baseType) =>
        !baseType.DeclaringSyntaxReferences.IsDefaultOrEmpty;

    private static string BuildGeneratedSource(AssemblyDocumentationModel model) {
        var builder = new StringBuilder();

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("#pragma warning disable CS1591");
        builder.AppendLine();
        builder.AppendLine($"namespace {model.GeneratedNamespace};");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Represents documentation for a compiled assembly.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public sealed class CodeDocumentationModel {");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Creates a new instance of <see cref=\"CodeDocumentationModel\" />.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"assemblyName\">The source assembly name.</param>");
        builder.AppendLine("    /// <param name=\"types\">The documented types in the source assembly.</param>");
        builder.AppendLine("    public CodeDocumentationModel(string assemblyName, global::System.Collections.Generic.IReadOnlyList<TypeDocumentation> types) {");
        builder.AppendLine("        AssemblyName = assemblyName;");
        builder.AppendLine("        Types = types;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the source assembly name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string AssemblyName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the documented types in the source assembly.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public global::System.Collections.Generic.IReadOnlyList<TypeDocumentation> Types { get; }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Represents documentation for a single type.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public sealed class TypeDocumentation {");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Creates a new instance of <see cref=\"TypeDocumentation\" />.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"name\">The type name.</param>");
        builder.AppendLine("    /// <param name=\"fullName\">The fully qualified type name.</param>");
        builder.AppendLine("    /// <param name=\"baseTypeFullName\">The fully qualified base type name, when available.</param>");
        builder.AppendLine("    /// <param name=\"kind\">The type kind (class, struct, enum).</param>");
        builder.AppendLine("    /// <param name=\"summary\">The extracted summary text.</param>");
        builder.AppendLine("    /// <param name=\"xmlDocumentation\">The full XML documentation block.</param>");
        builder.AppendLine("    /// <param name=\"methods\">The documented methods.</param>");
        builder.AppendLine("    /// <param name=\"properties\">The documented properties.</param>");
        builder.AppendLine("    /// <param name=\"fields\">The documented fields.</param>");
        builder.AppendLine("    public TypeDocumentation(");
        builder.AppendLine("        string name,");
        builder.AppendLine("        string fullName,");
        builder.AppendLine("        string baseTypeFullName,");
        builder.AppendLine("        string kind,");
        builder.AppendLine("        string summary,");
        builder.AppendLine("        string xmlDocumentation,");
        builder.AppendLine("        global::System.Collections.Generic.IReadOnlyList<MethodDocumentation> methods,");
        builder.AppendLine("        global::System.Collections.Generic.IReadOnlyList<PropertyDocumentation> properties,");
        builder.AppendLine("        global::System.Collections.Generic.IReadOnlyList<FieldDocumentation> fields) {");
        builder.AppendLine("        Name = name;");
        builder.AppendLine("        FullName = fullName;");
        builder.AppendLine("        BaseTypeFullName = baseTypeFullName;");
        builder.AppendLine("        Kind = kind;");
        builder.AppendLine("        Summary = summary;");
        builder.AppendLine("        XmlDocumentation = xmlDocumentation;");
        builder.AppendLine("        Methods = methods;");
        builder.AppendLine("        Properties = properties;");
        builder.AppendLine("        Fields = fields;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the type name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Name { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the fully qualified type name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string FullName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the type kind.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Kind { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the fully qualified base type name, when present.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string BaseTypeFullName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the summary text extracted from XML docs.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Summary { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the full XML documentation block.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string XmlDocumentation { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets documented methods for the type.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public global::System.Collections.Generic.IReadOnlyList<MethodDocumentation> Methods { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets documented properties for the type.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public global::System.Collections.Generic.IReadOnlyList<PropertyDocumentation> Properties { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets documented fields for the type.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public global::System.Collections.Generic.IReadOnlyList<FieldDocumentation> Fields { get; }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Represents documentation for a method.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public sealed class MethodDocumentation {");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Creates a new instance of <see cref=\"MethodDocumentation\" />.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"name\">The method name.</param>");
        builder.AppendLine("    /// <param name=\"signature\">The method signature.</param>");
        builder.AppendLine("    /// <param name=\"summary\">The method summary.</param>");
        builder.AppendLine("    /// <param name=\"xmlDocumentation\">The full XML documentation block.</param>");
        builder.AppendLine("    /// <param name=\"declaringTypeFullName\">The fully qualified type that declares the method.</param>");
        builder.AppendLine("    /// <param name=\"isFromBaseType\">Indicates whether the method was inherited from a base class.</param>");
        builder.AppendLine("    public MethodDocumentation(string name, string signature, string summary, string xmlDocumentation, string declaringTypeFullName, bool isFromBaseType) {");
        builder.AppendLine("        Name = name;");
        builder.AppendLine("        Signature = signature;");
        builder.AppendLine("        Summary = summary;");
        builder.AppendLine("        XmlDocumentation = xmlDocumentation;");
        builder.AppendLine("        DeclaringTypeFullName = declaringTypeFullName;");
        builder.AppendLine("        IsFromBaseType = isFromBaseType;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the method name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Name { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the method signature.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Signature { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the summary text.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Summary { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the full XML documentation block.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string XmlDocumentation { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the fully qualified type that declares this method.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string DeclaringTypeFullName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this method is inherited from a base class.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsFromBaseType { get; }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Represents documentation for a property.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public sealed class PropertyDocumentation {");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Creates a new instance of <see cref=\"PropertyDocumentation\" />.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"name\">The property name.</param>");
        builder.AppendLine("    /// <param name=\"signature\">The property signature.</param>");
        builder.AppendLine("    /// <param name=\"typeDisplayName\">The display name of the property type.</param>");
        builder.AppendLine("    /// <param name=\"typeFullName\">The fully qualified property type.</param>");
        builder.AppendLine("    /// <param name=\"summary\">The property summary.</param>");
        builder.AppendLine("    /// <param name=\"xmlDocumentation\">The full XML documentation block.</param>");
        builder.AppendLine("    /// <param name=\"declaringTypeFullName\">The fully qualified type that declares the property.</param>");
        builder.AppendLine("    /// <param name=\"isFromBaseType\">Indicates whether the property was inherited from a base class.</param>");
        builder.AppendLine("    public PropertyDocumentation(string name, string signature, string typeDisplayName, string typeFullName, string summary, string xmlDocumentation, string declaringTypeFullName, bool isFromBaseType) {");
        builder.AppendLine("        Name = name;");
        builder.AppendLine("        Signature = signature;");
        builder.AppendLine("        TypeDisplayName = typeDisplayName;");
        builder.AppendLine("        TypeFullName = typeFullName;");
        builder.AppendLine("        Summary = summary;");
        builder.AppendLine("        XmlDocumentation = xmlDocumentation;");
        builder.AppendLine("        DeclaringTypeFullName = declaringTypeFullName;");
        builder.AppendLine("        IsFromBaseType = isFromBaseType;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the property name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Name { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the property signature.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Signature { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the property type display name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string TypeDisplayName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the fully qualified property type.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string TypeFullName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the summary text.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Summary { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the full XML documentation block.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string XmlDocumentation { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the fully qualified type that declares this property.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string DeclaringTypeFullName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this property is inherited from a base class.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsFromBaseType { get; }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Represents documentation for a field.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public sealed class FieldDocumentation {");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Creates a new instance of <see cref=\"FieldDocumentation\" />.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"name\">The field name.</param>");
        builder.AppendLine("    /// <param name=\"signature\">The field signature.</param>");
        builder.AppendLine("    /// <param name=\"typeDisplayName\">The display name of the field type.</param>");
        builder.AppendLine("    /// <param name=\"typeFullName\">The fully qualified field type.</param>");
        builder.AppendLine("    /// <param name=\"summary\">The field summary.</param>");
        builder.AppendLine("    /// <param name=\"xmlDocumentation\">The full XML documentation block.</param>");
        builder.AppendLine("    /// <param name=\"declaringTypeFullName\">The fully qualified type that declares the field.</param>");
        builder.AppendLine("    /// <param name=\"isFromBaseType\">Indicates whether the field was inherited from a base class.</param>");
        builder.AppendLine("    public FieldDocumentation(string name, string signature, string typeDisplayName, string typeFullName, string summary, string xmlDocumentation, string declaringTypeFullName, bool isFromBaseType) {");
        builder.AppendLine("        Name = name;");
        builder.AppendLine("        Signature = signature;");
        builder.AppendLine("        TypeDisplayName = typeDisplayName;");
        builder.AppendLine("        TypeFullName = typeFullName;");
        builder.AppendLine("        Summary = summary;");
        builder.AppendLine("        XmlDocumentation = xmlDocumentation;");
        builder.AppendLine("        DeclaringTypeFullName = declaringTypeFullName;");
        builder.AppendLine("        IsFromBaseType = isFromBaseType;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the field name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Name { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the field signature.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Signature { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the field type display name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string TypeDisplayName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the fully qualified field type.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string TypeFullName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the summary text.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Summary { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the full XML documentation block.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string XmlDocumentation { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the fully qualified type that declares this field.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string DeclaringTypeFullName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this field is inherited from a base class.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsFromBaseType { get; }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Provides access to generated code documentation for the assembly.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public static class GeneratedCodeDocumentation {");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the generated documentation model.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public static CodeDocumentationModel Model { get; } =");
        builder.AppendLine($"        new CodeDocumentationModel({ToLiteral(model.AssemblyName)}, CreateTypes());");
        builder.AppendLine();
        builder.AppendLine("    private static TypeDocumentation[] CreateTypes() => new TypeDocumentation[] {");

        for (var index = 0; index < model.Types.Length; ++index) {
            builder.AppendLine($"        {GetTypeFactoryMethodName(index)}(),");
        }

        builder.AppendLine("    };");
        builder.AppendLine();

        for (var index = 0; index < model.Types.Length; ++index) {
            AppendTypeFactoryMethod(builder, model.Types[index], index);
        }

        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("#pragma warning restore CS1591");

        return builder.ToString();
    }

    private static string GetTypeFactoryMethodName(int index) => $"CreateTypeDocumentation{index}";

    private static void AppendTypeFactoryMethod(StringBuilder builder, TypeDocumentationModel typeDocumentation, int index) {
        builder.AppendLine($"    private static TypeDocumentation {GetTypeFactoryMethodName(index)}() =>");
        builder.AppendLine("        new TypeDocumentation(");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.Name)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.FullName)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.BaseTypeFullName)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.Kind)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.Summary)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.XmlDocumentation)},");

        builder.AppendLine("            new MethodDocumentation[] {");
        foreach (var method in typeDocumentation.Methods) {
            builder.AppendLine("                new MethodDocumentation(");
            builder.AppendLine($"                    {ToLiteral(method.Name)},");
            builder.AppendLine($"                    {ToLiteral(method.Signature)},");
            builder.AppendLine($"                    {ToLiteral(method.Summary)},");
            builder.AppendLine($"                    {ToLiteral(method.XmlDocumentation)},");
            builder.AppendLine($"                    {ToLiteral(method.DeclaringTypeFullName)},");
            builder.AppendLine($"                    {ToBooleanLiteral(method.IsFromBaseType)}),");
        }
        builder.AppendLine("            },");

        builder.AppendLine("            new PropertyDocumentation[] {");
        foreach (var property in typeDocumentation.Properties) {
            builder.AppendLine("                new PropertyDocumentation(");
            builder.AppendLine($"                    {ToLiteral(property.Name)},");
            builder.AppendLine($"                    {ToLiteral(property.Signature)},");
            builder.AppendLine($"                    {ToLiteral(property.TypeDisplayName)},");
            builder.AppendLine($"                    {ToLiteral(property.TypeFullName)},");
            builder.AppendLine($"                    {ToLiteral(property.Summary)},");
            builder.AppendLine($"                    {ToLiteral(property.XmlDocumentation)},");
            builder.AppendLine($"                    {ToLiteral(property.DeclaringTypeFullName)},");
            builder.AppendLine($"                    {ToBooleanLiteral(property.IsFromBaseType)}),");
        }
        builder.AppendLine("            },");

        builder.AppendLine("            new FieldDocumentation[] {");
        foreach (var field in typeDocumentation.Fields) {
            builder.AppendLine("                new FieldDocumentation(");
            builder.AppendLine($"                    {ToLiteral(field.Name)},");
            builder.AppendLine($"                    {ToLiteral(field.Signature)},");
            builder.AppendLine($"                    {ToLiteral(field.TypeDisplayName)},");
            builder.AppendLine($"                    {ToLiteral(field.TypeFullName)},");
            builder.AppendLine($"                    {ToLiteral(field.Summary)},");
            builder.AppendLine($"                    {ToLiteral(field.XmlDocumentation)},");
            builder.AppendLine($"                    {ToLiteral(field.DeclaringTypeFullName)},");
            builder.AppendLine($"                    {ToBooleanLiteral(field.IsFromBaseType)}),");
        }
        builder.AppendLine("            });");
        builder.AppendLine();
    }

    private static string ToLiteral(string value) {
        if (string.IsNullOrEmpty(value)) {
            return "string.Empty";
        }

        return "@\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string ToBooleanLiteral(bool value) => value ? "true" : "false";

    private static bool HasCodeDocumentationAttribute(Compilation compilation, CancellationToken cancellationToken) {
        foreach (var attribute in compilation.Assembly.GetAttributes()) {
            cancellationToken.ThrowIfCancellationRequested();
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(attributeName, FullyQualifiedAttributeName, StringComparison.Ordinal)) {
                return true;
            }
        }

        foreach (var syntaxTree in compilation.SyntaxTrees) {
            cancellationToken.ThrowIfCancellationRequested();
            var root = syntaxTree.GetRoot(cancellationToken);

            foreach (var attributeList in root.DescendantNodes().OfType<AttributeListSyntax>()) {
                if (attributeList.Target is null || !attributeList.Target.Identifier.IsKind(SyntaxKind.AssemblyKeyword)) {
                    continue;
                }

                foreach (var attribute in attributeList.Attributes) {
                    var attributeName = attribute.Name.ToString();
                    if (IsMatchingAttributeName(attributeName)) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsMatchingAttributeName(string attributeName) {
        if (attributeName.StartsWith("global::", StringComparison.Ordinal)) {
            attributeName = attributeName.Substring("global::".Length);
        }

        return string.Equals(attributeName, FullyQualifiedAttributeName, StringComparison.Ordinal) ||
               string.Equals(attributeName, AttributeName, StringComparison.Ordinal) ||
               string.Equals(attributeName, "GenerateCodeDocumentation", StringComparison.Ordinal) ||
               string.Equals(attributeName, AttributeNamespace + ".GenerateCodeDocumentation", StringComparison.Ordinal);
    }

    private static string CreateGeneratedNamespace(string? assemblyName) {
        var candidate = string.IsNullOrWhiteSpace(assemblyName) ? "GeneratedAssembly" : assemblyName!;
        var parts = candidate.Split('.');
        var sanitizedParts = new List<string>(parts.Length + 1);

        foreach (var part in parts) {
            sanitizedParts.Add(SanitizeIdentifier(part));
        }

        sanitizedParts.Add("GeneratedDocumentation");
        return string.Join(".", sanitizedParts);
    }

    private static string SanitizeIdentifier(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "_";
        }

        var builder = new StringBuilder();
        var firstCharacter = value[0];
        if (!SyntaxFacts.IsIdentifierStartCharacter(firstCharacter)) {
            builder.Append('_');
        }

        foreach (var character in value) {
            builder.Append(SyntaxFacts.IsIdentifierPartCharacter(character) ? character : '_');
        }

        return builder.ToString();
    }

    private static bool ShouldIncludeType(INamedTypeSymbol type) {
        if (type.IsImplicitlyDeclared ||
            type.DeclaringSyntaxReferences.IsDefaultOrEmpty ||
            (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct && type.TypeKind != TypeKind.Enum)) {
            return false;
        }

        var containingNamespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (containingNamespace.EndsWith(".GeneratedDocumentation", StringComparison.Ordinal) ||
            string.Equals(containingNamespace, AttributeNamespace, StringComparison.Ordinal)) {
            return false;
        }

        return true;
    }

    private static bool ShouldIncludeMethod(IMethodSymbol method) {
        if (method.IsImplicitlyDeclared) {
            return false;
        }

        return method.MethodKind != MethodKind.PropertyGet &&
               method.MethodKind != MethodKind.PropertySet &&
               method.MethodKind != MethodKind.EventAdd &&
               method.MethodKind != MethodKind.EventRemove &&
               method.MethodKind != MethodKind.EventRaise;
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol namespaceSymbol) {
        foreach (var type in namespaceSymbol.GetTypeMembers()) {
            foreach (var nestedType in EnumerateNestedTypes(type)) {
                yield return nestedType;
            }
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers()) {
            foreach (var type in EnumerateTypes(childNamespace)) {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNestedTypes(INamedTypeSymbol type) {
        yield return type;

        foreach (var nestedType in type.GetTypeMembers()) {
            foreach (var childNestedType in EnumerateNestedTypes(nestedType)) {
                yield return childNestedType;
            }
        }
    }

    private static string GetXmlDocumentation(ISymbol symbol, CancellationToken cancellationToken) =>
        symbol.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: cancellationToken) ?? string.Empty;

    private static string ExtractSummary(string xmlDocumentation) {
        if (string.IsNullOrWhiteSpace(xmlDocumentation)) {
            return string.Empty;
        }

        try {
            var document = XDocument.Parse("<root>" + xmlDocumentation + "</root>");
            var summary = document.Root?.Descendants("summary").FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(summary)) {
                return string.Empty;
            }

            return NormalizeWhitespace(summary!);
        }
        catch {
            return string.Empty;
        }
    }

    private static string NormalizeWhitespace(string value) {
        var builder = new StringBuilder(value.Length);
        var isInWhitespace = false;

        foreach (var character in value) {
            if (char.IsWhiteSpace(character)) {
                if (!isInWhitespace) {
                    builder.Append(' ');
                    isInWhitespace = true;
                }

                continue;
            }

            builder.Append(character);
            isInWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    private const string AttributeSourceText = """
        // <auto-generated />
        #nullable enable
        #pragma warning disable CS1591

        namespace NTCodeDocumentation;

        /// <summary>
        /// Enables generation of a code documentation model for an assembly.
        /// </summary>
        [global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class GenerateCodeDocumentationAttribute : global::System.Attribute {
        }

        #pragma warning restore CS1591
        """;

    private sealed class AssemblyDocumentationModel {
        public AssemblyDocumentationModel(string assemblyName, string generatedNamespace, ImmutableArray<TypeDocumentationModel> types) {
            AssemblyName = assemblyName;
            GeneratedNamespace = generatedNamespace;
            Types = types;
        }

        public string AssemblyName { get; }

        public string GeneratedNamespace { get; }

        public ImmutableArray<TypeDocumentationModel> Types { get; }
    }

    private sealed class TypeDocumentationModel {
        public TypeDocumentationModel(
            string name,
            string fullName,
            string baseTypeFullName,
            string kind,
            string summary,
            string xmlDocumentation,
            ImmutableArray<MethodDocumentationModel> methods,
            ImmutableArray<PropertyDocumentationModel> properties,
            ImmutableArray<FieldDocumentationModel> fields) {
            Name = name;
            FullName = fullName;
            BaseTypeFullName = baseTypeFullName;
            Kind = kind;
            Summary = summary;
            XmlDocumentation = xmlDocumentation;
            Methods = methods;
            Properties = properties;
            Fields = fields;
        }

        public string Name { get; }

        public string FullName { get; }

        public string BaseTypeFullName { get; }

        public string Kind { get; }

        public string Summary { get; }

        public string XmlDocumentation { get; }

        public ImmutableArray<MethodDocumentationModel> Methods { get; }

        public ImmutableArray<PropertyDocumentationModel> Properties { get; }

        public ImmutableArray<FieldDocumentationModel> Fields { get; }
    }

    private sealed class MethodDocumentationModel {
        public MethodDocumentationModel(
            string name,
            string signature,
            string summary,
            string xmlDocumentation,
            string declaringTypeFullName,
            bool isFromBaseType) {
            Name = name;
            Signature = signature;
            Summary = summary;
            XmlDocumentation = xmlDocumentation;
            DeclaringTypeFullName = declaringTypeFullName;
            IsFromBaseType = isFromBaseType;
        }

        public string Name { get; }

        public string Signature { get; }

        public string Summary { get; }

        public string XmlDocumentation { get; }

        public string DeclaringTypeFullName { get; }

        public bool IsFromBaseType { get; }
    }

    private sealed class PropertyDocumentationModel {
        public PropertyDocumentationModel(
            string name,
            string signature,
            string typeDisplayName,
            string typeFullName,
            string summary,
            string xmlDocumentation,
            string declaringTypeFullName,
            bool isFromBaseType) {
            Name = name;
            Signature = signature;
            TypeDisplayName = typeDisplayName;
            TypeFullName = typeFullName;
            Summary = summary;
            XmlDocumentation = xmlDocumentation;
            DeclaringTypeFullName = declaringTypeFullName;
            IsFromBaseType = isFromBaseType;
        }

        public string Name { get; }

        public string Signature { get; }

        public string TypeDisplayName { get; }

        public string TypeFullName { get; }

        public string Summary { get; }

        public string XmlDocumentation { get; }

        public string DeclaringTypeFullName { get; }

        public bool IsFromBaseType { get; }
    }

    private sealed class FieldDocumentationModel {
        public FieldDocumentationModel(
            string name,
            string signature,
            string typeDisplayName,
            string typeFullName,
            string summary,
            string xmlDocumentation,
            string declaringTypeFullName,
            bool isFromBaseType) {
            Name = name;
            Signature = signature;
            TypeDisplayName = typeDisplayName;
            TypeFullName = typeFullName;
            Summary = summary;
            XmlDocumentation = xmlDocumentation;
            DeclaringTypeFullName = declaringTypeFullName;
            IsFromBaseType = isFromBaseType;
        }

        public string Name { get; }

        public string Signature { get; }

        public string TypeDisplayName { get; }

        public string TypeFullName { get; }

        public string Summary { get; }

        public string XmlDocumentation { get; }

        public string DeclaringTypeFullName { get; }

        public bool IsFromBaseType { get; }
    }

    private sealed class DocumentedMember {
        public DocumentedMember(ISymbol member, bool isFromBaseType) {
            Member = member;
            IsFromBaseType = isFromBaseType;
        }

        public ISymbol Member { get; }

        public bool IsFromBaseType { get; }
    }
}

using System.Collections.Immutable;
using System.IO;
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
    private const string DocumentationAttributeFullName = "NTComponents.CodeDocumentation.NTDocumentationAttribute";
    private const string CompilerGeneratedAttributeFullName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";

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

        var resultProvider = context.CompilationProvider
            .Combine(context.AdditionalTextsProvider.Collect())
            .Select(static (input, cancellationToken) => BuildResult(input.Left, input.Right, cancellationToken));

        context.RegisterSourceOutput(resultProvider, static (sourceProductionContext, result) => {
            if (result.Error is not null) {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(GenerationFailed, Location.None, result.Error));
            }

            if (result.Model is not null) {
                sourceProductionContext.AddSource("GeneratedCodeDocumentation.g.cs", SourceText.From(BuildGeneratedSource(result.Model), Encoding.UTF8));
            }
        });
    }

    private static readonly DiagnosticDescriptor GenerationFailed = new("NTDOC001", "Code documentation could not be generated", "{0}", "NTCodeDocumentation", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static GenerationResult BuildResult(Compilation compilation, ImmutableArray<AdditionalText> additionalTexts, CancellationToken cancellationToken) {
        var assemblyName = GetDocumentedAssemblyName(compilation);
        if (assemblyName is null) {
            return new GenerationResult(null, null);
        }

        var assembly = compilation.SourceModule.ReferencedAssemblySymbols.FirstOrDefault(reference => string.Equals(reference.Name, assemblyName, StringComparison.Ordinal));
        if (assembly is null) {
            return new GenerationResult(null, $"The documented assembly '{assemblyName}' is not referenced.");
        }

        var documentationFile = additionalTexts.FirstOrDefault(text => string.Equals(Path.GetFileName(text.Path), assemblyName + ".xml", StringComparison.OrdinalIgnoreCase));
        if (documentationFile?.GetText(cancellationToken) is not { } documentationText) {
            return new GenerationResult(null, $"The XML documentation file '{assemblyName}.xml' is not an additional file.");
        }

        var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions ?? CSharpParseOptions.Default;
        var context = new DocumentationContext(assembly, ReadXmlDocumentation(documentationText), SourceFacts.Create(additionalTexts, parseOptions, cancellationToken));
        return new GenerationResult(BuildModel(context, assemblyName, cancellationToken), null);
    }

    private static AssemblyDocumentationModel BuildModel(DocumentationContext context, string assemblyName, CancellationToken cancellationToken) {
        var types = ImmutableArray.CreateBuilder<TypeDocumentationModel>();

        foreach (var type in EnumerateTypes(context.Assembly.GlobalNamespace)) {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ShouldIncludeType(type)) {
                continue;
            }

            types.Add(BuildTypeDocumentation(context, type, cancellationToken));
        }

        var orderedTypes = types
            .ToImmutable()
            .Sort(static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));

        return new AssemblyDocumentationModel(assemblyName, CreateGeneratedNamespace(assemblyName), orderedTypes);
    }

    private static TypeDocumentationModel BuildTypeDocumentation(DocumentationContext context, INamedTypeSymbol type, CancellationToken cancellationToken) {
        var methods = ImmutableArray.CreateBuilder<MethodDocumentationModel>();
        var properties = ImmutableArray.CreateBuilder<PropertyDocumentationModel>();
        var fields = ImmutableArray.CreateBuilder<FieldDocumentationModel>();
        var xmlDocumentation = context.GetXmlDocumentation(type);
        var sourceFile = context.Sources.GetTypeFile(type);

        foreach (var documentedMember in EnumerateMembersIncludingBaseTypes(context.Assembly, type)) {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ShouldIncludeMember(context, documentedMember.Member)) {
                continue;
            }

            switch (documentedMember.Member) {
                case IMethodSymbol method when ShouldIncludeMethod(method):
                    methods.Add(BuildMethodDocumentation(context, method, documentedMember.IsFromBaseType));
                    break;
                case IPropertySymbol property:
                    properties.Add(BuildPropertyDocumentation(context, property, documentedMember.IsFromBaseType));
                    break;
                case IFieldSymbol field:
                    fields.Add(BuildFieldDocumentation(context, field, documentedMember.IsFromBaseType));
                    break;
            }
        }

        return new TypeDocumentationModel(
            type.Name,
            type.ToDisplayString(TypeDisplayFormat),
            type.BaseType?.ToDisplayString(TypeDisplayFormat) ?? string.Empty,
            type.TypeKind.ToString(),
            type.DeclaredAccessibility.ToString(),
            sourceFile.Folder,
            sourceFile.FileName,
            ExtractSummary(xmlDocumentation),
            ExtractRemarks(xmlDocumentation),
            xmlDocumentation,
            GetDocumentationMetadata(type),
            GetObsoleteDocumentation(type),
            methods.ToImmutable().Sort(static (left, right) => CompareBySignatureAndDeclaringType(left.Signature, left.DeclaringTypeFullName, right.Signature, right.DeclaringTypeFullName)),
            properties.ToImmutable().Sort(static (left, right) => CompareBySignatureAndDeclaringType(left.Signature, left.DeclaringTypeFullName, right.Signature, right.DeclaringTypeFullName)),
            fields.ToImmutable().Sort(static (left, right) => CompareBySignatureAndDeclaringType(left.Signature, left.DeclaringTypeFullName, right.Signature, right.DeclaringTypeFullName)));
    }

    private static MethodDocumentationModel BuildMethodDocumentation(DocumentationContext context, IMethodSymbol method, bool isFromBaseType) {
        var xmlDocumentation = context.GetXmlDocumentation(method);
        return new MethodDocumentationModel(
            method.Name,
            method.ToDisplayString(MethodDisplayFormat),
            method.DeclaredAccessibility.ToString(),
            ExtractSummary(xmlDocumentation),
            xmlDocumentation,
            method.ContainingType?.ToDisplayString(TypeDisplayFormat) ?? string.Empty,
            isFromBaseType,
            GetObsoleteDocumentation(method));
    }

    private static PropertyDocumentationModel BuildPropertyDocumentation(DocumentationContext context, IPropertySymbol property, bool isFromBaseType) {
        var xmlDocumentation = context.GetXmlDocumentation(property);
        return new PropertyDocumentationModel(
            property.Name,
            property.ToDisplayString(PropertyDisplayFormat),
            property.DeclaredAccessibility.ToString(),
            property.Type.ToDisplayString(MemberTypeDisplayFormat),
            property.Type.ToDisplayString(TypeDisplayFormat),
            context.Sources.GetDefaultValueExpression(property),
            ExtractSummary(xmlDocumentation),
            xmlDocumentation,
            property.ContainingType?.ToDisplayString(TypeDisplayFormat) ?? string.Empty,
            isFromBaseType,
            HasAttribute(property, "Microsoft.AspNetCore.Components.ParameterAttribute"),
            HasAttribute(property, "Microsoft.AspNetCore.Components.CascadingParameterAttribute"),
            HasAttribute(property, "Microsoft.AspNetCore.Components.EditorRequiredAttribute"),
            GetObsoleteDocumentation(property));
    }

    private static FieldDocumentationModel BuildFieldDocumentation(DocumentationContext context, IFieldSymbol field, bool isFromBaseType) {
        var xmlDocumentation = context.GetXmlDocumentation(field);
        return new FieldDocumentationModel(
            field.Name,
            field.ToDisplayString(FieldDisplayFormat),
            field.DeclaredAccessibility.ToString(),
            field.Type.ToDisplayString(MemberTypeDisplayFormat),
            field.Type.ToDisplayString(TypeDisplayFormat),
            GetConstantValue(field),
            ExtractSummary(xmlDocumentation),
            xmlDocumentation,
            field.ContainingType?.ToDisplayString(TypeDisplayFormat) ?? string.Empty,
            isFromBaseType,
            GetObsoleteDocumentation(field));
    }

    private static int CompareBySignatureAndDeclaringType(string leftSignature, string leftDeclaringType, string rightSignature, string rightDeclaringType) {
        var signatureComparison = string.CompareOrdinal(leftSignature, rightSignature);
        if (signatureComparison != 0) {
            return signatureComparison;
        }

        return string.CompareOrdinal(leftDeclaringType, rightDeclaringType);
    }

    private static IEnumerable<DocumentedMember> EnumerateMembersIncludingBaseTypes(IAssemblySymbol assembly, INamedTypeSymbol type) {
        foreach (var member in type.GetMembers()) {
            yield return new DocumentedMember(member, false);
        }

        // Only base types from the documented assembly contribute members, so framework bases such as ComponentBase are left out.
        var baseType = type.BaseType;
        while (baseType is not null) {
            if (SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, assembly)) {
                foreach (var member in baseType.GetMembers()) {
                    yield return new DocumentedMember(member, true);
                }
            }

            baseType = baseType.BaseType;
        }
    }

    // Consumers only show public types and their public or protected members, so nothing else is generated.
    private static bool IsVisible(Accessibility accessibility) => accessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal;

    private static bool ShouldIncludeMember(DocumentationContext context, ISymbol member) {
        if (member.IsImplicitlyDeclared || !IsVisible(member.DeclaredAccessibility) || HasAttribute(member, CompilerGeneratedAttributeFullName)) {
            return false;
        }

        // Metadata keeps the compiler's implicit parameterless constructor, which source never declared.
        return member is not IMethodSymbol { MethodKind: MethodKind.Constructor } constructor || context.Sources.HasExplicitConstructor(constructor.ContainingType);
    }

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
        builder.AppendLine("    /// <param name=\"accessibility\">The declared accessibility.</param>");
        builder.AppendLine("    /// <param name=\"sourceFolder\">The source folder that contains the type.</param>");
        builder.AppendLine("    /// <param name=\"sourceFileName\">The source file that declares the type.</param>");
        builder.AppendLine("    /// <param name=\"summary\">The extracted summary text.</param>");
        builder.AppendLine("    /// <param name=\"remarks\">The extracted remarks text.</param>");
        builder.AppendLine("    /// <param name=\"xmlDocumentation\">The full XML documentation block.</param>");
        builder.AppendLine("    /// <param name=\"renderCompatibility\">The declared render compatibility.</param>");
        builder.AppendLine("    /// <param name=\"compatibilitySummary\">The short render compatibility summary.</param>");
        builder.AppendLine("    /// <param name=\"compatibilityDetails\">The detailed render compatibility guidance.</param>");
        builder.AppendLine("    /// <param name=\"isObsolete\">Indicates whether the type has the Obsolete attribute.</param>");
        builder.AppendLine("    /// <param name=\"obsoleteMessage\">The obsolete message, when provided.</param>");
        builder.AppendLine("    /// <param name=\"isObsoleteError\">Indicates whether use of the obsolete type is treated as an error.</param>");
        builder.AppendLine("    /// <param name=\"methods\">The documented methods.</param>");
        builder.AppendLine("    /// <param name=\"properties\">The documented properties.</param>");
        builder.AppendLine("    /// <param name=\"fields\">The documented fields.</param>");
        builder.AppendLine("    public TypeDocumentation(");
        builder.AppendLine("        string name,");
        builder.AppendLine("        string fullName,");
        builder.AppendLine("        string baseTypeFullName,");
        builder.AppendLine("        string kind,");
        builder.AppendLine("        string accessibility,");
        builder.AppendLine("        string sourceFolder,");
        builder.AppendLine("        string sourceFileName,");
        builder.AppendLine("        string summary,");
        builder.AppendLine("        string remarks,");
        builder.AppendLine("        string xmlDocumentation,");
        builder.AppendLine("        string renderCompatibility,");
        builder.AppendLine("        string compatibilitySummary,");
        builder.AppendLine("        string compatibilityDetails,");
        builder.AppendLine("        bool isObsolete,");
        builder.AppendLine("        string obsoleteMessage,");
        builder.AppendLine("        bool isObsoleteError,");
        builder.AppendLine("        global::System.Collections.Generic.IReadOnlyList<MethodDocumentation> methods,");
        builder.AppendLine("        global::System.Collections.Generic.IReadOnlyList<PropertyDocumentation> properties,");
        builder.AppendLine("        global::System.Collections.Generic.IReadOnlyList<FieldDocumentation> fields) {");
        builder.AppendLine("        Name = name;");
        builder.AppendLine("        FullName = fullName;");
        builder.AppendLine("        BaseTypeFullName = baseTypeFullName;");
        builder.AppendLine("        Kind = kind;");
        builder.AppendLine("        Accessibility = accessibility;");
        builder.AppendLine("        SourceFolder = sourceFolder;");
        builder.AppendLine("        SourceFileName = sourceFileName;");
        builder.AppendLine("        Summary = summary;");
        builder.AppendLine("        Remarks = remarks;");
        builder.AppendLine("        XmlDocumentation = xmlDocumentation;");
        builder.AppendLine("        RenderCompatibility = renderCompatibility;");
        builder.AppendLine("        CompatibilitySummary = compatibilitySummary;");
        builder.AppendLine("        CompatibilityDetails = compatibilityDetails;");
        builder.AppendLine("        IsSsrCompatible = string.Equals(renderCompatibility, \"SsrCompatible\", global::System.StringComparison.Ordinal) || string.Equals(renderCompatibility, \"ProgressivelyEnhanced\", global::System.StringComparison.Ordinal);");
        builder.AppendLine("        IsObsolete = isObsolete;");
        builder.AppendLine("        ObsoleteMessage = obsoleteMessage;");
        builder.AppendLine("        IsObsoleteError = isObsoleteError;");
        builder.AppendLine("        Methods = methods;");
        builder.AppendLine("        Properties = properties;");
        builder.AppendLine("        Fields = fields;");
        builder.AppendLine("        Parameters = global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Where(Properties, static property => property.IsParameter));");
        builder.AppendLine("        CascadingParameters = global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Where(Properties, static property => property.IsCascadingParameter));");
        builder.AppendLine("        ObsoleteMethods = global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Where(Methods, static method => method.IsObsolete));");
        builder.AppendLine("        ObsoleteProperties = global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Where(Properties, static property => property.IsObsolete));");
        builder.AppendLine("        ObsoleteFields = global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Where(Fields, static field => field.IsObsolete));");
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
        builder.AppendLine("    /// Gets the declared accessibility.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Accessibility { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the source folder that contains the type.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string SourceFolder { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the source file that declares the type.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string SourceFileName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the fully qualified base type name, when present.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string BaseTypeFullName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the summary text extracted from XML docs.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Summary { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the remarks text extracted from XML docs.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Remarks { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the full XML documentation block.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string XmlDocumentation { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the render compatibility classification.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string RenderCompatibility { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this type is compatible with static SSR output.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsSsrCompatible { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the short render compatibility summary.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string CompatibilitySummary { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets detailed render compatibility guidance.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string CompatibilityDetails { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this type has the Obsolete attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsObsolete { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the obsolete message, when provided.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string ObsoleteMessage { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether use of this obsolete type is treated as an error.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsObsoleteError { get; }");
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
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets documented properties marked with the Parameter attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public global::System.Collections.Generic.IReadOnlyList<PropertyDocumentation> Parameters { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets documented properties marked with the CascadingParameter attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public global::System.Collections.Generic.IReadOnlyList<PropertyDocumentation> CascadingParameters { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets documented methods marked with the Obsolete attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public global::System.Collections.Generic.IReadOnlyList<MethodDocumentation> ObsoleteMethods { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets documented properties marked with the Obsolete attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public global::System.Collections.Generic.IReadOnlyList<PropertyDocumentation> ObsoleteProperties { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets documented fields marked with the Obsolete attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public global::System.Collections.Generic.IReadOnlyList<FieldDocumentation> ObsoleteFields { get; }");
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
        builder.AppendLine("    /// <param name=\"accessibility\">The declared accessibility.</param>");
        builder.AppendLine("    /// <param name=\"summary\">The method summary.</param>");
        builder.AppendLine("    /// <param name=\"xmlDocumentation\">The full XML documentation block.</param>");
        builder.AppendLine("    /// <param name=\"declaringTypeFullName\">The fully qualified type that declares the method.</param>");
        builder.AppendLine("    /// <param name=\"isFromBaseType\">Indicates whether the method was inherited from a base class.</param>");
        builder.AppendLine("    /// <param name=\"isObsolete\">Indicates whether the method has the Obsolete attribute.</param>");
        builder.AppendLine("    /// <param name=\"obsoleteMessage\">The obsolete message, when provided.</param>");
        builder.AppendLine("    /// <param name=\"isObsoleteError\">Indicates whether use of the obsolete method is treated as an error.</param>");
        builder.AppendLine("    public MethodDocumentation(string name, string signature, string accessibility, string summary, string xmlDocumentation, string declaringTypeFullName, bool isFromBaseType, bool isObsolete, string obsoleteMessage, bool isObsoleteError) {");
        builder.AppendLine("        Name = name;");
        builder.AppendLine("        Signature = signature;");
        builder.AppendLine("        Accessibility = accessibility;");
        builder.AppendLine("        Summary = summary;");
        builder.AppendLine("        XmlDocumentation = xmlDocumentation;");
        builder.AppendLine("        DeclaringTypeFullName = declaringTypeFullName;");
        builder.AppendLine("        IsFromBaseType = isFromBaseType;");
        builder.AppendLine("        IsObsolete = isObsolete;");
        builder.AppendLine("        ObsoleteMessage = obsoleteMessage;");
        builder.AppendLine("        IsObsoleteError = isObsoleteError;");
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
        builder.AppendLine("    /// Gets the declared accessibility.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Accessibility { get; }");
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
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this method has the Obsolete attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsObsolete { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the obsolete message, when provided.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string ObsoleteMessage { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether use of this obsolete method is treated as an error.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsObsoleteError { get; }");
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
        builder.AppendLine("    /// <param name=\"accessibility\">The declared accessibility.</param>");
        builder.AppendLine("    /// <param name=\"typeDisplayName\">The display name of the property type.</param>");
        builder.AppendLine("    /// <param name=\"typeFullName\">The fully qualified property type.</param>");
        builder.AppendLine("    /// <param name=\"summary\">The property summary.</param>");
        builder.AppendLine("    /// <param name=\"xmlDocumentation\">The full XML documentation block.</param>");
        builder.AppendLine("    /// <param name=\"declaringTypeFullName\">The fully qualified type that declares the property.</param>");
        builder.AppendLine("    /// <param name=\"isFromBaseType\">Indicates whether the property was inherited from a base class.</param>");
        builder.AppendLine("    /// <param name=\"isParameter\">Indicates whether the property has the Parameter attribute.</param>");
        builder.AppendLine("    /// <param name=\"isCascadingParameter\">Indicates whether the property has the CascadingParameter attribute.</param>");
        builder.AppendLine("    /// <param name=\"isEditorRequired\">Indicates whether the property has the EditorRequired attribute.</param>");
        builder.AppendLine("    /// <param name=\"isObsolete\">Indicates whether the property has the Obsolete attribute.</param>");
        builder.AppendLine("    /// <param name=\"obsoleteMessage\">The obsolete message, when provided.</param>");
        builder.AppendLine("    /// <param name=\"isObsoleteError\">Indicates whether use of the obsolete property is treated as an error.</param>");
        builder.AppendLine("    public PropertyDocumentation(string name, string signature, string accessibility, string typeDisplayName, string typeFullName, string summary, string xmlDocumentation, string declaringTypeFullName, bool isFromBaseType, bool isParameter, bool isCascadingParameter, bool isEditorRequired, bool isObsolete, string obsoleteMessage, bool isObsoleteError)");
        builder.AppendLine("        : this(name, signature, accessibility, typeDisplayName, typeFullName, string.Empty, summary, xmlDocumentation, declaringTypeFullName, isFromBaseType, isParameter, isCascadingParameter, isEditorRequired, isObsolete, obsoleteMessage, isObsoleteError) { }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Creates a new instance of <see cref=\"PropertyDocumentation\" /> with its declared default-value expression.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"name\">The property name.</param>");
        builder.AppendLine("    /// <param name=\"signature\">The property signature.</param>");
        builder.AppendLine("    /// <param name=\"accessibility\">The declared accessibility.</param>");
        builder.AppendLine("    /// <param name=\"typeDisplayName\">The display name of the property type.</param>");
        builder.AppendLine("    /// <param name=\"typeFullName\">The fully qualified property type.</param>");
        builder.AppendLine("    /// <param name=\"defaultValueExpression\">The source expression used to initialize the property, when declared.</param>");
        builder.AppendLine("    /// <param name=\"summary\">The property summary.</param>");
        builder.AppendLine("    /// <param name=\"xmlDocumentation\">The full XML documentation block.</param>");
        builder.AppendLine("    /// <param name=\"declaringTypeFullName\">The fully qualified type that declares the property.</param>");
        builder.AppendLine("    /// <param name=\"isFromBaseType\">Indicates whether the property was inherited from a base class.</param>");
        builder.AppendLine("    /// <param name=\"isParameter\">Indicates whether the property has the Parameter attribute.</param>");
        builder.AppendLine("    /// <param name=\"isCascadingParameter\">Indicates whether the property has the CascadingParameter attribute.</param>");
        builder.AppendLine("    /// <param name=\"isEditorRequired\">Indicates whether the property has the EditorRequired attribute.</param>");
        builder.AppendLine("    /// <param name=\"isObsolete\">Indicates whether the property has the Obsolete attribute.</param>");
        builder.AppendLine("    /// <param name=\"obsoleteMessage\">The obsolete message, when provided.</param>");
        builder.AppendLine("    /// <param name=\"isObsoleteError\">Indicates whether use of the obsolete property is treated as an error.</param>");
        builder.AppendLine("    public PropertyDocumentation(string name, string signature, string accessibility, string typeDisplayName, string typeFullName, string defaultValueExpression, string summary, string xmlDocumentation, string declaringTypeFullName, bool isFromBaseType, bool isParameter, bool isCascadingParameter, bool isEditorRequired, bool isObsolete, string obsoleteMessage, bool isObsoleteError) {");
        builder.AppendLine("        Name = name;");
        builder.AppendLine("        Signature = signature;");
        builder.AppendLine("        Accessibility = accessibility;");
        builder.AppendLine("        TypeDisplayName = typeDisplayName;");
        builder.AppendLine("        TypeFullName = typeFullName;");
        builder.AppendLine("        DefaultValueExpression = defaultValueExpression;");
        builder.AppendLine("        Summary = summary;");
        builder.AppendLine("        XmlDocumentation = xmlDocumentation;");
        builder.AppendLine("        DeclaringTypeFullName = declaringTypeFullName;");
        builder.AppendLine("        IsFromBaseType = isFromBaseType;");
        builder.AppendLine("        IsParameter = isParameter;");
        builder.AppendLine("        IsCascadingParameter = isCascadingParameter;");
        builder.AppendLine("        IsEditorRequired = isEditorRequired;");
        builder.AppendLine("        IsObsolete = isObsolete;");
        builder.AppendLine("        ObsoleteMessage = obsoleteMessage;");
        builder.AppendLine("        IsObsoleteError = isObsoleteError;");
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
        builder.AppendLine("    /// Gets the declared accessibility.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Accessibility { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the property type display name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string TypeDisplayName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the fully qualified property type.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string TypeFullName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the source expression used to initialize this property, or an empty string when no initializer is declared.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string DefaultValueExpression { get; }");
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
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this property has the Parameter attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsParameter { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this property has the CascadingParameter attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsCascadingParameter { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this property has the EditorRequired attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsEditorRequired { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this property has the Obsolete attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsObsolete { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the obsolete message, when provided.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string ObsoleteMessage { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether use of this obsolete property is treated as an error.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsObsoleteError { get; }");
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
        builder.AppendLine("    /// <param name=\"accessibility\">The declared accessibility.</param>");
        builder.AppendLine("    /// <param name=\"typeDisplayName\">The display name of the field type.</param>");
        builder.AppendLine("    /// <param name=\"typeFullName\">The fully qualified field type.</param>");
        builder.AppendLine("    /// <param name=\"constantValue\">The constant field value, when available.</param>");
        builder.AppendLine("    /// <param name=\"summary\">The field summary.</param>");
        builder.AppendLine("    /// <param name=\"xmlDocumentation\">The full XML documentation block.</param>");
        builder.AppendLine("    /// <param name=\"declaringTypeFullName\">The fully qualified type that declares the field.</param>");
        builder.AppendLine("    /// <param name=\"isFromBaseType\">Indicates whether the field was inherited from a base class.</param>");
        builder.AppendLine("    /// <param name=\"isObsolete\">Indicates whether the field has the Obsolete attribute.</param>");
        builder.AppendLine("    /// <param name=\"obsoleteMessage\">The obsolete message, when provided.</param>");
        builder.AppendLine("    /// <param name=\"isObsoleteError\">Indicates whether use of the obsolete field is treated as an error.</param>");
        builder.AppendLine("    public FieldDocumentation(string name, string signature, string accessibility, string typeDisplayName, string typeFullName, string constantValue, string summary, string xmlDocumentation, string declaringTypeFullName, bool isFromBaseType, bool isObsolete, string obsoleteMessage, bool isObsoleteError) {");
        builder.AppendLine("        Name = name;");
        builder.AppendLine("        Signature = signature;");
        builder.AppendLine("        Accessibility = accessibility;");
        builder.AppendLine("        TypeDisplayName = typeDisplayName;");
        builder.AppendLine("        TypeFullName = typeFullName;");
        builder.AppendLine("        ConstantValue = constantValue;");
        builder.AppendLine("        Summary = summary;");
        builder.AppendLine("        XmlDocumentation = xmlDocumentation;");
        builder.AppendLine("        DeclaringTypeFullName = declaringTypeFullName;");
        builder.AppendLine("        IsFromBaseType = isFromBaseType;");
        builder.AppendLine("        IsObsolete = isObsolete;");
        builder.AppendLine("        ObsoleteMessage = obsoleteMessage;");
        builder.AppendLine("        IsObsoleteError = isObsoleteError;");
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
        builder.AppendLine("    /// Gets the declared accessibility.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string Accessibility { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the field type display name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string TypeDisplayName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the fully qualified field type.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string TypeFullName { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the constant value, when available.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string ConstantValue { get; }");
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
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether this field has the Obsolete attribute.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsObsolete { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets the obsolete message, when provided.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public string ObsoleteMessage { get; }");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Gets a value indicating whether use of this obsolete field is treated as an error.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public bool IsObsoleteError { get; }");
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
        builder.AppendLine($"            {ToLiteral(typeDocumentation.Accessibility)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.SourceFolder)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.SourceFileName)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.Summary)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.Remarks)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.XmlDocumentation)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.RenderCompatibility.RenderCompatibility)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.RenderCompatibility.Summary)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.RenderCompatibility.Details)},");
        builder.AppendLine($"            {ToBooleanLiteral(typeDocumentation.Obsolete.IsObsolete)},");
        builder.AppendLine($"            {ToLiteral(typeDocumentation.Obsolete.Message)},");
        builder.AppendLine($"            {ToBooleanLiteral(typeDocumentation.Obsolete.IsError)},");

        builder.AppendLine("            new MethodDocumentation[] {");
        foreach (var method in typeDocumentation.Methods) {
            builder.AppendLine("                new MethodDocumentation(");
            builder.AppendLine($"                    {ToLiteral(method.Name)},");
            builder.AppendLine($"                    {ToLiteral(method.Signature)},");
            builder.AppendLine($"                    {ToLiteral(method.Accessibility)},");
            builder.AppendLine($"                    {ToLiteral(method.Summary)},");
            builder.AppendLine($"                    {ToLiteral(method.XmlDocumentation)},");
            builder.AppendLine($"                    {ToLiteral(method.DeclaringTypeFullName)},");
            builder.AppendLine($"                    {ToBooleanLiteral(method.IsFromBaseType)},");
            builder.AppendLine($"                    {ToBooleanLiteral(method.Obsolete.IsObsolete)},");
            builder.AppendLine($"                    {ToLiteral(method.Obsolete.Message)},");
            builder.AppendLine($"                    {ToBooleanLiteral(method.Obsolete.IsError)}),");
        }
        builder.AppendLine("            },");

        builder.AppendLine("            new PropertyDocumentation[] {");
        foreach (var property in typeDocumentation.Properties) {
            builder.AppendLine("                new PropertyDocumentation(");
            builder.AppendLine($"                    {ToLiteral(property.Name)},");
            builder.AppendLine($"                    {ToLiteral(property.Signature)},");
            builder.AppendLine($"                    {ToLiteral(property.Accessibility)},");
            builder.AppendLine($"                    {ToLiteral(property.TypeDisplayName)},");
            builder.AppendLine($"                    {ToLiteral(property.TypeFullName)},");
            builder.AppendLine($"                    {ToLiteral(property.DefaultValueExpression)},");
            builder.AppendLine($"                    {ToLiteral(property.Summary)},");
            builder.AppendLine($"                    {ToLiteral(property.XmlDocumentation)},");
            builder.AppendLine($"                    {ToLiteral(property.DeclaringTypeFullName)},");
            builder.AppendLine($"                    {ToBooleanLiteral(property.IsFromBaseType)},");
            builder.AppendLine($"                    {ToBooleanLiteral(property.IsParameter)},");
            builder.AppendLine($"                    {ToBooleanLiteral(property.IsCascadingParameter)},");
            builder.AppendLine($"                    {ToBooleanLiteral(property.IsEditorRequired)},");
            builder.AppendLine($"                    {ToBooleanLiteral(property.Obsolete.IsObsolete)},");
            builder.AppendLine($"                    {ToLiteral(property.Obsolete.Message)},");
            builder.AppendLine($"                    {ToBooleanLiteral(property.Obsolete.IsError)}),");
        }
        builder.AppendLine("            },");

        builder.AppendLine("            new FieldDocumentation[] {");
        foreach (var field in typeDocumentation.Fields) {
            builder.AppendLine("                new FieldDocumentation(");
            builder.AppendLine($"                    {ToLiteral(field.Name)},");
            builder.AppendLine($"                    {ToLiteral(field.Signature)},");
            builder.AppendLine($"                    {ToLiteral(field.Accessibility)},");
            builder.AppendLine($"                    {ToLiteral(field.TypeDisplayName)},");
            builder.AppendLine($"                    {ToLiteral(field.TypeFullName)},");
            builder.AppendLine($"                    {ToLiteral(field.ConstantValue)},");
            builder.AppendLine($"                    {ToLiteral(field.Summary)},");
            builder.AppendLine($"                    {ToLiteral(field.XmlDocumentation)},");
            builder.AppendLine($"                    {ToLiteral(field.DeclaringTypeFullName)},");
            builder.AppendLine($"                    {ToBooleanLiteral(field.IsFromBaseType)},");
            builder.AppendLine($"                    {ToBooleanLiteral(field.Obsolete.IsObsolete)},");
            builder.AppendLine($"                    {ToLiteral(field.Obsolete.Message)},");
            builder.AppendLine($"                    {ToBooleanLiteral(field.Obsolete.IsError)}),");
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

    private static string? GetDocumentedAssemblyName(Compilation compilation) {
        foreach (var attribute in compilation.Assembly.GetAttributes()) {
            if (string.Equals(attribute.AttributeClass?.ToDisplayString(), FullyQualifiedAttributeName, StringComparison.Ordinal)) {
                return attribute.ConstructorArguments.FirstOrDefault().Value as string;
            }
        }

        return null;
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
            !IsPublicType(type) ||
            HasAttribute(type, CompilerGeneratedAttributeFullName) ||
            (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct && type.TypeKind != TypeKind.Enum)) {
            return false;
        }

        // Razor emits a public _Imports class for every _Imports.razor; it is not part of the library surface.
        var containingNamespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (string.Equals(type.Name, "_Imports", StringComparison.Ordinal) ||
            containingNamespace.EndsWith(".GeneratedDocumentation", StringComparison.Ordinal) ||
            string.Equals(containingNamespace, AttributeNamespace, StringComparison.Ordinal)) {
            return false;
        }

        return true;
    }

    private static bool IsPublicType(INamedTypeSymbol type) {
        for (var current = type; current is not null; current = current.ContainingType) {
            if (current.DeclaredAccessibility != Accessibility.Public) {
                return false;
            }
        }

        return true;
    }

    private static bool ShouldIncludeMethod(IMethodSymbol method) {
        return method.MethodKind != MethodKind.PropertyGet &&
               method.MethodKind != MethodKind.PropertySet &&
               method.MethodKind != MethodKind.EventAdd &&
               method.MethodKind != MethodKind.EventRemove &&
               method.MethodKind != MethodKind.EventRaise;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeFullName) {
        foreach (var attribute in symbol.GetAttributes()) {
            if (string.Equals(attribute.AttributeClass?.ToDisplayString(), attributeFullName, StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    private static ObsoleteDocumentationModel GetObsoleteDocumentation(ISymbol symbol) {
        foreach (var attribute in symbol.GetAttributes()) {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), "System.ObsoleteAttribute", StringComparison.Ordinal)) {
                continue;
            }

            var message = string.Empty;
            var isError = false;
            if (attribute.ConstructorArguments.Length > 0) {
                message = attribute.ConstructorArguments[0].Value as string ?? string.Empty;
            }

            if (attribute.ConstructorArguments.Length > 1 &&
                attribute.ConstructorArguments[1].Value is bool constructorIsError) {
                isError = constructorIsError;
            }

            return new ObsoleteDocumentationModel(true, message, isError);
        }

        return ObsoleteDocumentationModel.None;
    }

    private static RenderCompatibilityDocumentationModel GetDocumentationMetadata(ISymbol symbol) {
        foreach (var attribute in symbol.GetAttributes()) {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), DocumentationAttributeFullName, StringComparison.Ordinal)) {
                continue;
            }

            var renderCompatibility = "Unknown";
            var summary = string.Empty;
            var details = string.Empty;
            foreach (var argument in attribute.NamedArguments) {
                switch (argument.Key) {
                    case "RenderCompatibility":
                        renderCompatibility = GetEnumArgumentName(argument.Value) ?? argument.Value.Value?.ToString() ?? "Unknown";
                        break;
                    case "CompatibilitySummary":
                        summary = argument.Value.Value as string ?? string.Empty;
                        break;
                    case "CompatibilityDetails":
                        details = argument.Value.Value as string ?? string.Empty;
                        break;
                }
            }

            return new RenderCompatibilityDocumentationModel(renderCompatibility, summary, details);
        }

        return RenderCompatibilityDocumentationModel.None;
    }

    private static string? GetEnumArgumentName(TypedConstant argument) {
        if (argument.Type is not INamedTypeSymbol enumType || argument.Value is null) {
            return null;
        }

        foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>()) {
            if (member.HasConstantValue && Equals(member.ConstantValue, argument.Value)) {
                return member.Name;
            }
        }

        return null;
    }

    private static string GetConstantValue(IFieldSymbol field) {
        if (!field.HasConstantValue) {
            return string.Empty;
        }

        return field.ConstantValue switch {
            null => "null",
            bool value => value ? "true" : "false",
            char value => value.ToString(),
            string value => value,
            IFormattable value => value.ToString(null, global::System.Globalization.CultureInfo.InvariantCulture),
            _ => field.ConstantValue.ToString() ?? string.Empty
        };
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

    /// <summary>
    ///     Reads each <c>member</c> element of a compiler-written XML documentation file in the form
    ///     <see cref="ISymbol.GetDocumentationCommentXml" /> returns for source symbols: dedented to the member element, CRLF line endings.
    /// </summary>
    private static Dictionary<string, string> ReadXmlDocumentation(SourceText text) {
        var members = new Dictionary<string, string>(StringComparer.Ordinal);
        var builder = new StringBuilder();
        string? id = null;
        var indent = 0;

        foreach (var textLine in text.Lines) {
            var line = textLine.ToString();
            var trimmed = line.TrimStart();
            if (id is null) {
                if (trimmed.StartsWith("<member name=\"", StringComparison.Ordinal) && !trimmed.EndsWith("/>", StringComparison.Ordinal)) {
                    id = trimmed.Substring(14, trimmed.IndexOf('"', 14) - 14);
                    indent = line.Length - trimmed.Length;
                    builder.Clear().Append(trimmed).Append("\r\n");
                }

                continue;
            }

            builder.Append(line.Length >= indent && string.IsNullOrWhiteSpace(line.Substring(0, indent)) ? line.Substring(indent) : trimmed).Append("\r\n");
            if (trimmed == "</member>") {
                members[id] = builder.ToString();
                id = null;
            }
        }

        return members;
    }

    private static string ExtractSummary(string xmlDocumentation) {
        return ExtractXmlElement(xmlDocumentation, "summary");
    }

    private static string ExtractRemarks(string xmlDocumentation) {
        return ExtractXmlElement(xmlDocumentation, "remarks");
    }

    private static string ExtractXmlElement(string xmlDocumentation, string elementName) {
        if (string.IsNullOrWhiteSpace(xmlDocumentation)) {
            return string.Empty;
        }

        try {
            var document = XDocument.Parse("<root>" + xmlDocumentation + "</root>");
            var element = document.Root?.Descendants(elementName).FirstOrDefault();
            if (element is null) {
                return string.Empty;
            }

            var builder = new StringBuilder();
            AppendDocumentationText(builder, element);
            return NormalizeWhitespace(builder.ToString());
        }
        catch {
            return string.Empty;
        }
    }

    private static void AppendDocumentationText(StringBuilder builder, XElement element) {
        foreach (var node in element.Nodes()) {
            if (node is XText text) {
                builder.Append(text.Value);
                continue;
            }

            if (node is not XElement child) {
                continue;
            }

            var elementName = child.Name.LocalName;
            var isStructuralElement = elementName is "para" or "item" or "term" or "description" or "list" or "br";
            if (isStructuralElement) {
                builder.Append(' ');
            }

            if (elementName == "see") {
                var cref = child.Attribute("cref")?.Value;
                builder.Append(cref is null ? child.Attribute("langword")?.Value : GetCrefDisplayText(cref));
                if (cref is null && child.Attribute("langword") is null) {
                    AppendDocumentationText(builder, child);
                }
            }
            else if (elementName is "paramref" or "typeparamref") {
                builder.Append(child.Attribute("name")?.Value);
            }
            else {
                AppendDocumentationText(builder, child);
            }

            if (isStructuralElement) {
                builder.Append(' ');
            }
        }
    }

    private static string GetCrefDisplayText(string cref) {
        var memberKind = cref.Length > 1 && cref[1] == ':' ? cref[0] : '\0';
        var value = memberKind == '\0' ? cref : cref.Substring(2);
        var parametersIndex = value.IndexOf('(');
        if (parametersIndex >= 0) {
            value = value.Substring(0, parametersIndex);
        }

        var separatorIndex = value.LastIndexOf('.');
        if (separatorIndex >= 0 && memberKind is 'F' or 'M' or 'P' or 'E') {
            var declaringTypeIndex = value.LastIndexOf('.', separatorIndex - 1);
            value = value.Substring(declaringTypeIndex + 1);
        }
        else if (separatorIndex >= 0) {
            value = value.Substring(separatorIndex + 1);
        }

        var genericArityIndex = value.IndexOf('`');
        while (genericArityIndex >= 0) {
            var genericArityEndIndex = genericArityIndex + 1;
            while (genericArityEndIndex < value.Length && char.IsDigit(value[genericArityEndIndex])) {
                genericArityEndIndex++;
            }

            value = value.Remove(genericArityIndex, genericArityEndIndex - genericArityIndex);
            genericArityIndex = value.IndexOf('`');
        }

        return value.Replace('#', '.');
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
        /// Generates a code documentation model for a referenced assembly. The assembly's XML documentation file and its source
        /// files must be passed to the compiler as additional files.
        /// </summary>
        [global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        internal sealed class GenerateCodeDocumentationAttribute(string assemblyName) : global::System.Attribute {
            /// <summary>
            /// Gets the name of the referenced assembly to document.
            /// </summary>
            public string AssemblyName { get; } = assemblyName;
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
            string accessibility,
            string sourceFolder,
            string sourceFileName,
            string summary,
            string remarks,
            string xmlDocumentation,
            RenderCompatibilityDocumentationModel renderCompatibility,
            ObsoleteDocumentationModel obsolete,
            ImmutableArray<MethodDocumentationModel> methods,
            ImmutableArray<PropertyDocumentationModel> properties,
            ImmutableArray<FieldDocumentationModel> fields) {
            Name = name;
            FullName = fullName;
            BaseTypeFullName = baseTypeFullName;
            Kind = kind;
            Accessibility = accessibility;
            SourceFolder = sourceFolder;
            SourceFileName = sourceFileName;
            Summary = summary;
            Remarks = remarks;
            XmlDocumentation = xmlDocumentation;
            RenderCompatibility = renderCompatibility;
            Obsolete = obsolete;
            Methods = methods;
            Properties = properties;
            Fields = fields;
        }

        public string Name { get; }

        public string FullName { get; }

        public string BaseTypeFullName { get; }

        public string Kind { get; }

        public string Accessibility { get; }

        public string SourceFolder { get; }

        public string SourceFileName { get; }

        public string Summary { get; }

        public string Remarks { get; }

        public string XmlDocumentation { get; }

        public RenderCompatibilityDocumentationModel RenderCompatibility { get; }

        public ObsoleteDocumentationModel Obsolete { get; }

        public ImmutableArray<MethodDocumentationModel> Methods { get; }

        public ImmutableArray<PropertyDocumentationModel> Properties { get; }

        public ImmutableArray<FieldDocumentationModel> Fields { get; }
    }

    private sealed class MethodDocumentationModel {
        public MethodDocumentationModel(
            string name,
            string signature,
            string accessibility,
            string summary,
            string xmlDocumentation,
            string declaringTypeFullName,
            bool isFromBaseType,
            ObsoleteDocumentationModel obsolete) {
            Name = name;
            Signature = signature;
            Accessibility = accessibility;
            Summary = summary;
            XmlDocumentation = xmlDocumentation;
            DeclaringTypeFullName = declaringTypeFullName;
            IsFromBaseType = isFromBaseType;
            Obsolete = obsolete;
        }

        public string Name { get; }

        public string Signature { get; }

        public string Accessibility { get; }

        public string Summary { get; }

        public string XmlDocumentation { get; }

        public string DeclaringTypeFullName { get; }

        public bool IsFromBaseType { get; }

        public ObsoleteDocumentationModel Obsolete { get; }
    }

    private sealed class PropertyDocumentationModel {
        public PropertyDocumentationModel(
            string name,
            string signature,
            string accessibility,
            string typeDisplayName,
            string typeFullName,
            string defaultValueExpression,
            string summary,
            string xmlDocumentation,
            string declaringTypeFullName,
            bool isFromBaseType,
            bool isParameter,
            bool isCascadingParameter,
            bool isEditorRequired,
            ObsoleteDocumentationModel obsolete) {
            Name = name;
            Signature = signature;
            Accessibility = accessibility;
            TypeDisplayName = typeDisplayName;
            TypeFullName = typeFullName;
            DefaultValueExpression = defaultValueExpression;
            Summary = summary;
            XmlDocumentation = xmlDocumentation;
            DeclaringTypeFullName = declaringTypeFullName;
            IsFromBaseType = isFromBaseType;
            IsParameter = isParameter;
            IsCascadingParameter = isCascadingParameter;
            IsEditorRequired = isEditorRequired;
            Obsolete = obsolete;
        }

        public string Name { get; }

        public string Signature { get; }

        public string Accessibility { get; }

        public string TypeDisplayName { get; }

        public string TypeFullName { get; }

        public string DefaultValueExpression { get; }

        public string Summary { get; }

        public string XmlDocumentation { get; }

        public string DeclaringTypeFullName { get; }

        public bool IsFromBaseType { get; }

        public bool IsParameter { get; }

        public bool IsCascadingParameter { get; }

        public bool IsEditorRequired { get; }

        public ObsoleteDocumentationModel Obsolete { get; }
    }

    private sealed class FieldDocumentationModel {
        public FieldDocumentationModel(
            string name,
            string signature,
            string accessibility,
            string typeDisplayName,
            string typeFullName,
            string constantValue,
            string summary,
            string xmlDocumentation,
            string declaringTypeFullName,
            bool isFromBaseType,
            ObsoleteDocumentationModel obsolete) {
            Name = name;
            Signature = signature;
            Accessibility = accessibility;
            TypeDisplayName = typeDisplayName;
            TypeFullName = typeFullName;
            ConstantValue = constantValue;
            Summary = summary;
            XmlDocumentation = xmlDocumentation;
            DeclaringTypeFullName = declaringTypeFullName;
            IsFromBaseType = isFromBaseType;
            Obsolete = obsolete;
        }

        public string Name { get; }

        public string Signature { get; }

        public string Accessibility { get; }

        public string TypeDisplayName { get; }

        public string TypeFullName { get; }

        public string ConstantValue { get; }

        public string Summary { get; }

        public string XmlDocumentation { get; }

        public string DeclaringTypeFullName { get; }

        public bool IsFromBaseType { get; }

        public ObsoleteDocumentationModel Obsolete { get; }
    }

    private sealed class ObsoleteDocumentationModel {
        public static readonly ObsoleteDocumentationModel None = new(false, string.Empty, false);

        public ObsoleteDocumentationModel(bool isObsolete, string message, bool isError) {
            IsObsolete = isObsolete;
            Message = message;
            IsError = isError;
        }

        public bool IsObsolete { get; }

        public string Message { get; }

        public bool IsError { get; }
    }

    private sealed class RenderCompatibilityDocumentationModel {
        public static readonly RenderCompatibilityDocumentationModel None = new("Unknown", string.Empty, string.Empty);

        public RenderCompatibilityDocumentationModel(string renderCompatibility, string summary, string details) {
            RenderCompatibility = renderCompatibility;
            Summary = summary;
            Details = details;
        }

        public string RenderCompatibility { get; }

        public string Summary { get; }

        public string Details { get; }
    }

    private sealed class DocumentedMember {
        public DocumentedMember(ISymbol member, bool isFromBaseType) {
            Member = member;
            IsFromBaseType = isFromBaseType;
        }

        public ISymbol Member { get; }

        public bool IsFromBaseType { get; }
    }

    private sealed class GenerationResult(AssemblyDocumentationModel? model, string? error) {
        public AssemblyDocumentationModel? Model { get; } = model;

        public string? Error { get; } = error;
    }

    private sealed class DocumentationContext(IAssemblySymbol assembly, Dictionary<string, string> xmlDocumentation, SourceFacts sources) {
        public IAssemblySymbol Assembly { get; } = assembly;

        public SourceFacts Sources { get; } = sources;

        // Members of constructed generic bases share the documentation of their original definitions.
        public string GetXmlDocumentation(ISymbol symbol) =>
            symbol.OriginalDefinition.GetDocumentationCommentId() is { } id && xmlDocumentation.TryGetValue(id, out var xml) ? xml : string.Empty;
    }

    private sealed class SourceFile(string folder, string fileName) {
        public static readonly SourceFile None = new(string.Empty, string.Empty);

        public string Folder { get; } = folder;

        public string FileName { get; } = fileName;
    }

    /// <summary>
    ///     Facts that only source code holds: which file declares each type, property initializers, and which types declare
    ///     constructors. Keys use metadata names (<c>Namespace.Outer`1+Inner</c>); Razor-only types are keyed by file name.
    /// </summary>
    private sealed class SourceFacts {
        private readonly Dictionary<string, SourceFile> _typeFiles = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SourceFile> _razorTypeFiles = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _defaultValues = new(StringComparer.Ordinal);
        private readonly HashSet<string> _typesWithConstructors = new(StringComparer.Ordinal);

        public static SourceFacts Create(ImmutableArray<AdditionalText> texts, CSharpParseOptions parseOptions, CancellationToken cancellationToken) {
            var facts = new SourceFacts();

            // C# files come first and in path order, as in the library's compilation, so a partial type reports its first file.
            foreach (var text in texts.Where(static text => text.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).OrderBy(static text => text.Path, StringComparer.OrdinalIgnoreCase)) {
                if (text.GetText(cancellationToken) is not { } source) {
                    continue;
                }

                var file = new SourceFile(GetFolder(text.Path), Path.GetFileName(text.Path));
                foreach (var declaration in CSharpSyntaxTree.ParseText(source, parseOptions, text.Path, cancellationToken).GetRoot(cancellationToken).DescendantNodes().OfType<BaseTypeDeclarationSyntax>()) {
                    var key = GetKey(declaration);
                    if (!facts._typeFiles.ContainsKey(key)) {
                        facts._typeFiles.Add(key, file);
                    }

                    if (declaration is TypeDeclarationSyntax type) {
                        facts.AddMembers(type, key);
                    }
                }
            }

            foreach (var text in texts.Where(static text => text.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))) {
                if (text.GetText(cancellationToken)?.ToString() is not { } source) {
                    continue;
                }

                // A Razor component is named after its file and gets one type parameter per @typeparam directive.
                var name = Path.GetFileNameWithoutExtension(text.Path);
                var arity = source.Split('\n').Count(static line => line.TrimStart().StartsWith("@typeparam ", StringComparison.Ordinal));
                var key = arity == 0 ? name : $"{name}`{arity}";
                if (!facts._razorTypeFiles.ContainsKey(key)) {
                    facts._razorTypeFiles.Add(key, new SourceFile(GetFolder(text.Path), name + "_razor.decl.g.cs"));
                }

                foreach (var block in GetCodeBlocks(source)) {
                    // Parsing the block as the component's class body lets Roslyn find the matching brace and any constructors.
                    var root = CSharpSyntaxTree.ParseText($"class {name} {block}", parseOptions, cancellationToken: cancellationToken).GetRoot(cancellationToken);
                    if (root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault() is { } type) {
                        facts.AddMembers(type, key);
                    }
                }
            }

            return facts;
        }

        public SourceFile GetTypeFile(INamedTypeSymbol type) =>
            _typeFiles.TryGetValue(GetKey(type), out var file) || _razorTypeFiles.TryGetValue(type.OriginalDefinition.MetadataName, out file) ? file : SourceFile.None;

        public string GetDefaultValueExpression(IPropertySymbol property) {
            var type = property.ContainingType.OriginalDefinition;
            return _defaultValues.TryGetValue($"{GetKey(type)}|{property.Name}", out var value) || _defaultValues.TryGetValue($"{type.MetadataName}|{property.Name}", out value) ? value : string.Empty;
        }

        public bool HasExplicitConstructor(INamedTypeSymbol type) =>
            _typesWithConstructors.Contains(GetKey(type)) || _typesWithConstructors.Contains(type.OriginalDefinition.MetadataName);

        private void AddMembers(TypeDeclarationSyntax type, string key) {
            if (type.ParameterList is not null) {
                _typesWithConstructors.Add(key);
            }

            // Razor @code blocks can hold markup templates that C# cannot parse; error recovery there invents members, so they are skipped.
            foreach (var member in type.Members.Where(static member => !member.ContainsDiagnostics)) {
                switch (member) {
                    case PropertyDeclarationSyntax { Initializer.Value: { } initializer } property when !_defaultValues.ContainsKey($"{key}|{property.Identifier.Text}"):
                        _defaultValues.Add($"{key}|{property.Identifier.Text}", initializer.ToString());
                        break;
                    case ConstructorDeclarationSyntax constructor when !constructor.Modifiers.Any(SyntaxKind.StaticKeyword):
                        _typesWithConstructors.Add(key);
                        break;
                }
            }
        }

        private static IEnumerable<string> GetCodeBlocks(string razor) {
            foreach (var directive in new[] { "@code", "@functions" }) {
                for (var index = razor.IndexOf(directive, StringComparison.Ordinal); index >= 0; index = razor.IndexOf(directive, index + directive.Length, StringComparison.Ordinal)) {
                    var brace = razor.IndexOf('{', index + directive.Length);
                    if (brace >= 0 && string.IsNullOrWhiteSpace(razor.Substring(index + directive.Length, brace - index - directive.Length))) {
                        yield return razor.Substring(brace);
                    }
                }
            }
        }

        private static string GetFolder(string path) => Path.GetFileName(Path.GetDirectoryName(path)) ?? string.Empty;

        private static string GetKey(BaseTypeDeclarationSyntax declaration) {
            var parts = new List<string>();
            string? containingNamespace = null;
            for (SyntaxNode? node = declaration; node is not null; node = node.Parent) {
                if (node is BaseTypeDeclarationSyntax type) {
                    var arity = type is TypeDeclarationSyntax { TypeParameterList: { } typeParameters } ? typeParameters.Parameters.Count : 0;
                    parts.Insert(0, arity == 0 ? type.Identifier.Text : $"{type.Identifier.Text}`{arity}");
                }
                else if (node is BaseNamespaceDeclarationSyntax namespaceDeclaration) {
                    containingNamespace = containingNamespace is null ? namespaceDeclaration.Name.ToString() : $"{namespaceDeclaration.Name}.{containingNamespace}";
                }
            }

            return containingNamespace is null ? string.Join("+", parts) : $"{containingNamespace}.{string.Join("+", parts)}";
        }

        private static string GetKey(INamedTypeSymbol type) {
            var parts = new List<string>();
            for (var current = type.OriginalDefinition; current is not null; current = current.ContainingType) {
                parts.Insert(0, current.MetadataName);
            }

            return type.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace ? $"{containingNamespace.ToDisplayString()}.{string.Join("+", parts)}" : string.Join("+", parts);
        }
    }
}

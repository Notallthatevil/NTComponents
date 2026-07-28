using System.Text.Json.Serialization;

namespace NTComponents.MCP.Contracts;

public sealed record ServiceDiscovery(string Name, string Mcp, string OpenApi, string Health, string Api, CatalogOverview Catalog);

public sealed record HealthStatus(string Status);

public sealed record ErrorResponse(string Error);

public sealed record CatalogOverview(
    string ServerVersion,
    string ComponentsVersion,
    string BuildRevision,
    string DocumentationBaseUrl,
    int ComponentCount,
    int ReferenceTypeCount,
    IReadOnlyList<string> ComponentFolders,
    IReadOnlyList<string> ReferenceKinds,
    IReadOnlyList<string> ReferenceScopes);

public sealed record CatalogPage<T>(IReadOnlyList<T> Items, int TotalCount, int Offset, int Limit, bool HasMore, int? NextOffset);

public sealed record McpPage<T>(IReadOnlyList<T> Items, int TotalCount, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? NextOffset);

public sealed record ComponentSummary(string Name, string FullName, string Folder, string Summary, string RenderCompatibility, bool IsObsolete, IReadOnlyList<string> RequiredParameters, string DocumentationUrl);

public sealed record McpComponentSummary(
    string Name,
    string Folder,
    string Summary,
    string RenderCompatibility,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsObsolete,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? RequiredParameters,
    string DocumentationUrl);

public sealed record ComponentUsageSummary(
    string Name,
    string Summary,
    string RenderCompatibility,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsObsolete,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ObsoleteMessage,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<RequiredParameterSummary>? RequiredParameters,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? UsageGuidelines,
    string RazorUsage,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? RelatedTypes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? RelatedComponents,
    int ParameterCount,
    int MethodCount,
    string DocumentationUrl,
    string ResourceUri);

public sealed record RequiredParameterSummary(string Name, string Type, string Summary);

public sealed record ComponentMemberSummary(
    string Name,
    string Kind,
    string Declaration,
    string Summary,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsRequired,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsInherited,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsObsolete,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DefaultValue,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Category);

public sealed record ComponentDetails(
    string Name,
    string FullName,
    string Folder,
    string SourceFile,
    string Summary,
    string Remarks,
    string RenderCompatibility,
    bool IsSsrCompatible,
    string CompatibilitySummary,
    string CompatibilityDetails,
    bool IsObsolete,
    string ObsoleteMessage,
    IReadOnlyList<ParameterDetails> Parameters,
    IReadOnlyList<MemberDetails> Methods,
    IReadOnlyList<ReferenceSummary> RelatedTypes,
    IReadOnlyList<ComponentSummary> RelatedComponents,
    IReadOnlyList<string> UsageGuidelines,
    string RazorUsage,
    IReadOnlyList<UsageExample> UsageExamples,
    string DocumentationUrl,
    IReadOnlyList<RelatedEnumDetails> RelatedEnums) {
    public ComponentDetails(
        string name,
        string fullName,
        string folder,
        string sourceFile,
        string summary,
        string remarks,
        string renderCompatibility,
        bool isSsrCompatible,
        string compatibilitySummary,
        string compatibilityDetails,
        bool isObsolete,
        string obsoleteMessage,
        IReadOnlyList<ParameterDetails> parameters,
        IReadOnlyList<MemberDetails> methods,
        IReadOnlyList<ReferenceSummary> relatedTypes,
        IReadOnlyList<string> usageGuidelines)
        : this(name, fullName, folder, sourceFile, summary, remarks, renderCompatibility, isSsrCompatible, compatibilitySummary, compatibilityDetails, isObsolete, obsoleteMessage, parameters, methods, relatedTypes, [], usageGuidelines, string.Empty, [], string.Empty, []) { }
}

public sealed record UsageExample(string Title, string Description, string Razor);

public sealed record ParameterDetails(string Name, string Type, string Summary, bool IsRequired, bool IsCascading, bool IsInherited, bool IsObsolete, string Accessibility, string? DefaultValueExpression, string Category, int CategoryOrder) {
    public ParameterDetails(string name, string type, string summary, bool isRequired, bool isCascading, bool isInherited, bool isObsolete)
        : this(name, type, summary, isRequired, isCascading, isInherited, isObsolete, string.Empty, null, string.Empty, 0) { }
}

public sealed record MemberDetails(string Name, string Signature, string Summary, bool IsInherited, bool IsObsolete, string Accessibility) {
    public MemberDetails(string name, string signature, string summary, bool isInherited, bool isObsolete)
        : this(name, signature, summary, isInherited, isObsolete, string.Empty) { }
}

public sealed record ReferenceSummary(string Name, string FullName, string Kind, string Summary, bool IsObsolete, IReadOnlyList<string> UsedByComponents, string Scope, string DocumentationUrl);

public sealed record McpReferenceSummary(string Name, string Kind, string Summary, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsObsolete, string Scope, string DocumentationUrl);

public sealed record ReferenceUsageSummary(
    string Name,
    string Kind,
    string Summary,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Remarks,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsObsolete,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ObsoleteMessage,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? UsedByComponents,
    string Scope,
    string DocumentationUrl,
    string ResourceUri,
    McpPage<ReferenceMemberSummary> Members);

public sealed record ReferenceMemberSummary(string Name, string Kind, string Declaration, string Summary, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsObsolete);

public sealed record ReferenceDetails(
    string Name,
    string FullName,
    string Kind,
    string Folder,
    string SourceFile,
    string Summary,
    string Remarks,
    bool IsObsolete,
    string ObsoleteMessage,
    IReadOnlyList<FieldDetails> Fields,
    IReadOnlyList<ParameterDetails> Properties,
    IReadOnlyList<MemberDetails> Methods,
    IReadOnlyList<string> UsedByComponents,
    string Scope,
    string DocumentationUrl);

public sealed record FieldDetails(string Name, string Type, string Value, string Summary, bool IsObsolete);

public sealed record RelatedEnumDetails(string Name, string FullName, IReadOnlyList<FieldDetails> Values, int TotalValueCount, bool IsTruncated);

public sealed record DocumentationSearchResult(string Name, string FullName, string Category, string Summary, string Folder, int Score, IReadOnlyList<string> MatchedTerms, IReadOnlyList<string> MatchedFields, string DocumentationUrl);

public sealed record DocumentationSearchPage(IReadOnlyList<DocumentationSearchResult> Items, int TotalCount, int Offset, int Limit, bool HasMore, int? NextOffset, string? DidYouMean);

public sealed record McpDocumentationSearchResult(string Name, string Category, string Summary, string DocumentationUrl);

public sealed record McpDocumentationSearchPage(IReadOnlyList<McpDocumentationSearchResult> Items, int TotalCount, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? NextOffset, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DidYouMean);

public sealed record LookupResult<T>(bool Found, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] T? Value, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Error) where T : class {
    public static LookupResult<T> Success(T value) => new(true, value, null);

    public static LookupResult<T> Missing(string error) => new(false, null, error);
}

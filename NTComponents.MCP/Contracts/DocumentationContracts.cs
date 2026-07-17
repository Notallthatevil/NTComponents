namespace NTComponents.MCP.Contracts;

public sealed record CatalogOverview(int ComponentCount, int ReferenceTypeCount, IReadOnlyList<string> ComponentFolders, IReadOnlyList<string> ReferenceKinds);

public sealed record ComponentSummary(string Name, string FullName, string Folder, string Summary, string RenderCompatibility, bool IsObsolete, IReadOnlyList<string> RequiredParameters);

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
    IReadOnlyList<string> UsageGuidelines);

public sealed record ParameterDetails(string Name, string Type, string Summary, bool IsRequired, bool IsCascading, bool IsInherited, bool IsObsolete);

public sealed record MemberDetails(string Name, string Signature, string Summary, bool IsInherited, bool IsObsolete);

public sealed record ReferenceSummary(string Name, string FullName, string Kind, string Summary, bool IsObsolete, IReadOnlyList<string> UsedByComponents);

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
    IReadOnlyList<string> UsedByComponents);

public sealed record FieldDetails(string Name, string Type, string Value, string Summary, bool IsObsolete);

public sealed record DocumentationSearchResult(string Name, string FullName, string Category, string Summary, string Folder, int Score);

public sealed record LookupResult<T>(bool Found, T? Value, string? Error) where T : class {
    public static LookupResult<T> Success(T value) => new(true, value, null);

    public static LookupResult<T> Missing(string error) => new(false, null, error);
}

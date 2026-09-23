using NTComponents.MCP.Contracts;

namespace NTComponents.MCP.Catalog;

internal static class McpReferenceProjector {
    public static ReferenceUsageSummary Project(ReferenceDetails reference, string? query, int limit, int offset) {
        var members = reference.Fields
            .Select(field => (Name: field.Name, Kind: "Field", Detail: (object)field))
            .Concat(reference.Properties.Select(property => (property.Name, Kind: "Property", Detail: (object)property)))
            .Concat(reference.Methods.Select(method => (method.Name, Kind: "Method", Detail: (object)method)))
            .Where(member => string.IsNullOrWhiteSpace(query) || Matches(member.Detail, query))
            .OrderBy(member => member.Kind, StringComparer.Ordinal)
            .ThenBy(member => member.Name, StringComparer.Ordinal)
            .ToArray();
        var items = members.Skip(offset).Take(limit).Select(member => member.Detail switch {
            FieldDetails field => new ReferenceMemberSummary(field.Name, "Field", $"{field.Type} {field.Name} = {field.Value}", field.Summary, TrueOrNull(field.IsObsolete)),
            ParameterDetails property => new ReferenceMemberSummary(property.Name, "Property", property.Type, property.Summary, TrueOrNull(property.IsObsolete)),
            MemberDetails method => new ReferenceMemberSummary(method.Name, "Method", method.Signature, method.Summary, TrueOrNull(method.IsObsolete)),
            _ => throw new InvalidOperationException("Unsupported reference member."),
        }).ToArray();
        var nextOffset = (long)offset + items.Length;
        return new(
            reference.Name,
            reference.Kind,
            reference.Summary,
            NullIfEmpty(reference.Remarks),
            TrueOrNull(reference.IsObsolete),
            NullIfEmpty(reference.ObsoleteMessage),
            NullIfEmpty(reference.UsedByComponents),
            reference.Scope,
            reference.DocumentationUrl,
            $"ntcomponents://references/{reference.Name}",
            new(items, members.Length, nextOffset < members.Length ? (int)nextOffset : null));
    }

    private static bool Matches(object detail, string query) => detail switch {
        FieldDetails field => field.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || field.Summary.Contains(query, StringComparison.OrdinalIgnoreCase)
            || $"{field.Type} {field.Name} = {field.Value}".Contains(query, StringComparison.OrdinalIgnoreCase),
        ParameterDetails property => property.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || property.Type.Contains(query, StringComparison.OrdinalIgnoreCase)
            || property.Summary.Contains(query, StringComparison.OrdinalIgnoreCase),
        MemberDetails method => method.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || method.Signature.Contains(query, StringComparison.OrdinalIgnoreCase)
            || method.Summary.Contains(query, StringComparison.OrdinalIgnoreCase),
        _ => false,
    };

    private static bool? TrueOrNull(bool value) => value ? true : null;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static IReadOnlyList<T>? NullIfEmpty<T>(IReadOnlyList<T> values) => values.Count == 0 ? null : values;
}

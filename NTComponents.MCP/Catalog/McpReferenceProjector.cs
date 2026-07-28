using NTComponents.MCP.Contracts;

namespace NTComponents.MCP.Catalog;

internal static class McpReferenceProjector {
    public static ReferenceUsageSummary Project(ReferenceDetails reference, string? query, int limit, int offset) {
        var members = reference.Fields
            .Select(field => new ReferenceMemberSummary(field.Name, "Field", $"{field.Type} {field.Name} = {field.Value}", field.Summary, TrueOrNull(field.IsObsolete)))
            .Concat(reference.Properties.Select(property => new ReferenceMemberSummary(property.Name, "Property", property.Type, property.Summary, TrueOrNull(property.IsObsolete))))
            .Concat(reference.Methods.Select(method => new ReferenceMemberSummary(method.Name, "Method", method.Signature, method.Summary, TrueOrNull(method.IsObsolete))))
            .Where(member => string.IsNullOrWhiteSpace(query) || Matches(member, query))
            .OrderBy(member => member.Kind, StringComparer.Ordinal)
            .ThenBy(member => member.Name, StringComparer.Ordinal)
            .ToArray();
        var items = members.Skip(offset).Take(limit).ToArray();
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

    private static bool Matches(ReferenceMemberSummary member, string query) =>
        member.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
        || member.Declaration.Contains(query, StringComparison.OrdinalIgnoreCase)
        || member.Summary.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool? TrueOrNull(bool value) => value ? true : null;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static IReadOnlyList<T>? NullIfEmpty<T>(IReadOnlyList<T> values) => values.Count == 0 ? null : values;
}

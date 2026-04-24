using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NTComponents.GeneratedDocumentation;

namespace NTComponents.Site.Documentation;

public sealed class DocumentationCatalog {

    private static readonly string[] _componentTypeFullNames = [
        "NTComponents.NTButton",
        "NTComponents.NTCard"
    ];

    private static readonly string[] _featuredTypeFullNames = [
        "NTComponents.NTCardVariant",
        "NTComponents.NTElevation",
        "NTComponents.NTTypography",
        "NTComponents.TnTColor"
    ];

    private readonly IReadOnlyDictionary<string, DocumentationTypePage> _componentTypesBySlug;
    private readonly IReadOnlyDictionary<string, DocumentationTypePage> _referenceTypesBySlug;
    private readonly IReadOnlyDictionary<string, string> _typeHrefsByFullName;
    private readonly IReadOnlyDictionary<string, string> _typeHrefsByName;
    private readonly IReadOnlyDictionary<string, DocumentationTypePage> _typesBySlug;

    public DocumentationCatalog() {
        var rawPages = GeneratedCodeDocumentation.Model.Types
            .Select(MapType)
            .OrderBy(page => GetKindSortOrder(page.Kind))
            .ThenBy(page => page.Name, StringComparer.Ordinal)
            .ToArray();
        var hierarchy = rawPages.ToDictionary(page => page.FullName, page => page.BaseTypeFullName, StringComparer.Ordinal);
        var pages = rawPages
            .Select(page => page with {
                Fields = SortMembersByInheritance(page.Fields, page.FullName, hierarchy),
                Properties = SortMembersByInheritance(page.Properties, page.FullName, hierarchy),
                Methods = page.Methods
            })
            .ToArray();

        Types = pages;
        ComponentTypes = SelectPages(pages, _componentTypeFullNames);
        ReferenceTypes = pages
            .Where(page => !IsComponentType(page.FullName))
            .ToArray();
        FeaturedTypes = _featuredTypeFullNames
            .Select(fullName => pages.FirstOrDefault(page => string.Equals(page.FullName, fullName, StringComparison.Ordinal)))
            .OfType<DocumentationTypePage>()
            .ToArray();

        _componentTypesBySlug = new ReadOnlyDictionary<string, DocumentationTypePage>(
            ComponentTypes.ToDictionary(page => page.Slug, StringComparer.Ordinal));
        _referenceTypesBySlug = new ReadOnlyDictionary<string, DocumentationTypePage>(
            ReferenceTypes.ToDictionary(page => page.Slug, StringComparer.Ordinal));
        _typesBySlug = new ReadOnlyDictionary<string, DocumentationTypePage>(
            pages.ToDictionary(page => page.Slug, StringComparer.Ordinal));
        _typeHrefsByFullName = new ReadOnlyDictionary<string, string>(
            pages.ToDictionary(page => page.FullName, CreateTypeHref, StringComparer.Ordinal));
        _typeHrefsByName = new ReadOnlyDictionary<string, string>(
            pages
                .GroupBy(page => page.Name, StringComparer.Ordinal)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => CreateTypeHref(group.Single()), StringComparer.Ordinal));
    }

    public IReadOnlyList<DocumentationTypePage> ComponentTypes { get; }

    public IReadOnlyList<DocumentationTypePage> FeaturedTypes { get; }

    public IReadOnlyList<DocumentationTypePage> ReferenceTypes { get; }

    public IReadOnlyList<DocumentationTypePage> Types { get; }

    public bool TryGetComponentType(string slug, [NotNullWhen(true)] out DocumentationTypePage? typePage) {
        return _componentTypesBySlug.TryGetValue(slug, out typePage);
    }

    public bool TryGetReferenceType(string slug, [NotNullWhen(true)] out DocumentationTypePage? typePage) {
        return _referenceTypesBySlug.TryGetValue(slug, out typePage);
    }

    public bool TryGetType(string slug, [NotNullWhen(true)] out DocumentationTypePage? typePage) {
        return _typesBySlug.TryGetValue(slug, out typePage);
    }

    public bool TryGetTypeHref(string fullName, [NotNullWhen(true)] out string? href) {
        return _typeHrefsByFullName.TryGetValue(fullName, out href);
    }

    public bool TryGetTypeHrefByName(string name, [NotNullWhen(true)] out string? href) {
        return _typeHrefsByName.TryGetValue(name, out href);
    }

    private static DocumentationTypePage MapType(TypeDocumentation typeDocumentation) {
        var normalizedKind = NormalizeKind(typeDocumentation.Kind);
        var slug = CreateSlug(typeDocumentation.FullName);
        var content = XmlDocumentationContentParser.Parse(typeDocumentation.XmlDocumentation, typeDocumentation.Summary);

        return new DocumentationTypePage(
            typeDocumentation.Name,
            typeDocumentation.FullName,
            typeDocumentation.BaseTypeFullName,
            slug,
            normalizedKind,
            CreateTypeDeclaration(typeDocumentation, normalizedKind),
            content,
            typeDocumentation.Fields.Select(member => MapMember(member.Name, member.Signature, member.TypeDisplayName, member.TypeFullName, member.Summary, member.XmlDocumentation, member.DeclaringTypeFullName, member.IsFromBaseType))
                .OrderBy(member => member.Name, StringComparer.Ordinal)
                .ToArray(),
            typeDocumentation.Properties.Select(member => MapMember(member.Name, member.Signature, member.TypeDisplayName, member.TypeFullName, member.Summary, member.XmlDocumentation, member.DeclaringTypeFullName, member.IsFromBaseType))
                .OrderBy(member => member.Name, StringComparer.Ordinal)
                .ToArray(),
            typeDocumentation.Methods.Select(member => MapMember(member.Name, member.Signature, null, null, member.Summary, member.XmlDocumentation, member.DeclaringTypeFullName, member.IsFromBaseType))
                .OrderBy(member => member.Name, StringComparer.Ordinal)
                .ToArray(),
            _featuredTypeFullNames.Contains(typeDocumentation.FullName, StringComparer.Ordinal));
    }

    private static DocumentationMemberPage MapMember(string name, string signature, string? typeDisplayName, string? typeFullName, string summary, string xmlDocumentation, string declaringTypeFullName, bool isFromBaseType) {
        var content = XmlDocumentationContentParser.Parse(xmlDocumentation, summary);

        return new DocumentationMemberPage(
            name,
            CreateMemberAnchor(name),
            signature,
            typeDisplayName,
            typeFullName,
            declaringTypeFullName,
            isFromBaseType,
            content);
    }

    private static string CreateMemberAnchor(string name) => CreateSlug(name);

    private static string CreateSlug(string value) {
        var normalized = value.Replace('+', '.');
        normalized = Regex.Replace(normalized, "([a-z0-9])([A-Z])", "$1-$2");
        normalized = Regex.Replace(normalized, "([A-Z]+)([A-Z][a-z])", "$1-$2");
        normalized = normalized.Replace('.', '-').Replace('_', '-');
        normalized = Regex.Replace(normalized, "-{2,}", "-");
        return normalized.Trim('-').ToLowerInvariant();
    }

    private static string CreateTypeDeclaration(TypeDocumentation typeDocumentation, string normalizedKind) {
        return normalizedKind switch {
            "enum" => $"public enum {typeDocumentation.FullName}",
            "struct" => $"public struct {typeDocumentation.FullName}",
            _ => $"public class {typeDocumentation.FullName}"
        };
    }

    private static int GetKindSortOrder(string kind) {
        return kind switch {
            "enum" => 0,
            "class" => 1,
            "struct" => 2,
            _ => 9
        };
    }

    private static string CreateTypeHref(DocumentationTypePage page) {
        return IsComponentType(page.FullName)
            ? $"/docs/components/{page.Slug}"
            : $"/docs/reference/{page.Slug}";
    }

    private static IReadOnlyList<DocumentationMemberPage> SortMembersByInheritance(
        IReadOnlyList<DocumentationMemberPage> members,
        string ownerTypeFullName,
        IReadOnlyDictionary<string, string> hierarchy) {
        return members
            .OrderBy(member => GetInheritanceDistance(member, ownerTypeFullName, hierarchy))
            .ThenBy(member => member.Name, StringComparer.Ordinal)
            .ThenBy(member => member.Signature, StringComparer.Ordinal)
            .ToArray();
    }

    private static int GetInheritanceDistance(
        DocumentationMemberPage member,
        string ownerTypeFullName,
        IReadOnlyDictionary<string, string> hierarchy) {
        if (!member.IsFromBaseType || string.Equals(member.DeclaringTypeFullName, ownerTypeFullName, StringComparison.Ordinal)) {
            return 0;
        }

        var current = hierarchy.TryGetValue(ownerTypeFullName, out var baseTypeFullName) ? baseTypeFullName : string.Empty;
        var distance = 1;

        while (!string.IsNullOrWhiteSpace(current)) {
            if (string.Equals(current, member.DeclaringTypeFullName, StringComparison.Ordinal)) {
                return distance;
            }

            current = hierarchy.TryGetValue(current, out var nextBaseType) ? nextBaseType : string.Empty;
            distance++;
        }

        return int.MaxValue;
    }

    private static bool IsComponentType(string fullName) => _componentTypeFullNames.Contains(fullName, StringComparer.Ordinal);

    private static string NormalizeKind(string kind) => kind.Trim().ToLowerInvariant();

    private static IReadOnlyList<DocumentationTypePage> SelectPages(
        IReadOnlyList<DocumentationTypePage> pages,
        IReadOnlyList<string> fullNames) {
        return fullNames
            .Select(fullName => pages.FirstOrDefault(page => string.Equals(page.FullName, fullName, StringComparison.Ordinal)))
            .OfType<DocumentationTypePage>()
            .ToArray();
    }
}

public sealed record DocumentationTypePage(
    string Name,
    string FullName,
    string BaseTypeFullName,
    string Slug,
    string Kind,
    string Declaration,
    DocumentationContent Content,
    IReadOnlyList<DocumentationMemberPage> Fields,
    IReadOnlyList<DocumentationMemberPage> Properties,
    IReadOnlyList<DocumentationMemberPage> Methods,
    bool IsFeatured);

public sealed record DocumentationMemberPage(
    string Name,
    string AnchorId,
    string Signature,
    string? TypeDisplayName,
    string? TypeFullName,
    string DeclaringTypeFullName,
    bool IsFromBaseType,
    DocumentationContent Content);

public sealed record DocumentationContent(
    IReadOnlyList<string> SummaryParagraphs,
    IReadOnlyList<string> RemarksParagraphs,
    IReadOnlyList<DocumentationNamedContent> Parameters,
    string? ReturnsHtml,
    string? ValueHtml,
    IReadOnlyList<DocumentationNamedContent> Exceptions);

public sealed record DocumentationNamedContent(string Name, string Html);

public sealed record DocumentationOutlineItem(string FragmentId, string Label);

internal static class XmlDocumentationContentParser {

    public static DocumentationContent Parse(string xmlDocumentation, string summaryFallback) {
        if (string.IsNullOrWhiteSpace(xmlDocumentation)) {
            return new DocumentationContent(
                CreateFallbackParagraphs(summaryFallback),
                Array.Empty<string>(),
                Array.Empty<DocumentationNamedContent>(),
                null,
                null,
                Array.Empty<DocumentationNamedContent>());
        }

        try {
            var document = XDocument.Parse("<root>" + xmlDocumentation + "</root>", LoadOptions.PreserveWhitespace);
            var root = document.Root;
            if (root is null) {
                return new DocumentationContent(
                    CreateFallbackParagraphs(summaryFallback),
                    Array.Empty<string>(),
                    Array.Empty<DocumentationNamedContent>(),
                    null,
                    null,
                    Array.Empty<DocumentationNamedContent>());
            }

            var contentRoot = root.Element("member") ?? root;

            var summaryParagraphs = ParseParagraphs(contentRoot.Element("summary"), summaryFallback);
            var remarksParagraphs = ParseParagraphs(contentRoot.Element("remarks"), null);
            var parameters = contentRoot.Elements("param")
                .Select(param => new DocumentationNamedContent(
                    param.Attribute("name")?.Value ?? string.Empty,
                    FormatNodes(param.Nodes())))
                .Where(item => !string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(item.Html))
                .ToArray();
            var exceptions = contentRoot.Elements("exception")
                .Select(exception => new DocumentationNamedContent(
                    CleanCref(exception.Attribute("cref")?.Value) ?? "Exception",
                    FormatNodes(exception.Nodes())))
                .Where(item => !string.IsNullOrWhiteSpace(item.Html))
                .ToArray();

            return new DocumentationContent(
                summaryParagraphs,
                remarksParagraphs,
                parameters,
                FormatSingle(contentRoot.Element("returns")),
                FormatSingle(contentRoot.Element("value")),
                exceptions);
        }
        catch {
            return new DocumentationContent(
                CreateFallbackParagraphs(summaryFallback),
                Array.Empty<string>(),
                Array.Empty<DocumentationNamedContent>(),
                null,
                null,
                Array.Empty<DocumentationNamedContent>());
        }
    }

    private static IReadOnlyList<string> CreateFallbackParagraphs(string summaryFallback) {
        return string.IsNullOrWhiteSpace(summaryFallback)
            ? Array.Empty<string>()
            : [WebUtility.HtmlEncode(summaryFallback.Trim())];
    }

    private static string? FormatSingle(XElement? element) {
        if (element is null) {
            return null;
        }

        var formatted = FormatNodes(element.Nodes());
        return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
    }

    private static IReadOnlyList<string> ParseParagraphs(XElement? sectionElement, string? fallbackText) {
        if (sectionElement is null) {
            return string.IsNullOrWhiteSpace(fallbackText) ? Array.Empty<string>() : [WebUtility.HtmlEncode(fallbackText.Trim())];
        }

        var paragraphElements = sectionElement.Elements("para").ToArray();
        if (paragraphElements.Length > 0) {
            return paragraphElements
                .Select(para => FormatNodes(para.Nodes()))
                .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
                .ToArray();
        }

        var sectionText = FormatNodes(sectionElement.Nodes());
        if (!string.IsNullOrWhiteSpace(sectionText)) {
            return [sectionText];
        }

        return string.IsNullOrWhiteSpace(fallbackText) ? Array.Empty<string>() : [WebUtility.HtmlEncode(fallbackText.Trim())];
    }

    private static string FormatNodes(IEnumerable<XNode> nodes) {
        var builder = new StringBuilder();
        foreach (var node in nodes) {
            AppendNode(builder, node);
        }

        return NormalizeWhitespace(builder.ToString());
    }

    private static void AppendNode(StringBuilder builder, XNode node) {
        switch (node) {
            case XText textNode:
                builder.Append(WebUtility.HtmlEncode(textNode.Value));
                return;
            case XElement element:
                AppendElement(builder, element);
                return;
            default:
                builder.Append(WebUtility.HtmlEncode(node.ToString(SaveOptions.DisableFormatting)));
                return;
        }
    }

    private static void AppendElement(StringBuilder builder, XElement element) {
        switch (element.Name.LocalName) {
            case "c":
            case "code":
                builder.Append("<code>");
                builder.Append(FormatNodes(element.Nodes()));
                builder.Append("</code>");
                return;
            case "see":
                builder.Append("<code>");
                builder.Append(WebUtility.HtmlEncode(CleanCref(element.Attribute("cref")?.Value) ?? element.Attribute("langword")?.Value ?? element.Value));
                builder.Append("</code>");
                return;
            case "paramref":
            case "typeparamref":
                builder.Append("<code>");
                builder.Append(WebUtility.HtmlEncode(element.Attribute("name")?.Value ?? element.Value));
                builder.Append("</code>");
                return;
            case "br":
                builder.Append("<br />");
                return;
            case "para":
                builder.Append(FormatNodes(element.Nodes()));
                return;
            default:
                builder.Append(FormatNodes(element.Nodes()));
                return;
        }
    }

    private static string NormalizeWhitespace(string value) {
        var normalized = value.Replace("\r", " ").Replace("\n", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        normalized = Regex.Replace(normalized, @"\s+([.,;:!?])", "$1");
        normalized = Regex.Replace(normalized, @"(<br\s*/?>)\s+", "$1");
        return normalized;
    }

    private static string? CleanCref(string? cref) {
        if (string.IsNullOrWhiteSpace(cref)) {
            return null;
        }

        var value = cref.Trim();
        var separatorIndex = value.IndexOf(':');
        if (separatorIndex >= 0 && separatorIndex < value.Length - 1) {
            value = value[(separatorIndex + 1)..];
        }

        value = value.Replace('{', '<').Replace('}', '>');
        return value;
    }
}

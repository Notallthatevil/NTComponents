using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace NTComponents;

/// <summary>
///     Rich text editor that stores Markdown while editing rendered output.
/// </summary>
public partial class NTRichTextEditor : ITnTPageScriptComponent<NTRichTextEditor> {

    /// <summary>
    ///     Gets the .NET object reference used for JavaScript interop.
    /// </summary>
    public DotNetObjectReference<NTRichTextEditor>? DotNetObjectRef { get; private set; }

    /// <summary>
    ///     Gets the isolated JavaScript module used by the component.
    /// </summary>
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <inheritdoc />
    public string? JsModulePath => "./_content/NTComponents/Editors/NTRichTextEditor.razor.js";

    /// <inheritdoc />
    public override InputType Type => InputType.TextArea;

    /// <summary>
    ///     Gets the JavaScript runtime used to import and interact with the editor module.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    /// <summary>
    ///     Gets the page script fragment that ensures the editor module is available during SSR rendering.
    /// </summary>
    protected RenderFragment PageScript => builder => {
        builder.OpenComponent<TnTPageScript>(0);
        builder.AddAttribute(1, nameof(TnTPageScript.Src), JsModulePath);
        builder.CloseComponent();
    };

    private string _renderedHtml = string.Empty;
    private string? _lastMarkdownFromJs;

    private string AriaLabel => Label ?? Placeholder ?? FieldIdentifier.FieldName;

    private string ContentEditableAttribute => (!FieldDisabled && !FieldReadonly).ToString().ToLowerInvariant();

    private IReadOnlyDictionary<string, object>? EditorAttributes => AdditionalAttributes?
        .Where(kvp => kvp.Key is not ("autocomplete" or "autofocus" or "class" or "disabled" or "id" or "lang" or "maxlength" or "minlength" or "name" or "placeholder" or "readonly" or "required" or "style" or "title" or "type" or "value"))
        .ToDictionary();

    private string EditorEmptyAttribute => string.IsNullOrWhiteSpace(CurrentValueAsString).ToString().ToLowerInvariant();

    private string EditorLengthText => $"{CurrentValueAsString?.Length ?? 0}/{GetMaxLength()}";

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTRichTextEditor" /> class.
    /// </summary>
    public NTRichTextEditor() => DotNetObjectRef = DotNetObjectReference.Create(this);

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override async ValueTask SetFocusAsync() {
        if (IsolatedJsModule is null) {
            await base.SetFocusAsync();
            return;
        }

        try {
            await IsolatedJsModule.InvokeVoidAsync("focusEditor", Element);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore.
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        try {
            if (firstRender) {
                IsolatedJsModule = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                await (IsolatedJsModule?.InvokeVoidAsync("onLoad", Element, DotNetObjectRef) ?? ValueTask.CompletedTask);
            }

            await (IsolatedJsModule?.InvokeVoidAsync("onUpdate", Element, DotNetObjectRef) ?? ValueTask.CompletedTask);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during render.
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        var currentMarkdown = CurrentValueAsString ?? string.Empty;
        if (!string.Equals(currentMarkdown, _lastMarkdownFromJs, StringComparison.Ordinal)) {
            _renderedHtml = RenderMarkdown(currentMarkdown);
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    /// <summary>
    ///     Synchronizes the editor value from JavaScript while the user is typing.
    /// </summary>
    /// <param name="value">The current Markdown value.</param>
    [JSInvokable]
    public async Task UpdateValueFromJs(string value) {
        _lastMarkdownFromJs = value;
        CurrentValueAsString = value;

        if (BindOnInput) {
            await BindAfter.InvokeAsync(CurrentValue);
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    ///     Commits the editor value and notifies the form that the field changed.
    /// </summary>
    /// <param name="value">The current Markdown value.</param>
    [JSInvokable]
    public async Task CommitValueFromJs(string value) {
        var previousValue = CurrentValueAsString ?? string.Empty;

        _lastMarkdownFromJs = value;
        CurrentValueAsString = value;
        EditContext?.NotifyFieldChanged(FieldIdentifier);

        if (!BindOnInput || !string.Equals(previousValue, value, StringComparison.Ordinal)) {
            await BindAfter.InvokeAsync(CurrentValue);
        }

        await OnBlurCallback.InvokeAsync(new FocusEventArgs());
        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///     Releases the JavaScript module asynchronously.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore() {
        if (IsolatedJsModule is not null) {
            try {
                await IsolatedJsModule.InvokeVoidAsync("onDispose", Element, DotNetObjectRef);
                await IsolatedJsModule.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during disposal.
            }

            IsolatedJsModule = null;
        }

        DotNetObjectRef?.Dispose();
        DotNetObjectRef = null;
    }

    internal static string RenderMarkdown(string? markdown) {
        if (string.IsNullOrWhiteSpace(markdown)) {
            return string.Empty;
        }

        var lines = NormalizeNewLines(markdown).Split('\n');
        var index = 0;
        var builder = new StringBuilder();

        while (index < lines.Length) {
            if (string.IsNullOrWhiteSpace(lines[index])) {
                index++;
                continue;
            }

            if (TryRenderAlignmentBlock(lines, ref index, builder)) {
                continue;
            }

            if (TryRenderIframeBlock(lines[index], builder)) {
                index++;
                continue;
            }

            if (TryRenderCodeBlock(lines, ref index, builder)) {
                continue;
            }

            if (TryRenderBlockQuote(lines, ref index, builder)) {
                continue;
            }

            if (TryRenderList(lines, ref index, builder)) {
                continue;
            }

            if (TryRenderTable(lines, ref index, builder)) {
                continue;
            }

            if (TryRenderHeading(lines[index], builder)) {
                index++;
                continue;
            }

            RenderParagraph(lines, ref index, builder);
        }

        return builder.ToString();
    }

    private static void RenderParagraph(IReadOnlyList<string> lines, ref int index, StringBuilder builder) {
        var paragraphLines = new List<string>();

        while (index < lines.Count) {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line)) {
                break;
            }

            if (paragraphLines.Count > 0 && IsBlockBoundary(lines, index)) {
                break;
            }

            paragraphLines.Add(line.TrimEnd());
            index++;
        }

        if (paragraphLines.Count == 0) {
            return;
        }

        builder.Append("<p>");
        builder.Append(RenderInlineMarkdown(string.Join("\n", paragraphLines)).Replace("\n", "<br />", StringComparison.Ordinal));
        builder.Append("</p>");
    }

    private static bool TryRenderHeading(string block, StringBuilder builder) {
        var headingMatch = HeadingRegex().Match(block.Trim());
        if (!headingMatch.Success) {
            return false;
        }

        var level = headingMatch.Groups["level"].Value.Length;
        if (level is < 1 or > 6) {
            return false;
        }

        builder.Append("<h");
        builder.Append(level);
        builder.Append('>');
        builder.Append(RenderInlineMarkdown(headingMatch.Groups["content"].Value.Trim()));
        builder.Append("</h");
        builder.Append(level);
        builder.Append('>');
        return true;
    }

    private static bool TryRenderAlignmentBlock(IReadOnlyList<string> lines, ref int index, StringBuilder builder) {
        var alignmentMatch = AlignmentOpenRegex().Match(lines[index].Trim());
        if (!alignmentMatch.Success) {
            return false;
        }

        var alignment = alignmentMatch.Groups["alignment"].Value.ToLowerInvariant();
        index++;

        var innerLines = new List<string>();
        while (index < lines.Count && !AlignmentCloseRegex().IsMatch(lines[index].Trim())) {
            innerLines.Add(lines[index]);
            index++;
        }

        if (index < lines.Count) {
            index++;
        }

        builder.Append("<div class=\"tnt-rich-text-editor-alignment\" style=\"text-align:");
        builder.Append(WebUtility.HtmlEncode(alignment));
        builder.Append(";\">");
        builder.Append(RenderMarkdown(string.Join("\n", innerLines)));
        builder.Append("</div>");
        return true;
    }

    private static bool TryRenderIframeBlock(string line, StringBuilder builder) {
        var iframeMatch = IframeRegex().Match(line.Trim());
        if (!iframeMatch.Success) {
            return false;
        }

        var src = WebUtility.HtmlEncode(iframeMatch.Groups["src"].Value);
        var title = WebUtility.HtmlEncode(iframeMatch.Groups["title"].Value);
        var width = WebUtility.HtmlEncode(iframeMatch.Groups["width"].Value);
        var height = WebUtility.HtmlEncode(iframeMatch.Groups["height"].Value);

        builder.Append("<iframe src=\"");
        builder.Append(src);
        builder.Append("\" title=\"");
        builder.Append(title);
        builder.Append("\" width=\"");
        builder.Append(width);
        builder.Append("\" height=\"");
        builder.Append(height);
        builder.Append("\" loading=\"lazy\"></iframe>");
        return true;
    }

    private static bool TryRenderTable(IReadOnlyList<string> lines, ref int index, StringBuilder builder) {
        if (!IsTableStart(lines, index)) {
            return false;
        }

        var headerCells = SplitTableRow(lines[index].Trim());
        var alignments = ParseTableSeparator(lines[index + 1].Trim());
        index += 2;

        builder.Append("<table><thead><tr>");
        for (var cellIndex = 0; cellIndex < headerCells.Count; cellIndex++) {
            AppendTableCell(builder, "th", headerCells[cellIndex], alignments[cellIndex]);
        }

        builder.Append("</tr></thead>");

        var bodyRows = new List<IReadOnlyList<string>>();
        while (index < lines.Count) {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line)) {
                break;
            }

            var trimmed = line.Trim();
            if (IsBlockBoundary(lines, index) && !LooksLikeTableRow(trimmed)) {
                break;
            }

            var rowCells = SplitTableRow(trimmed);
            if (rowCells.Count == 0) {
                break;
            }

            bodyRows.Add(NormalizeTableCells(rowCells, headerCells.Count));
            index++;
        }

        if (bodyRows.Count > 0) {
            builder.Append("<tbody>");
            foreach (var row in bodyRows) {
                builder.Append("<tr>");
                for (var cellIndex = 0; cellIndex < headerCells.Count; cellIndex++) {
                    AppendTableCell(builder, "td", row[cellIndex], alignments[cellIndex]);
                }

                builder.Append("</tr>");
            }

            builder.Append("</tbody>");
        }

        builder.Append("</table>");
        return true;
    }

    private static void AppendTableCell(StringBuilder builder, string tagName, string content, string alignment) {
        builder.Append('<');
        builder.Append(tagName);
        if (!string.IsNullOrWhiteSpace(alignment)) {
            builder.Append(" style=\"text-align:");
            builder.Append(WebUtility.HtmlEncode(alignment));
            builder.Append(";\"");
        }

        builder.Append('>');
        builder.Append(RenderInlineMarkdown(content.Trim()));
        builder.Append("</");
        builder.Append(tagName);
        builder.Append('>');
    }

    private static bool IsTableStart(IReadOnlyList<string> lines, int index) {
        if (index + 1 >= lines.Count) {
            return false;
        }

        var headerLine = lines[index].Trim();
        var separatorLine = lines[index + 1].Trim();
        if (!LooksLikeTableRow(headerLine)) {
            return false;
        }

        var headerCells = SplitTableRow(headerLine);
        var separatorCells = ParseTableSeparator(separatorLine);
        return headerCells.Count > 0 && separatorCells.Count == headerCells.Count;
    }

    private static bool LooksLikeTableRow(string line) => line.Contains('|');

    private static IReadOnlyList<string> SplitTableRow(string line) {
        if (!LooksLikeTableRow(line)) {
            return Array.Empty<string>();
        }

        var normalized = line.Trim();
        if (normalized.StartsWith("|", StringComparison.Ordinal)) {
            normalized = normalized[1..];
        }

        if (normalized.EndsWith("|", StringComparison.Ordinal)) {
            normalized = normalized[..^1];
        }

        return normalized
            .Split('|')
            .Select(cell => cell.Trim())
            .ToArray();
    }

    private static IReadOnlyList<string> NormalizeTableCells(IReadOnlyList<string> cells, int targetCount) {
        var normalized = cells.Take(targetCount).ToList();
        while (normalized.Count < targetCount) {
            normalized.Add(string.Empty);
        }

        return normalized;
    }

    private static IReadOnlyList<string> ParseTableSeparator(string line) {
        var cells = SplitTableRow(line);
        if (cells.Count == 0) {
            return Array.Empty<string>();
        }

        var alignments = new List<string>(cells.Count);
        foreach (var cell in cells) {
            var trimmed = cell.Trim();
            if (trimmed.Length < 3) {
                return Array.Empty<string>();
            }

            var leftAligned = trimmed.StartsWith(':');
            var rightAligned = trimmed.EndsWith(':');
            var dashSection = trimmed.Trim(':');
            if (dashSection.Length < 3 || dashSection.Any(character => character != '-')) {
                return Array.Empty<string>();
            }

            alignments.Add(leftAligned && rightAligned ? "center"
                : rightAligned ? "right"
                : leftAligned ? "left"
                : string.Empty);
        }

        return alignments;
    }

    private static bool TryRenderCodeBlock(IReadOnlyList<string> lines, ref int index, StringBuilder builder) {
        var line = lines[index].Trim();
        if (!line.StartsWith("```", StringComparison.Ordinal)) {
            return false;
        }

        var language = line[3..].Trim();
        var codeLines = new List<string>();
        index++;

        while (index < lines.Count && !lines[index].Trim().StartsWith("```", StringComparison.Ordinal)) {
            codeLines.Add(lines[index]);
            index++;
        }

        if (index < lines.Count) {
            index++;
        }

        var encodedCode = WebUtility.HtmlEncode(string.Join("\n", codeLines));
        var encodedLanguage = WebUtility.HtmlEncode(language);

        builder.Append("<pre");
        if (!string.IsNullOrWhiteSpace(language)) {
            builder.Append(" data-language=\"");
            builder.Append(encodedLanguage);
            builder.Append('"');
        }

        builder.Append("><code");
        if (!string.IsNullOrWhiteSpace(language)) {
            builder.Append(" data-language=\"");
            builder.Append(encodedLanguage);
            builder.Append('"');
        }

        builder.Append('>');
        builder.Append(encodedCode);
        builder.Append("</code></pre>");
        return true;
    }

    private static bool TryRenderBlockQuote(IReadOnlyList<string> lines, ref int index, StringBuilder builder) {
        if (!IsBlockQuoteLine(lines[index])) {
            return false;
        }

        var quoteLines = new List<string>();
        while (index < lines.Count) {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line)) {
                if (index + 1 < lines.Count && IsBlockQuoteLine(lines[index + 1])) {
                    quoteLines.Add(string.Empty);
                    index++;
                    continue;
                }

                break;
            }

            if (!IsBlockQuoteLine(line)) {
                break;
            }

            quoteLines.Add(StripBlockQuoteMarker(line));
            index++;
        }

        builder.Append("<blockquote>");
        builder.Append(RenderMarkdown(string.Join("\n", quoteLines)));
        builder.Append("</blockquote>");
        return true;
    }

    private static bool TryRenderList(IReadOnlyList<string> lines, ref int index, StringBuilder builder) {
        if (!TryGetListMarker(lines[index], out var indent, out var ordered, out _)) {
            return false;
        }

        RenderList(lines, ref index, builder, indent, ordered);
        return true;
    }

    private static void RenderList(IReadOnlyList<string> lines, ref int index, StringBuilder builder, int indent, bool ordered) {
        builder.Append(ordered ? "<ol>" : "<ul>");

        while (index < lines.Count) {
            if (string.IsNullOrWhiteSpace(lines[index])) {
                index++;
                continue;
            }

            if (!TryGetListMarker(lines[index], out var lineIndent, out var lineOrdered, out var lineContent) || lineIndent != indent || lineOrdered != ordered) {
                break;
            }

            index++;
            builder.Append("<li>");

            var inlineLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(lineContent)) {
                inlineLines.Add(lineContent.TrimEnd());
            }

            while (index < lines.Count) {
                if (string.IsNullOrWhiteSpace(lines[index])) {
                    if (HasListContinuation(lines, index, indent)) {
                        index++;
                        continue;
                    }

                    break;
                }

                if (TryGetListMarker(lines[index], out var nestedIndent, out var nestedOrdered, out _)) {
                    if (nestedIndent == indent) {
                        break;
                    }

                    if (nestedIndent > indent) {
                        if (inlineLines.Count > 0) {
                            builder.Append(RenderInlineMarkdown(string.Join("\n", inlineLines)).Replace("\n", "<br />", StringComparison.Ordinal));
                            inlineLines.Clear();
                        }

                        RenderList(lines, ref index, builder, nestedIndent, nestedOrdered);
                        continue;
                    }
                }

                var continuationIndent = CountLeadingSpaces(lines[index]);
                if (continuationIndent <= indent) {
                    break;
                }

                var continuationText = StripIndent(lines[index], Math.Min(lines[index].Length, indent + 2)).TrimEnd();
                if (continuationText.Length > 0) {
                    inlineLines.Add(continuationText);
                }

                index++;
            }

            if (inlineLines.Count > 0) {
                builder.Append(RenderInlineMarkdown(string.Join("\n", inlineLines)).Replace("\n", "<br />", StringComparison.Ordinal));
            }

            builder.Append("</li>");
        }

        builder.Append(ordered ? "</ol>" : "</ul>");
    }

    private static bool HasListContinuation(IReadOnlyList<string> lines, int blankLineIndex, int currentIndent) {
        for (var lookahead = blankLineIndex + 1; lookahead < lines.Count; lookahead++) {
            if (string.IsNullOrWhiteSpace(lines[lookahead])) {
                continue;
            }

            if (TryGetListMarker(lines[lookahead], out var indent, out _, out _)) {
                return indent >= currentIndent;
            }

            return CountLeadingSpaces(lines[lookahead]) > currentIndent;
        }

        return false;
    }

    private static bool IsBlockBoundary(IReadOnlyList<string> lines, int index) => lines[index].TrimStart() switch {
        var trimmed when AlignmentOpenRegex().IsMatch(trimmed) => true,
        var trimmed when IframeRegex().IsMatch(trimmed) => true,
        var trimmed when trimmed.StartsWith("```", StringComparison.Ordinal) => true,
        var trimmed when trimmed.StartsWith(">", StringComparison.Ordinal) => true,
        var trimmed when HeadingRegex().IsMatch(trimmed) => true,
        var trimmed when IsTableStart(lines, index) => true,
        _ => TryGetListMarker(lines[index], out _, out _, out _)
    };

    private static bool IsBlockQuoteLine(string line) => line.TrimStart().StartsWith(">", StringComparison.Ordinal);

    private static string StripBlockQuoteMarker(string line) {
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith(">", StringComparison.Ordinal)) {
            return trimmed;
        }

        return trimmed.Length > 1 && trimmed[1] == ' ' ? trimmed[2..] : trimmed[1..];
    }

    private static bool TryGetListMarker(string line, out int indent, out bool ordered, out string content) {
        indent = CountLeadingSpaces(line);
        ordered = false;
        content = string.Empty;

        var trimmed = StripIndent(line, indent);
        if (trimmed.Length >= 2 && (trimmed[0] is '-' or '*' or '+') && trimmed[1] == ' ') {
            content = trimmed[2..];
            return true;
        }

        var orderedMatch = OrderedListRegex().Match(trimmed);
        if (!orderedMatch.Success) {
            return false;
        }

        ordered = true;
        content = orderedMatch.Groups["content"].Value;
        return true;
    }

    private static string StripIndent(string value, int count) => count >= value.Length ? string.Empty : value[count..];

    private static int CountLeadingSpaces(string value) {
        var count = 0;
        while (count < value.Length && value[count] == ' ') {
            count++;
        }

        return count;
    }

    private static string NormalizeNewLines(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string RenderInlineMarkdown(string value) {
        var tokens = new Dictionary<string, string>();
        var tokenIndex = 0;

        var replaced = ImageRegex().Replace(value, match => CreateInlineToken(tokens, ref tokenIndex, RenderImage(match)));
        replaced = LinkRegex().Replace(replaced, match => CreateInlineToken(tokens, ref tokenIndex, RenderLink(match)));
        replaced = TextColorRegex().Replace(replaced, match => CreateInlineToken(tokens, ref tokenIndex, RenderTextColor(match)));
        replaced = UnderlineRegex().Replace(replaced, match => CreateInlineToken(tokens, ref tokenIndex, $"<u>{RenderInlineMarkdown(match.Groups["content"].Value)}</u>"));

        replaced = WebUtility.HtmlEncode(replaced);
        replaced = BoldRegex().Replace(replaced, "<strong>${content}</strong>");
        replaced = ItalicRegex().Replace(replaced, "<em>${content}</em>");
        replaced = StrikethroughRegex().Replace(replaced, "<s>${content}</s>");

        foreach (var (token, html) in tokens) {
            replaced = replaced.Replace(token, html, StringComparison.Ordinal);
        }

        return replaced;
    }

    private static string CreateInlineToken(IDictionary<string, string> tokens, ref int tokenIndex, string html) {
        var token = $"__NT_INLINE_TOKEN_{tokenIndex}__";
        tokens[token] = html;
        tokenIndex++;
        return token;
    }

    private static string RenderImage(Match match) {
        var alt = WebUtility.HtmlEncode(match.Groups["alt"].Value);
        var url = WebUtility.HtmlEncode(match.Groups["url"].Value);
        return $"<img src=\"{url}\" alt=\"{alt}\" />";
    }

    private static string RenderLink(Match match) {
        var text = RenderInlineMarkdown(match.Groups["text"].Value);
        var url = WebUtility.HtmlEncode(match.Groups["url"].Value);
        return $"<a href=\"{url}\">{text}</a>";
    }

    private static string RenderTextColor(Match match) {
        var color = WebUtility.HtmlEncode(match.Groups["color"].Value.Trim());
        var content = RenderInlineMarkdown(match.Groups["content"].Value);
        return $"<span style=\"color:{color};\">{content}</span>";
    }

    [GeneratedRegex(@"\*\*(?<content>.+?)\*\*", RegexOptions.Singleline)]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"(?<!\*)\*(?<content>[^*\r\n]+?)\*(?!\*)", RegexOptions.Singleline)]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"~~(?<content>.+?)~~", RegexOptions.Singleline)]
    private static partial Regex StrikethroughRegex();

    [GeneratedRegex(@"<u>(?<content>.+?)</u>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex UnderlineRegex();

    [GeneratedRegex(@"<span\s+style=""color:\s*(?<color>[^"";>]+)\s*;?"">\s*(?<content>.+?)\s*</span>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TextColorRegex();

    [GeneratedRegex(@"!\[(?<alt>[^\]]*)\]\((?<url>[^)\s]+)\)", RegexOptions.Singleline)]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"(?<!!)\[(?<text>[^\]]+)\]\((?<url>[^)\s]+)\)", RegexOptions.Singleline)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"^(?<level>#{1,6})\s+(?<content>.+)$", RegexOptions.Singleline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^(?<number>\d+)\.\s+(?<content>.*)$", RegexOptions.Singleline)]
    private static partial Regex OrderedListRegex();

    [GeneratedRegex(@"^<div\s+align=""(?<alignment>left|center|right|justify)"">\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex AlignmentOpenRegex();

    [GeneratedRegex(@"^</div>\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex AlignmentCloseRegex();

    [GeneratedRegex(@"^<iframe\s+src=""(?<src>[^""]+)""\s+title=""(?<title>[^""]*)""\s+width=""(?<width>[^""]+)""\s+height=""(?<height>[^""]+)""(?:\s+loading=""lazy"")?\s*>\s*</iframe>\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex IframeRegex();
}

const editorState = new WeakMap();
const blockNodeTags = new Set(['DIV', 'P', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'UL', 'OL', 'BLOCKQUOTE', 'PRE', 'TABLE', 'IFRAME']);
const blockSelector = 'h1, h2, h3, h4, h5, h6, pre, blockquote, table, th, td, iframe, li, p, div';
let selectionChangeRegistered = false;

function getSurface(element) {
    return element?.querySelector?.('.tnt-rich-text-editor-surface') ?? null;
}

function getSourceValueElement(element) {
    return element?.querySelector?.('.tnt-rich-text-editor-value') ?? null;
}

function getHiddenInput(element) {
    return element?.querySelector?.('.tnt-rich-text-editor-hidden-input') ?? null;
}

function getLengthElement(element) {
    return element?.closest?.('.tnt-input-container')?.querySelector?.('.tnt-input-length') ?? null;
}

function getToolbarButtons(element) {
    return Array.from(element?.querySelectorAll?.('.tnt-rich-text-editor-toolbar-button') ?? []);
}

function getSelectionElement(surface) {
    if (!surface) {
        return null;
    }

    const selection = window.getSelection?.();
    let node = selection?.anchorNode ?? surface;
    if (node?.nodeType === Node.TEXT_NODE) {
        node = node.parentElement;
    }

    if (!(node instanceof Element) || !surface.contains(node)) {
        return null;
    }

    return node;
}

function getCurrentBlockElement(surface) {
    const selectionElement = getSelectionElement(surface);
    if (selectionElement) {
        return selectionElement.closest(blockSelector) ?? selectionElement;
    }

    return surface?.querySelector?.(blockSelector) ?? null;
}

function getCurrentAlignment(surface, blockElement) {
    const selectionElement = getSelectionElement(surface) ?? blockElement;
    let node = selectionElement;

    while (node instanceof Element && surface?.contains(node)) {
        const alignment = getElementAlignment(node);
        if (alignment) {
            return alignment;
        }

        node = node.parentElement;
    }

    return getElementAlignment(blockElement) || 'left';
}

function hasAncestorTag(surface, tagNames) {
    const selectionElement = getSelectionElement(surface);
    return Boolean(selectionElement?.closest?.(tagNames));
}

function getElementTextColor(element) {
    if (!(element instanceof Element)) {
        return '';
    }

    const styleColor = element.style?.color?.trim?.() ?? '';
    if (styleColor) {
        return styleColor;
    }

    const styleAttribute = element.getAttribute?.('style') ?? '';
    const match = styleAttribute.match(/color:\s*([^;]+)/i);
    return match?.[1]?.trim?.() ?? '';
}

function normalizeNewLines(value) {
    return (value ?? '').replace(/\r\n/g, '\n').replace(/\r/g, '\n');
}

function countLeadingSpaces(value) {
    let count = 0;
    while (count < value.length && value[count] === ' ') {
        count++;
    }

    return count;
}

function stripIndent(value, count) {
    return count >= value.length ? '' : value.slice(count);
}

function escapeHtml(value) {
    return value
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll('\'', '&#39;');
}

function createInlineToken(tokens, html) {
    const token = `__NT_INLINE_TOKEN_${tokens.length}__`;
    tokens.push({ token, html });
    return token;
}

function renderImage(match, alt, url) {
    return `<img src="${escapeHtml(url)}" alt="${escapeHtml(alt)}" />`;
}

function renderLink(match, text, url) {
    return `<a href="${escapeHtml(url)}">${renderInlineMarkdown(text)}</a>`;
}

function renderTextColor(match, color, content) {
    return `<span style="color:${escapeHtml(color.trim())};">${renderInlineMarkdown(content)}</span>`;
}

function renderInlineMarkdown(value) {
    const tokens = [];
    let rendered = value.replace(/!\[([^\]]*)\]\(([^)\s]+)\)/g, (match, alt, url) => createInlineToken(tokens, renderImage(match, alt, url)));
    rendered = rendered.replace(/(?<!!)\[([^\]]+)\]\(([^)\s]+)\)/g, (match, text, url) => createInlineToken(tokens, renderLink(match, text, url)));
    rendered = rendered.replace(/<span\s+style="color:\s*([^";>]+)\s*;?">\s*([\s\S]+?)\s*<\/span>/gi, (match, color, content) => createInlineToken(tokens, renderTextColor(match, color, content)));
    rendered = rendered.replace(/<u>([\s\S]+?)<\/u>/gi, (_, content) => createInlineToken(tokens, `<u>${renderInlineMarkdown(content)}</u>`));

    rendered = escapeHtml(rendered);
    rendered = rendered.replace(/\*\*(.+?)\*\*/gs, '<strong>$1</strong>');
    rendered = rendered.replace(/(^|[^*])\*([^*\r\n]+?)\*(?!\*)/gs, (_, prefix, content) => `${prefix}<em>${content}</em>`);
    rendered = rendered.replace(/~~(.+?)~~/gs, '<s>$1</s>');

    for (const token of tokens) {
        rendered = rendered.replaceAll(token.token, token.html);
    }

    return rendered;
}

function isCodeFenceLine(line) {
    return line.trimStart().startsWith('```');
}

function isBlockQuoteLine(line) {
    return line.trimStart().startsWith('>');
}

function stripBlockQuoteMarker(line) {
    const trimmed = line.trimStart();
    if (!trimmed.startsWith('>')) {
        return trimmed;
    }

    return trimmed.length > 1 && trimmed[1] === ' ' ? trimmed.slice(2) : trimmed.slice(1);
}

function parseListMarker(line) {
    const indent = countLeadingSpaces(line);
    const trimmed = stripIndent(line, indent);

    if (trimmed.length >= 2 && ['-', '*', '+'].includes(trimmed[0]) && trimmed[1] === ' ') {
        return {
            indent,
            ordered: false,
            content: trimmed.slice(2)
        };
    }

    const orderedMatch = trimmed.match(/^\d+\.\s+(.*)$/s);
    if (!orderedMatch) {
        return null;
    }

    return {
        indent,
        ordered: true,
        content: orderedMatch[1]
    };
}

function parseAlignmentMarker(line) {
    const match = line.trim().match(/^<div\s+align="(left|center|right|justify)">$/i);
    if (!match) {
        return null;
    }

    return match[1].toLowerCase();
}

function parseIframeMarker(line) {
    const match = line.trim().match(/^<iframe\s+src="([^"]+)"\s+title="([^"]*)"\s+width="([^"]+)"\s+height="([^"]+)"(?:\s+loading="lazy")?\s*>\s*<\/iframe>$/i);
    if (!match) {
        return null;
    }

    return {
        src: match[1],
        title: match[2],
        width: match[3],
        height: match[4]
    };
}

function splitTableRow(line) {
    if (!line.includes('|')) {
        return [];
    }

    let normalized = line.trim();
    if (normalized.startsWith('|')) {
        normalized = normalized.slice(1);
    }

    if (normalized.endsWith('|')) {
        normalized = normalized.slice(0, -1);
    }

    return normalized.split('|').map((cell) => cell.trim());
}

function parseTableSeparator(line) {
    const cells = splitTableRow(line);
    if (cells.length === 0) {
        return null;
    }

    const alignments = [];
    for (const cell of cells) {
        const trimmed = cell.trim();
        if (trimmed.length < 3) {
            return null;
        }

        const leftAligned = trimmed.startsWith(':');
        const rightAligned = trimmed.endsWith(':');
        const dashSection = trimmed.replace(/^:/, '').replace(/:$/, '');
        if (dashSection.length < 3 || !/^-+$/.test(dashSection)) {
            return null;
        }

        alignments.push(leftAligned && rightAligned ? 'center'
            : rightAligned ? 'right'
            : leftAligned ? 'left'
            : '');
    }

    return alignments;
}

function isTableStart(lines, index) {
    if (index + 1 >= lines.length) {
        return false;
    }

    const headerCells = splitTableRow(lines[index]);
    const separatorCells = parseTableSeparator(lines[index + 1]);
    return headerCells.length > 0 && separatorCells !== null && separatorCells.length === headerCells.length;
}

function isBlockBoundary(lines, index) {
    const trimmed = lines[index].trimStart();
    return parseAlignmentMarker(trimmed) !== null
        || parseIframeMarker(trimmed) !== null
        || isCodeFenceLine(trimmed)
        || isBlockQuoteLine(trimmed)
        || isTableStart(lines, index)
        || /^(#{1,6})\s+(.+)$/s.test(trimmed)
        || parseListMarker(lines[index]) !== null;
}

function hasListContinuation(lines, blankLineIndex, currentIndent) {
    for (let lookahead = blankLineIndex + 1; lookahead < lines.length; lookahead++) {
        if (!lines[lookahead].trim()) {
            continue;
        }

        const listMarker = parseListMarker(lines[lookahead]);
        if (listMarker) {
            return listMarker.indent >= currentIndent;
        }

        return countLeadingSpaces(lines[lookahead]) > currentIndent;
    }

    return false;
}

function renderParagraph(lines, startIndex) {
    const paragraphLines = [];
    let index = startIndex;

    while (index < lines.length) {
        const line = lines[index];
        if (!line.trim()) {
            break;
        }

        if (paragraphLines.length > 0 && isBlockBoundary(lines, index)) {
            break;
        }

        paragraphLines.push(line.trimEnd());
        index++;
    }

    if (paragraphLines.length === 0) {
        return { html: '', nextIndex: index };
    }

    return {
        html: `<p>${renderInlineMarkdown(paragraphLines.join('\n')).replace(/\n/g, '<br />')}</p>`,
        nextIndex: index
    };
}

function renderCodeBlock(lines, startIndex) {
    const firstLine = lines[startIndex].trim();
    const language = firstLine.slice(3).trim();
    const codeLines = [];
    let index = startIndex + 1;

    while (index < lines.length && !lines[index].trimStart().startsWith('```')) {
        codeLines.push(lines[index]);
        index++;
    }

    if (index < lines.length) {
        index++;
    }

    const encodedLanguage = escapeHtml(language);
    const encodedCode = escapeHtml(codeLines.join('\n'));
    const languageAttribute = language ? ` data-language="${encodedLanguage}"` : '';

    return {
        html: `<pre${languageAttribute}><code${languageAttribute}>${encodedCode}</code></pre>`,
        nextIndex: index
    };
}

function renderAlignmentBlock(lines, startIndex) {
    const alignment = parseAlignmentMarker(lines[startIndex]);
    const innerLines = [];
    let index = startIndex + 1;

    while (index < lines.length && !/^<\/div>\s*$/i.test(lines[index].trim())) {
        innerLines.push(lines[index]);
        index++;
    }

    if (index < lines.length) {
        index++;
    }

    return {
        html: `<div class="tnt-rich-text-editor-alignment" style="text-align:${escapeHtml(alignment)};">${renderBlocksFromLines(innerLines).html}</div>`,
        nextIndex: index
    };
}

function renderIframeBlock(lines, startIndex) {
    const iframe = parseIframeMarker(lines[startIndex]);
    return {
        html: `<iframe src="${escapeHtml(iframe.src)}" title="${escapeHtml(iframe.title)}" width="${escapeHtml(iframe.width)}" height="${escapeHtml(iframe.height)}" loading="lazy"></iframe>`,
        nextIndex: startIndex + 1
    };
}

function normalizeTableCells(cells, targetCount) {
    const normalized = cells.slice(0, targetCount);
    while (normalized.length < targetCount) {
        normalized.push('');
    }

    return normalized;
}

function renderTable(lines, startIndex) {
    const headerCells = splitTableRow(lines[startIndex]);
    const alignments = parseTableSeparator(lines[startIndex + 1]) ?? headerCells.map(() => '');
    let index = startIndex + 2;
    const bodyRows = [];

    while (index < lines.length) {
        if (!lines[index].trim()) {
            break;
        }

        if (isBlockBoundary(lines, index) && !lines[index].includes('|')) {
            break;
        }

        const rowCells = splitTableRow(lines[index]);
        if (rowCells.length === 0) {
            break;
        }

        bodyRows.push(normalizeTableCells(rowCells, headerCells.length));
        index++;
    }

    const headerHtml = headerCells.map((cell, cellIndex) => {
        const alignment = alignments[cellIndex] ? ` style="text-align:${escapeHtml(alignments[cellIndex])};"` : '';
        return `<th${alignment}>${renderInlineMarkdown(cell)}</th>`;
    }).join('');

    const bodyHtml = bodyRows.length === 0
        ? ''
        : `<tbody>${bodyRows.map((row) => `<tr>${row.map((cell, cellIndex) => {
            const alignment = alignments[cellIndex] ? ` style="text-align:${escapeHtml(alignments[cellIndex])};"` : '';
            return `<td${alignment}>${renderInlineMarkdown(cell)}</td>`;
        }).join('')}</tr>`).join('')}</tbody>`;

    return {
        html: `<table><thead><tr>${headerHtml}</tr></thead>${bodyHtml}</table>`,
        nextIndex: index
    };
}

function renderBlockQuote(lines, startIndex) {
    const quoteLines = [];
    let index = startIndex;

    while (index < lines.length) {
        const line = lines[index];
        if (!line.trim()) {
            if (index + 1 < lines.length && isBlockQuoteLine(lines[index + 1])) {
                quoteLines.push('');
                index++;
                continue;
            }

            break;
        }

        if (!isBlockQuoteLine(line)) {
            break;
        }

        quoteLines.push(stripBlockQuoteMarker(line));
        index++;
    }

    return {
        html: `<blockquote>${renderBlocksFromLines(quoteLines).html}</blockquote>`,
        nextIndex: index
    };
}

function renderList(lines, startIndex) {
    const marker = parseListMarker(lines[startIndex]);
    if (!marker) {
        return { html: '', nextIndex: startIndex };
    }

    const { indent, ordered } = marker;
    const tagName = ordered ? 'ol' : 'ul';
    const parts = [`<${tagName}>`];
    let index = startIndex;

    while (index < lines.length) {
        if (!lines[index].trim()) {
            index++;
            continue;
        }

        const lineMarker = parseListMarker(lines[index]);
        if (!lineMarker || lineMarker.indent !== indent || lineMarker.ordered !== ordered) {
            break;
        }

        index++;
        parts.push('<li>');

        const inlineLines = [];
        if (lineMarker.content.trim()) {
            inlineLines.push(lineMarker.content.trimEnd());
        }

        while (index < lines.length) {
            if (!lines[index].trim()) {
                if (hasListContinuation(lines, index, indent)) {
                    index++;
                    continue;
                }

                break;
            }

            const nestedMarker = parseListMarker(lines[index]);
            if (nestedMarker) {
                if (nestedMarker.indent === indent) {
                    break;
                }

                if (nestedMarker.indent > indent) {
                    if (inlineLines.length > 0) {
                        parts.push(renderInlineMarkdown(inlineLines.join('\n')).replace(/\n/g, '<br />'));
                        inlineLines.length = 0;
                    }

                    const nestedList = renderList(lines, index);
                    parts.push(nestedList.html);
                    index = nestedList.nextIndex;
                    continue;
                }
            }

            const continuationIndent = countLeadingSpaces(lines[index]);
            if (continuationIndent <= indent) {
                break;
            }

            const continuationText = stripIndent(lines[index], Math.min(lines[index].length, indent + 2)).trimEnd();
            if (continuationText) {
                inlineLines.push(continuationText);
            }

            index++;
        }

        if (inlineLines.length > 0) {
            parts.push(renderInlineMarkdown(inlineLines.join('\n')).replace(/\n/g, '<br />'));
        }

        parts.push('</li>');
    }

    parts.push(`</${tagName}>`);
    return {
        html: parts.join(''),
        nextIndex: index
    };
}

function renderBlocksFromLines(lines, startIndex = 0) {
    const parts = [];
    let index = startIndex;

    while (index < lines.length) {
        if (!lines[index].trim()) {
            index++;
            continue;
        }

        if (parseAlignmentMarker(lines[index])) {
            const block = renderAlignmentBlock(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (parseIframeMarker(lines[index])) {
            const block = renderIframeBlock(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (isCodeFenceLine(lines[index])) {
            const block = renderCodeBlock(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (isBlockQuoteLine(lines[index])) {
            const block = renderBlockQuote(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (parseListMarker(lines[index])) {
            const block = renderList(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (isTableStart(lines, index)) {
            const block = renderTable(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        const headingMatch = lines[index].trim().match(/^(#{1,6})\s+(.+)$/s);
        if (headingMatch) {
            const level = headingMatch[1].length;
            parts.push(`<h${level}>${renderInlineMarkdown(headingMatch[2].trim())}</h${level}>`);
            index++;
            continue;
        }

        const block = renderParagraph(lines, index);
        parts.push(block.html);
        index = block.nextIndex;
    }

    return { html: parts.join(''), nextIndex: index };
}

function markdownToHtml(markdown) {
    const normalized = normalizeNewLines(markdown);
    if (!normalized.trim()) {
        return '';
    }

    return renderBlocksFromLines(normalized.split('\n')).html;
}

function escapeMarkdownText(value) {
    return (value ?? '').replace(/([\\*_\[\]\(\)])/g, '\\$1');
}

function escapeMarkdownAttribute(value) {
    return escapeMarkdownText(value).replace(/\n/g, ' ');
}

function normalizeAlignment(value) {
    const normalized = (value ?? '').trim().toLowerCase();
    if (normalized === 'start') {
        return 'left';
    }

    if (normalized === 'end') {
        return 'right';
    }

    return ['left', 'center', 'right', 'justify'].includes(normalized) ? normalized : '';
}

function getElementAlignment(element) {
    if (!(element instanceof Element)) {
        return '';
    }

    return normalizeAlignment(element.style?.textAlign || element.getAttribute?.('align') || '');
}

function hasRenderableBlockChildren(element) {
    return Array.from(element?.children ?? []).some((child) => blockNodeTags.has(child.tagName) || child.tagName === 'IMG');
}

function wrapAlignedMarkdown(alignment, markdown) {
    const normalizedAlignment = normalizeAlignment(alignment);
    if (!normalizedAlignment || normalizedAlignment === 'left' || !markdown.trim()) {
        return markdown;
    }

    return `<div align="${normalizedAlignment}">\n${markdown}\n</div>`;
}

function serializeInline(node) {
    if (!node) {
        return '';
    }

    if (node.nodeType === Node.TEXT_NODE) {
        return escapeMarkdownText(node.textContent ?? '');
    }

    if (node.nodeType !== Node.ELEMENT_NODE) {
        return '';
    }

    const tagName = node.tagName.toLowerCase();
    const content = Array.from(node.childNodes).map(serializeInline).join('');

    switch (tagName) {
        case 'strong':
        case 'b':
            return `**${content}**`;
        case 'em':
        case 'i':
            return `*${content}*`;
        case 'u':
            return `<u>${content}</u>`;
        case 's':
        case 'strike':
        case 'del':
            return `~~${content}~~`;
        case 'span': {
            const textColor = getElementTextColor(node);
            return textColor ? `<span style="color:${escapeMarkdownAttribute(textColor)};">${content}</span>` : content;
        }
        case 'br':
            return '\n';
        case 'img':
            return `![${escapeMarkdownAttribute(node.getAttribute('alt') ?? '')}](${node.getAttribute('src') ?? ''})`;
        case 'a':
            return `[${content}](${node.getAttribute('href') ?? ''})`;
        default:
            return content;
    }
}

function indentMarkdownBlock(markdown, indent) {
    const indentation = ' '.repeat(indent);
    return markdown
        .split('\n')
        .map((line) => line.length > 0 ? `${indentation}${line}` : line)
        .join('\n');
}

function serializeCodeBlock(block) {
    const codeElement = block.querySelector('code');
    const language = codeElement?.dataset.language ?? block.dataset.language ?? '';
    const codeText = normalizeNewLines(codeElement?.textContent ?? block.textContent ?? '').replace(/\n+$/g, '');
    return `\`\`\`${language}\n${codeText}\n\`\`\``;
}

function serializeBlockQuote(block) {
    const innerMarkdown = serializeContainerBlocks(block);
    return innerMarkdown
        .split('\n')
        .map((line) => line.length > 0 ? `> ${line}` : '>')
        .join('\n');
}

function serializeListItemContent(indent, marker, content) {
    const lines = content.split('\n');
    const firstLine = lines.shift() ?? '';
    const result = [`${' '.repeat(indent)}${marker}${firstLine}`.trimEnd()];
    const continuationIndent = ' '.repeat(indent + 2);

    for (const line of lines) {
        result.push(`${continuationIndent}${line}`.trimEnd());
    }

    return result.join('\n');
}

function serializeList(listElement, indent = 0) {
    const ordered = listElement.tagName.toLowerCase() === 'ol';
    const items = [];
    const listItems = Array.from(listElement.children).filter((child) => child.tagName === 'LI');

    listItems.forEach((item, itemIndex) => {
        const marker = ordered ? `${itemIndex + 1}. ` : '- ';
        let inlineContent = '';
        const nestedBlocks = [];

        for (const child of Array.from(item.childNodes)) {
            if (child.nodeType === Node.ELEMENT_NODE && blockNodeTags.has(child.tagName)) {
                if (child.tagName === 'UL' || child.tagName === 'OL') {
                    nestedBlocks.push(serializeList(child, indent + 2));
                    continue;
                }

                nestedBlocks.push(indentMarkdownBlock(serializeBlock(child), indent + 2));
                continue;
            }

            inlineContent += serializeInline(child);
        }

        const normalizedInline = inlineContent.replace(/\u00a0/g, ' ').replace(/\n+$/g, '').trimEnd();
        const lines = [];

        if (normalizedInline.length > 0 || nestedBlocks.length === 0) {
            lines.push(serializeListItemContent(indent, marker, normalizedInline));
        } else {
            lines.push(`${' '.repeat(indent)}${marker}`.trimEnd());
        }

        lines.push(...nestedBlocks);
        items.push(lines.join('\n'));
    });

    return items.join('\n');
}

function getTableRows(tableElement) {
    const rows = [];
    const headRows = Array.from(tableElement.querySelectorAll(':scope > thead > tr'));
    const bodyRows = Array.from(tableElement.querySelectorAll(':scope > tbody > tr'));

    if (headRows.length > 0) {
        rows.push(headRows[0]);
    } else {
        const firstRow = tableElement.querySelector(':scope > tr');
        if (firstRow) {
            rows.push(firstRow);
        }
    }

    rows.push(...bodyRows);
    return rows;
}

function serializeTableCell(cell) {
    return Array.from(cell.childNodes)
        .map(serializeInline)
        .join('')
        .replace(/\u00a0/g, ' ')
        .replace(/\s*\n+\s*/g, ' ')
        .trim();
}

function getTableCellAlignment(cell) {
    const alignment = getElementAlignment(cell);
    if (alignment) {
        return alignment;
    }

    return getElementAlignment(cell.closest('table'));
}

function formatTableSeparator(alignment) {
    switch (alignment) {
        case 'center':
            return ':---:';
        case 'right':
            return '---:';
        case 'left':
            return ':---';
        default:
            return '---';
    }
}

function serializeTable(tableElement) {
    const rows = getTableRows(tableElement);
    if (rows.length === 0) {
        return '';
    }

    const headerCells = Array.from(rows[0].children).filter((cell) => cell.tagName === 'TH' || cell.tagName === 'TD');
    if (headerCells.length === 0) {
        return '';
    }

    const headerRow = `| ${headerCells.map(serializeTableCell).join(' | ')} |`;
    const separatorRow = `| ${headerCells.map((cell) => formatTableSeparator(getTableCellAlignment(cell))).join(' | ')} |`;
    const bodyRows = rows.slice(1)
        .map((row) => Array.from(row.children).filter((cell) => cell.tagName === 'TH' || cell.tagName === 'TD'))
        .filter((cells) => cells.length > 0)
        .map((cells) => `| ${normalizeTableCells(cells.map(serializeTableCell), headerCells.length).join(' | ')} |`);

    return [headerRow, separatorRow, ...bodyRows].join('\n');
}

function serializeBlock(block) {
    if (!block || block.nodeType !== Node.ELEMENT_NODE) {
        return '';
    }

    const tagName = block.tagName.toLowerCase();
    const alignment = getElementAlignment(block);
    let markdown = '';

    if (tagName === 'div' && hasRenderableBlockChildren(block)) {
        markdown = serializeContainerBlocks(block);
    } else if (/^h[1-6]$/.test(tagName)) {
        const text = Array.from(block.childNodes).map(serializeInline).join('');
        const normalized = text.replace(/\u00a0/g, ' ').replace(/\n{3,}/g, '\n\n').replace(/\n+$/g, '').trimEnd();
        markdown = `${'#'.repeat(Number.parseInt(tagName[1], 10))} ${normalized}`.trimEnd();
    } else if (tagName === 'ul' || tagName === 'ol') {
        markdown = serializeList(block);
    } else if (tagName === 'blockquote') {
        markdown = serializeBlockQuote(block);
    } else if (tagName === 'pre') {
        markdown = serializeCodeBlock(block);
    } else if (tagName === 'table') {
        markdown = serializeTable(block);
    } else if (tagName === 'img') {
        markdown = serializeInline(block);
    } else if (tagName === 'iframe') {
        markdown = `<iframe src="${block.getAttribute('src') ?? ''}" title="${escapeMarkdownAttribute(block.getAttribute('title') ?? '')}" width="${block.getAttribute('width') ?? '100%'}" height="${block.getAttribute('height') ?? '315'}" loading="lazy"></iframe>`;
    } else {
        const text = Array.from(block.childNodes).map(serializeInline).join('');
        markdown = text.replace(/\u00a0/g, ' ').replace(/\n{3,}/g, '\n\n').replace(/\n+$/g, '').trimEnd();
    }

    return wrapAlignedMarkdown(alignment, markdown);
}

function serializeContainerBlocks(container) {
    const blocks = [];
    let inlineBuffer = '';

    for (const child of Array.from(container.childNodes)) {
        if (child.nodeType === Node.ELEMENT_NODE && (blockNodeTags.has(child.tagName) || child.tagName === 'IMG')) {
            if (inlineBuffer.trim().length > 0) {
                blocks.push(inlineBuffer.replace(/\n+$/g, ''));
                inlineBuffer = '';
            }

            blocks.push(serializeBlock(child));
            continue;
        }

        inlineBuffer += serializeInline(child);
    }

    if (inlineBuffer.trim().length > 0) {
        blocks.push(inlineBuffer.replace(/\n+$/g, ''));
    }

    return blocks
        .map((block) => block.replace(/\u00a0/g, ' ').trimEnd())
        .filter((block) => block.length > 0)
        .join('\n\n')
        .replace(/\n{3,}/g, '\n\n')
        .trim();
}

function surfaceToMarkdown(surface) {
    if (!surface) {
        return '';
    }

    return serializeContainerBlocks(surface);
}

function updateSourceValue(element, markdown) {
    const sourceValueElement = getSourceValueElement(element);
    if (sourceValueElement) {
        sourceValueElement.textContent = markdown;
    }

    const hiddenInput = getHiddenInput(element);
    if (hiddenInput) {
        hiddenInput.value = markdown;
    }
}

function updateEmptyState(surface) {
    if (!surface) {
        return;
    }

    const hasContent = Boolean(surface.textContent?.trim().length)
        || Boolean(surface.querySelector('strong, b, em, i, u, br, p, div, ul, ol, li, blockquote, pre, table, img, iframe'));
    surface.dataset.empty = hasContent ? 'false' : 'true';
}

function updateLength(element, markdown) {
    const lengthElement = getLengthElement(element);
    if (!lengthElement) {
        return;
    }

    const maxLength = getSurface(element)?.dataset.maxlength;
    if (!maxLength) {
        return;
    }

    lengthElement.textContent = `${markdown.length}/${maxLength}`;
}

function applyMarkdownToSurface(element, markdown) {
    const surface = getSurface(element);
    if (!surface) {
        return;
    }

    surface.innerHTML = markdownToHtml(markdown);
    updateEmptyState(surface);
    updateLength(element, markdown);
    updateSourceValue(element, markdown);
    updateToolbarState(element);
}

function syncValueFromSurface(element, notifyDotNet) {
    const state = editorState.get(element);
    const surface = getSurface(element);
    if (!state || !surface) {
        return;
    }

    const markdown = surfaceToMarkdown(surface);
    state.lastMarkdown = markdown;
    updateEmptyState(surface);
    updateLength(element, markdown);
    updateSourceValue(element, markdown);
    updateToolbarState(element);

    if (notifyDotNet && state.dotNetRef) {
        state.dotNetRef.invokeMethodAsync('UpdateValueFromJs', markdown);
    }
}

function commitValue(element) {
    const state = editorState.get(element);
    const surface = getSurface(element);
    if (!state || !surface) {
        return;
    }

    const markdown = surfaceToMarkdown(surface);
    state.lastMarkdown = markdown;
    updateEmptyState(surface);
    updateLength(element, markdown);
    updateSourceValue(element, markdown);
    updateToolbarState(element);

    state.dotNetRef?.invokeMethodAsync('CommitValueFromJs', markdown);
}

function updateToolbarState(element) {
    const surface = getSurface(element);
    const blockElement = getCurrentBlockElement(surface);
    const activeBlockTag = blockElement?.tagName?.toLowerCase?.() ?? 'p';
    const activeAlignment = getCurrentAlignment(surface, blockElement);
    const isBold = hasAncestorTag(surface, 'strong, b');
    const isItalic = hasAncestorTag(surface, 'em, i');
    const isUnderline = hasAncestorTag(surface, 'u');
    const isStrikethrough = hasAncestorTag(surface, 's, strike, del');
    const isTextColor = hasAncestorTag(surface, 'span[style*="color"]');
    const isLink = hasAncestorTag(surface, 'a');
    const isIframe = activeBlockTag === 'iframe';
    const isUnorderedList = hasAncestorTag(surface, 'ul');
    const isOrderedList = hasAncestorTag(surface, 'ol');
    const isBlockQuote = hasAncestorTag(surface, 'blockquote');
    const isCodeBlock = hasAncestorTag(surface, 'pre');
    const isTable = hasAncestorTag(surface, 'table');

    for (const button of getToolbarButtons(element)) {
        let isSelected = false;

        if (button.dataset.command === 'paragraph') {
            isSelected = activeBlockTag === 'p' || activeBlockTag === 'div';
        } else if (button.dataset.command === 'heading') {
            isSelected = activeBlockTag === `h${button.dataset.value ?? '1'}`;
        } else if (button.dataset.command === 'alignLeft') {
            isSelected = activeAlignment === 'left';
        } else if (button.dataset.command === 'alignCenter') {
            isSelected = activeAlignment === 'center';
        } else if (button.dataset.command === 'alignRight') {
            isSelected = activeAlignment === 'right';
        } else if (button.dataset.command === 'alignJustify') {
            isSelected = activeAlignment === 'justify';
        } else if (button.dataset.command === 'unorderedList') {
            isSelected = isUnorderedList;
        } else if (button.dataset.command === 'orderedList') {
            isSelected = isOrderedList;
        } else if (button.dataset.command === 'blockquote') {
            isSelected = isBlockQuote;
        } else if (button.dataset.command === 'codeBlock') {
            isSelected = isCodeBlock;
        } else if (button.dataset.command === 'table') {
            isSelected = isTable;
        } else if (button.dataset.command === 'bold') {
            isSelected = isBold;
        } else if (button.dataset.command === 'italic') {
            isSelected = isItalic;
        } else if (button.dataset.command === 'underline') {
            isSelected = isUnderline;
        } else if (button.dataset.command === 'strikeThrough') {
            isSelected = isStrikethrough;
        } else if (button.dataset.command === 'textColor') {
            isSelected = isTextColor;
        } else if (button.dataset.command === 'link') {
            isSelected = isLink;
        } else if (button.dataset.command === 'iframe') {
            isSelected = isIframe;
        }

        button.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
    }
}

function insertImageAtSelection(surface) {
    const imageUrl = window.prompt('Image URL');
    if (!imageUrl) {
        return false;
    }

    const imageAlt = window.prompt('Alt text', '') ?? '';
    const html = `<img src="${escapeHtml(imageUrl)}" alt="${escapeHtml(imageAlt)}" />`;
    document.execCommand('insertHTML', false, html);
    surface.focus();
    return true;
}

function insertIframeAtSelection(surface) {
    const iframeUrl = window.prompt('Iframe URL');
    if (!iframeUrl) {
        return false;
    }

    const iframeTitle = window.prompt('Iframe title', 'Embedded content') ?? 'Embedded content';
    const iframeWidth = (window.prompt('Iframe width', '100%') ?? '100%').trim() || '100%';
    const iframeHeight = (window.prompt('Iframe height', '315') ?? '315').trim() || '315';
    const html = `<iframe src="${escapeHtml(iframeUrl.trim())}" title="${escapeHtml(iframeTitle.trim())}" width="${escapeHtml(iframeWidth)}" height="${escapeHtml(iframeHeight)}" loading="lazy"></iframe>`;
    document.execCommand('insertHTML', false, html);
    surface.focus();
    return true;
}

function insertTableAtSelection(surface) {
    const requestedColumns = Number.parseInt(window.prompt('Table columns', '3') ?? '', 10);
    if (Number.isNaN(requestedColumns)) {
        return false;
    }

    const requestedRows = Number.parseInt(window.prompt('Table rows', '2') ?? '', 10);
    if (Number.isNaN(requestedRows)) {
        return false;
    }

    const columns = Math.min(Math.max(requestedColumns, 1), 8);
    const rows = Math.min(Math.max(requestedRows, 1), 12);
    const headerCells = Array.from({ length: columns }, (_, index) => `<th>Header ${index + 1}</th>`).join('');
    const bodyRows = Array.from({ length: rows }, (_, rowIndex) => `<tr>${Array.from({ length: columns }, (_, columnIndex) => `<td>Cell ${rowIndex + 1}-${columnIndex + 1}</td>`).join('')}</tr>`).join('');
    const html = `<table><thead><tr>${headerCells}</tr></thead><tbody>${bodyRows}</tbody></table><p><br></p>`;
    document.execCommand('insertHTML', false, html);
    surface.focus();
    return true;
}

function insertLinkAtSelection(surface) {
    const selectionElement = getSelectionElement(surface);
    const existingLink = selectionElement?.closest?.('a') ?? null;
    const defaultUrl = existingLink?.getAttribute('href') ?? '';
    const linkUrl = window.prompt('Link URL', defaultUrl);
    if (linkUrl === null) {
        return false;
    }

    const trimmedUrl = linkUrl.trim();
    if (!trimmedUrl) {
        if (existingLink) {
            document.execCommand('unlink', false);
            surface.focus();
            return true;
        }

        return false;
    }

    const selection = window.getSelection?.();
    const selectedText = selection?.toString?.().trim?.() ?? '';
    if (selectedText.length > 0) {
        document.execCommand('createLink', false, trimmedUrl);
        surface.focus();
        return true;
    }

    const defaultText = existingLink?.textContent?.trim?.() || trimmedUrl;
    const linkText = window.prompt('Link text', defaultText);
    if (linkText === null) {
        return false;
    }

    const text = linkText.trim() || trimmedUrl;
    document.execCommand('insertHTML', false, `<a href="${escapeHtml(trimmedUrl)}">${escapeHtml(text)}</a>`);
    surface.focus();
    return true;
}

function applyTextColorAtSelection(surface) {
    const selectionElement = getSelectionElement(surface);
    const existingColorElement = selectionElement?.closest?.('span[style*="color"]') ?? null;
    const defaultColor = getElementTextColor(existingColorElement) || '#1d4ed8';
    const colorValue = window.prompt('Text color', defaultColor);
    if (colorValue === null) {
        return false;
    }

    const trimmedColor = colorValue.trim();
    if (!trimmedColor) {
        if (existingColorElement) {
            const parent = existingColorElement.parentNode;
            while (existingColorElement.firstChild) {
                parent?.insertBefore(existingColorElement.firstChild, existingColorElement);
            }

            parent?.removeChild(existingColorElement);
            surface.focus();
            return true;
        }

        return false;
    }

    const escapedColor = escapeHtml(trimmedColor);
    const selection = window.getSelection?.();
    const selectedText = selection?.toString?.() ?? '';

    if (selectedText.trim().length > 0) {
        document.execCommand('insertHTML', false, `<span style="color:${escapedColor};">${escapeHtml(selectedText)}</span>`);
        surface.focus();
        return true;
    }

    if (existingColorElement) {
        existingColorElement.style.color = trimmedColor;
        surface.focus();
        return true;
    }

    const defaultText = selectedText.trim() || 'Colored text';
    const textValue = window.prompt('Text to color', defaultText);
    if (textValue === null) {
        return false;
    }

    const text = textValue.trim() || defaultText;
    document.execCommand('insertHTML', false, `<span style="color:${escapedColor};">${escapeHtml(text)}</span>`);
    surface.focus();
    return true;
}

function executeEditorCommand(element, command, value) {
    const surface = getSurface(element);
    if (!surface) {
        return false;
    }

    surface.focus();
    let didChange = true;
    switch (command) {
        case 'undo':
            document.execCommand('undo', false);
            break;
        case 'redo':
            document.execCommand('redo', false);
            break;
        case 'heading':
            document.execCommand('formatBlock', false, `H${value ?? '1'}`);
            break;
        case 'paragraph':
            document.execCommand('formatBlock', false, 'P');
            break;
        case 'alignLeft':
            document.execCommand('justifyLeft', false);
            break;
        case 'alignCenter':
            document.execCommand('justifyCenter', false);
            break;
        case 'alignRight':
            document.execCommand('justifyRight', false);
            break;
        case 'alignJustify':
            document.execCommand('justifyFull', false);
            break;
        case 'unorderedList':
            document.execCommand('insertUnorderedList', false);
            break;
        case 'orderedList':
            document.execCommand('insertOrderedList', false);
            break;
        case 'blockquote':
            if (hasAncestorTag(surface, 'blockquote')) {
                document.execCommand('outdent', false);
            } else {
                document.execCommand('formatBlock', false, 'BLOCKQUOTE');
            }
            break;
        case 'codeBlock':
            if (hasAncestorTag(surface, 'pre')) {
                document.execCommand('formatBlock', false, 'P');
            } else {
                document.execCommand('formatBlock', false, 'PRE');
            }
            break;
        case 'table':
            didChange = insertTableAtSelection(surface);
            break;
        case 'image':
            didChange = insertImageAtSelection(surface);
            break;
        case 'textColor':
            didChange = applyTextColorAtSelection(surface);
            break;
        case 'iframe':
            didChange = insertIframeAtSelection(surface);
            break;
        case 'link':
            didChange = insertLinkAtSelection(surface);
            break;
        default:
            document.execCommand(command, false);
            break;
    }

    if (!didChange) {
        updateToolbarState(element);
        return false;
    }

    syncValueFromSurface(element, true);
    updateToolbarState(element);
    return true;
}

function handleToolbarCommand(event) {
    const button = event.currentTarget;
    const element = button?.closest?.('nt-rich-text-editor');
    const surface = getSurface(element);
    if (!surface || button.disabled) {
        return;
    }

    event.preventDefault();
    executeEditorCommand(element, button.dataset.command, button.dataset.value);
}

function eventMatchesShortcut(event, key, { ctrl = false, alt = false, shift = false } = {}) {
    const hasPrimaryModifier = event.ctrlKey || event.metaKey;
    return hasPrimaryModifier === ctrl
        && event.altKey === alt
        && event.shiftKey === shift
        && event.key.toLowerCase() === key.toLowerCase();
}

function tryHandleShortcut(element, event) {
    const shortcutMap = [
        { key: 'z', modifiers: { ctrl: true }, command: 'undo' },
        { key: 'y', modifiers: { ctrl: true }, command: 'redo' },
        { key: 'z', modifiers: { ctrl: true, shift: true }, command: 'redo' },
        { key: 'b', modifiers: { ctrl: true }, command: 'bold' },
        { key: 'i', modifiers: { ctrl: true }, command: 'italic' },
        { key: 'u', modifiers: { ctrl: true }, command: 'underline' },
        { key: 's', modifiers: { ctrl: true, shift: true }, command: 'strikeThrough' },
        { key: 'x', modifiers: { ctrl: true, alt: true }, command: 'textColor' },
        { key: 'k', modifiers: { ctrl: true }, command: 'link' },
        { key: 'l', modifiers: { ctrl: true, shift: true }, command: 'alignLeft' },
        { key: 'e', modifiers: { ctrl: true, shift: true }, command: 'alignCenter' },
        { key: 'r', modifiers: { ctrl: true, shift: true }, command: 'alignRight' },
        { key: 'j', modifiers: { ctrl: true, shift: true }, command: 'alignJustify' },
        { key: '0', modifiers: { ctrl: true, alt: true }, command: 'paragraph' },
        { key: '1', modifiers: { ctrl: true, alt: true }, command: 'heading', value: '1' },
        { key: '2', modifiers: { ctrl: true, alt: true }, command: 'heading', value: '2' },
        { key: '3', modifiers: { ctrl: true, alt: true }, command: 'heading', value: '3' },
        { key: '4', modifiers: { ctrl: true, alt: true }, command: 'heading', value: '4' },
        { key: '5', modifiers: { ctrl: true, alt: true }, command: 'heading', value: '5' },
        { key: '6', modifiers: { ctrl: true, alt: true }, command: 'heading', value: '6' },
        { key: '7', modifiers: { ctrl: true, alt: true }, command: 'unorderedList' },
        { key: '8', modifiers: { ctrl: true, alt: true }, command: 'orderedList' },
        { key: 'q', modifiers: { ctrl: true, alt: true }, command: 'blockquote' },
        { key: 'c', modifiers: { ctrl: true, alt: true }, command: 'codeBlock' },
        { key: 't', modifiers: { ctrl: true, alt: true }, command: 'table' },
        { key: 'm', modifiers: { ctrl: true, alt: true }, command: 'image' },
        { key: 'f', modifiers: { ctrl: true, alt: true }, command: 'iframe' }
    ];

    const match = shortcutMap.find((shortcut) => eventMatchesShortcut(event, shortcut.key, shortcut.modifiers));
    if (!match) {
        return false;
    }

    event.preventDefault();
    executeEditorCommand(element, match.command, match.value);
    return true;
}

function handleEditorKeyDown(element, event) {
    const surface = getSurface(element);
    if (!surface) {
        return;
    }

    if (tryHandleShortcut(element, event)) {
        return;
    }

    if (event.key === 'Tab' && hasAncestorTag(surface, 'li')) {
        event.preventDefault();
        document.execCommand(event.shiftKey ? 'outdent' : 'indent', false);
        const shouldNotify = element.dataset.bindOnInput === 'true';
        syncValueFromSurface(element, shouldNotify);
        updateToolbarState(element);
    }
}

function ensureState(element, dotNetRef) {
    if (!element) {
        return null;
    }

    const existingState = editorState.get(element);
    if (existingState) {
        existingState.dotNetRef = dotNetRef ?? existingState.dotNetRef;
        return existingState;
    }

    const surface = getSurface(element);
    if (!surface) {
        return null;
    }

    const state = {
        dotNetRef,
        lastMarkdown: normalizeNewLines(getSourceValueElement(element)?.textContent ?? ''),
        onInput: () => {
            const shouldNotify = element.dataset.bindOnInput === 'true';
            syncValueFromSurface(element, shouldNotify);
        },
        onFocus: () => {
            updateToolbarState(element);
        },
        onBlur: () => {
            window.setTimeout(() => {
                if (element.contains(document.activeElement)) {
                    return;
                }

                updateToolbarState(element);
                commitValue(element);
            }, 0);
        },
        onKeyUp: () => {
            updateToolbarState(element);
        },
        onMouseUp: () => {
            updateToolbarState(element);
        },
        onKeyDown: (event) => {
            handleEditorKeyDown(element, event);
        },
        onToolbarMouseDown: (event) => {
            event.preventDefault();
        }
    };

    surface.addEventListener('input', state.onInput);
    surface.addEventListener('focus', state.onFocus);
    surface.addEventListener('blur', state.onBlur);
    surface.addEventListener('keyup', state.onKeyUp);
    surface.addEventListener('mouseup', state.onMouseUp);
    surface.addEventListener('keydown', state.onKeyDown);

    for (const button of getToolbarButtons(element)) {
        button.addEventListener('mousedown', state.onToolbarMouseDown);
        button.addEventListener('click', handleToolbarCommand);
    }

    editorState.set(element, state);
    return state;
}

function synchronizeElement(element, dotNetRef) {
    const state = ensureState(element, dotNetRef);
    if (!state) {
        return;
    }

    state.dotNetRef = dotNetRef ?? state.dotNetRef;

    const markdown = normalizeNewLines(getSourceValueElement(element)?.textContent ?? '');
    if (markdown !== state.lastMarkdown) {
        applyMarkdownToSurface(element, markdown);
        state.lastMarkdown = markdown;
    } else {
        updateLength(element, markdown);
        updateEmptyState(getSurface(element));
        updateSourceValue(element, markdown);
    }

    const isEditable = element.dataset.editable === 'true';
    const surface = getSurface(element);
    if (surface) {
        surface.setAttribute('contenteditable', isEditable ? 'true' : 'false');
    }

    for (const button of getToolbarButtons(element)) {
        button.disabled = !isEditable;
    }

    updateToolbarState(element);
}

function initializeAllEditors() {
    for (const element of document.querySelectorAll('nt-rich-text-editor')) {
        synchronizeElement(element, null);
    }
}

function registerSelectionChangeHandler() {
    if (selectionChangeRegistered) {
        return;
    }

    selectionChangeRegistered = true;
    document.addEventListener('selectionchange', () => {
        for (const element of document.querySelectorAll('nt-rich-text-editor')) {
            updateToolbarState(element);
        }
    });
}

export function focusEditor(element) {
    getSurface(element)?.focus();
}

export function onLoad(element, dotNetRef) {
    registerSelectionChangeHandler();

    if (!element) {
        initializeAllEditors();
        return;
    }

    synchronizeElement(element, dotNetRef);
}

export function onUpdate(element, dotNetRef) {
    if (!element) {
        initializeAllEditors();
        return;
    }

    synchronizeElement(element, dotNetRef);
}

export function onDispose(element) {
    if (!element) {
        return;
    }

    const state = editorState.get(element);
    const surface = getSurface(element);
    if (state && surface) {
        surface.removeEventListener('input', state.onInput);
        surface.removeEventListener('focus', state.onFocus);
        surface.removeEventListener('blur', state.onBlur);
        surface.removeEventListener('keyup', state.onKeyUp);
        surface.removeEventListener('mouseup', state.onMouseUp);
        surface.removeEventListener('keydown', state.onKeyDown);
    }

    for (const button of getToolbarButtons(element)) {
        button.removeEventListener('mousedown', state?.onToolbarMouseDown);
        button.removeEventListener('click', handleToolbarCommand);
    }

    editorState.delete(element);
}

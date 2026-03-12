using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a text area input component.
/// </summary>
public partial class TnTInputTextArea {

    /// <summary>
    ///     Gets or sets a value indicating whether field-based sizing is set to content.
    /// </summary>
    [Parameter]
    public bool SizeByContent { get; set; }

    private IReadOnlyDictionary<string, object>? InputAttributes {
        get {
            if (!SizeByContent) {
                return AdditionalAttributes;
            }

            var attributes = AdditionalAttributes is not null ? new Dictionary<string, object>(AdditionalAttributes) : [];
            var existingStyle = attributes.TryGetValue("style", out var style) ? style?.ToString() : null;

            attributes["style"] = CssStyleBuilder.Create()
                .Add(existingStyle ?? string.Empty)
                .AddStyle("field-sizing", "content")
                .Build()!;

            return attributes;
        }
    }

    /// <inheritdoc />
    public override InputType Type => InputType.TextArea;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}

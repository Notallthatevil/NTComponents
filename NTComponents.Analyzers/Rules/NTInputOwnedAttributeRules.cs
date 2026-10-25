using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>
///     NTC1061: raw native attributes that an NT form component filters out of its additional attributes or overwrites. Each
///     component lists only the attributes it really drops, with a replacement that exists on that component.
/// </summary>
/// <remarks>
///     Never listed: attributes the base classes read from additional attributes (<c>required</c>, <c>name</c>, <c>min</c>,
///     <c>max</c>, <c>minlength</c>, <c>maxlength</c>), <c>aria-describedby</c> on components that merge it, and any name that
///     matches a parameter ignoring case (<c>disabled</c>, <c>placeholder</c>, <c>value</c>, <c>min</c>, <c>accept</c> and so on),
///     because Razor and the renderer bind those to the parameter instead of passing them through. Names are compared
///     case-sensitively so a parameter such as <c>Value</c> is never mistaken for the native attribute.
/// </remarks>
internal static class NTInputOwnedAttributeRules {

    public static readonly DiagnosticDescriptor OwnedAttribute = new(
        "NTC1061",
        "NT form components own some native attributes",
        "Do not set '{0}' directly on {1}; {2}",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1061");

    private const string Managed = "the component manages it, so remove it";

    // Remedies by attribute name. Names without an entry use Managed.
    private static readonly Dictionary<string, string> Remedies = new(StringComparer.Ordinal) {
        ["id"] = "use ElementId",
        ["title"] = "use ElementTitle",
        ["lang"] = "use ElementLang",
        ["oninput"] = "use ValueChanged or @bind-Value",
        ["onchange"] = "use ValueChanged or @bind-Value",
        ["checked"] = "use Value or @bind-Value",
        ["aria-checked"] = "use Value or @bind-Value",
        ["aria-required"] = "use Required",
        ["aria-describedby"] = "use SupportingText, PrefixText, SuffixText, or validation text",
        ["aria-errormessage"] = "use ErrorText or validation text",
        ["aria-invalid"] = "use ErrorText or EditContext validation",
        ["aria-labelledby"] = "use Label",
        ["aria-label"] = "use AriaLabel",
        ["data-value"] = "use Value",
        ["data-show-alpha"] = "use ShowAlpha",
        ["data-disabled"] = "use Disabled"
    };

    private static readonly string[] Errors = ["aria-invalid", "aria-errormessage"];
    private static readonly string[] ComboboxOwned = ["role", "aria-expanded", "aria-controls", "aria-autocomplete", "aria-haspopup", "aria-activedescendant"];
    private static readonly string[] FieldIdentity = ["id", "title", "aria-describedby", "aria-invalid", "aria-errormessage"];
    private static readonly string[] SliderDropped = ["id", "aria-labelledby", "aria-invalid", "aria-errormessage", "oninput", "onchange"];

    /// <summary>Owned attributes by component metadata name. The most derived entry in a component's base chain wins.</summary>
    private static readonly Dictionary<string, Dictionary<string, string>> Owned = new(StringComparer.Ordinal) {
        // NTInputBase: NTInputText, NTInputNumeric, NTInputCurrency, NTInputColor, NTInputDateTime and NTTextArea.
        ["NTComponents.NTInputBase`1"] = Table([.. FieldIdentity, "oninput"]).With("oninput", "use BindOnInput or @bind-Value"),
        ["NTComponents.NTInputText"] = Table([.. FieldIdentity, "oninput", "type"]).With("oninput", "use BindOnInput or @bind-Value").With("type", "use InputType"),
        ["NTComponents.NTRichTextEditor"] = Table(FieldIdentity),
        ["NTComponents.NTAutocomplete"] = Table([.. FieldIdentity, "oninput", "onchange", .. ComboboxOwned]),
        ["NTComponents.NTCombobox`1"] = Table([.. FieldIdentity, .. ComboboxOwned]),
        ["NTComponents.NTTypeahead`1"] = Table([.. FieldIdentity, "oninput", .. ComboboxOwned]),
        ["NTComponents.NTSelect`1"] = Table([.. FieldIdentity, "onchange"]),
        ["NTComponents.NTFileUpload"] = Table(["id", "aria-labelledby", "aria-describedby", "aria-invalid", "aria-errormessage"]),
        ["NTComponents.NTInputSlider`1"] = Table(SliderDropped),
        ["NTComponents.NTInputRangeSlider`1"] = Table([.. SliderDropped, "aria-label"]).With("aria-label", "use Label, StartHandleLabel, or EndHandleLabel"),
        // These merge aria-describedby with the generated ids, so it is not listed.
        ["NTComponents.NTBooleanInputBase"] = Table(["id", "checked", "aria-checked", "aria-required", "onchange", .. Errors]),
        ["NTComponents.NTInputRadioGroup`1"] = Table(["id", "title", "lang", "role", "aria-labelledby", "aria-required", "tabindex", "onkeydown", .. Errors]).With("aria-required", "set required instead"),
        ["NTComponents.NTInputRadio`1"] = Table(["id", "name", "required", "type", "checked", "onchange", "aria-describedby"])
            .With("name", "set GroupName on NTInputRadioGroup")
            .With("required", "set required on NTInputRadioGroup")
            .With("checked", "bind Value on NTInputRadioGroup")
            .With("onchange", "bind Value on NTInputRadioGroup")
            .With("aria-describedby", "use SupportingText"),
        ["NTComponents.NTColorPicker"] = Table(["role", "aria-label", "data-nt-color-picker", "data-value", "data-show-alpha", "data-disabled"]),
        ["NTComponents.NTFormFieldGridView"] = Table(["id", "title", "lang"]),
        ["NTComponents.NTFormFieldLayoutSpan"] = Table(["id", "title", "lang"]),
        ["NTComponents.NTFormSectionView"] = Table(["id", "title", "lang"])
    };

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(OwnedAttribute);
        foreach (var name in new[] { "NTComponents.NTFormControlBaseCore`1", "NTComponents.NTInputRadio`1", "NTComponents.NTColorPicker", "NTComponents.NTFormFieldGridView", "NTComponents.NTFormFieldLayoutSpan", "NTComponents.NTFormSectionView" }) {
            registry.On(name, Analyze);
        }
    }

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        Dictionary<string, string>? owned = null;
        for (var type = frame.Type.OriginalDefinition; type is not null && owned is null; type = type.BaseType?.OriginalDefinition) {
            Owned.TryGetValue(ComponentFrame.MetadataNameOf(type), out owned);
        }

        if (owned is null) {
            return;
        }

        foreach (var attribute in frame.Attributes.Values) {
            if (owned.TryGetValue(attribute.Name, out var remedy)) {
                context.Report(OwnedAttribute, frame.LocationOf(attribute.Name), attribute.Name, frame.Type.Name, remedy);
            }
        }
    }

    private static Dictionary<string, string> Table(string[] names) {
        var table = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in names) {
            table[name] = Remedies.TryGetValue(name, out var remedy) ? remedy : Managed;
        }

        return table;
    }

    private static Dictionary<string, string> With(this Dictionary<string, string> table, string name, string remedy) {
        table[name] = remedy;
        return table;
    }
}

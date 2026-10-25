using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>
///     Rules for raw HTML attributes that non-input NT components overwrite or drop. Each entry was checked against the component's
///     markup: the component writes the attribute after <c>@attributes</c>, or filters it out, so a passed value never reaches the DOM.
///     Attributes that match a parameter name ignoring case, such as <c>href</c> or <c>disabled</c>, bind to that parameter and are not listed.
/// </summary>
internal static class OwnedAttributeRules {
    private const string Nothing = "nothing; the value is ignored";

    public static readonly DiagnosticDescriptor OwnedAttribute = new(
        "NTC1109",
        "Attribute is set by the component itself",
        "{0} sets the '{1}' attribute itself; use {2}",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1109");

    private static readonly (string Component, (string Attribute, string Instead)[] Owned)[] _components = [
        ("NTComponents.NTTooltip", [("role", Nothing), ("aria-hidden", Nothing)]),
        ("NTComponents.NTWindow", [("role", Nothing), ("aria-modal", Nothing), ("aria-labelledby", "Title"), ("tabindex", Nothing)]),
        ("NTComponents.NTWindowHost", [("role", Nothing), ("aria-label", "AriaLabel")]),
        ("NTComponents.NTDivider", [("role", Nothing), ("aria-orientation", "Direction")]),
        ("NTComponents.NTTab", [("role", Nothing), ("aria-labelledby", Nothing), ("tabindex", Nothing), ("hidden", Nothing)]),
        ("NTComponents.NTBreadcrumb", [("aria-label", "AccessibleLabel")]),
        ("NTComponents.NTNavigationRail", [("aria-label", "AriaLabel")]),
        ("NTComponents.NTSkeleton", [("aria-hidden", "HideFromAssistiveTechnology")]),
        ("NTComponents.NTDialog", [("aria-labelledby", "Title or TitleContent"), ("aria-describedby", "SupportingText")]),
        ("NTComponents.NTNavigationRailItem", [
            ("aria-current", "Selected"),
            ("aria-disabled", "Disabled"),
            ("aria-label", "AriaLabel"),
            ("data-nt-navigation-rail-match", "Match"),
            ("data-nt-navigation-rail-selected", "Selected"),
            ("name", "ElementName"),
            ("tabindex", Nothing)
        ]),
        // NTLayoutComponentBase.BuildElementAttributes replaces these with ElementId/ElementTitle/ElementLang, or removes them when those are null.
        ("NTComponents.Layout.NTLayoutComponentBase", [("id", "ElementId"), ("title", "ElementTitle"), ("lang", "ElementLang")])
    ];

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(OwnedAttribute);
        foreach (var (component, owned) in _components) {
            registry.On(component, context => Analyze(context, owned));
        }
    }

    private static void Analyze(ComponentContext context, (string Attribute, string Instead)[] owned) {
        var frame = context.Frame;
        foreach (var (attribute, instead) in owned) {
            if (frame.Has(attribute)) {
                context.Report(OwnedAttribute, RenderFragmentChecks.MappedLocationOf(frame, attribute), frame.Type.Name, attribute, instead);
            }
        }
    }
}

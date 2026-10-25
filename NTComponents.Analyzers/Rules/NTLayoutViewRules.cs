namespace NTComponents.Analyzers;

/// <summary>Info rules for layout and placeholder components that ignore or clamp a parameter.</summary>
internal static class NTLayoutViewRules {
    private static readonly string[] OnThisPageParameters = ["OnThisPageLabel", "OnThisPageAriaLabel", "OnThisPageTitle", "OnThisPageLabelColor", "OnThisPageSelectorColor", "OnThisPageSelectedTextColor", "OnThisPageTextColor"];

    public static void Register(ComponentRuleRegistry registry) {
        registry.On("NTComponents.NTSkeleton", static context => {
            // NTSkeleton.EffectiveCornerRadius forces Full for circles.
            if (context.Frame.Has("CornerRadius") && context.Frame.GetEnumName("Shape", "Text") == "Circle") {
                context.Report(CommonRules.NoEffect, context.Frame.LocationOf("CornerRadius"), "NTSkeleton CornerRadius", "when Shape is Circle");
            }
        });

        registry.On("NTComponents.NTMultiPaneView", static context => {
            if (context.Frame.GetNumber("PaneCount") is { } count && count is < 1 or > 5) {
                context.Report(CommonRules.Adjusted, context.Frame.LocationOf("PaneCount"), "NTMultiPaneView PaneCount", count, count < 1 ? 1 : 5);
            }
        });

        registry.On("NTComponents.NTThemeToggle", static context => {
            if (context.Frame.GetBool("AllowThemeSelection", true) == false && context.Frame.GetBool("AllowContrastSelection", true) == false) {
                context.Report(CommonRules.NoEffect, context.Frame.LocationOf("AllowThemeSelection"), "NTThemeToggle", "when AllowThemeSelection and AllowContrastSelection are both false; it renders nothing");
            }
        });

        registry.On("NTComponents.NTContainerView", static context => {
            if (context.Frame.GetBool("EnableOnThisPageNavigation", true) == false) {
                foreach (var parameter in OnThisPageParameters.Where(context.Frame.Has)) {
                    context.Report(CommonRules.NoEffect, context.Frame.LocationOf(parameter), "NTContainerView " + parameter, "unless EnableOnThisPageNavigation is true");
                }
            }
        });
    }
}

using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Background, content color, and elevation checks shared by the Material 3 button family.</summary>
internal static class ButtonVariantChecks {

    /// <summary>
    ///     Text and outlined variants need a transparent BackgroundColor (except a selected outlined toggle); the others need a visible
    ///     one. TextColor cannot be transparent. Only Elevated may, and must, use a non-None Elevation. Omitted or null colors and
    ///     elevations fall back to valid defaults, so they are not checked.
    /// </summary>
    public static void Analyze(ComponentContext context, string? variant, bool isSelectedToggle, DiagnosticDescriptor opaqueBackground, DiagnosticDescriptor transparentBackground, DiagnosticDescriptor invisibleTextColor, DiagnosticDescriptor invalidElevation) {
        var frame = context.Frame;
        if (variant is not null && frame.GetEnumName("BackgroundColor") is { } background) {
            var needsTransparent = variant == "Text" || (variant == "Outlined" && !isSelectedToggle);
            if (needsTransparent && background != "Transparent") {
                context.Report(opaqueBackground, frame.LocationOf("BackgroundColor"), variant);
            }
            else if (!needsTransparent && background == "Transparent") {
                context.Report(transparentBackground, frame.LocationOf("BackgroundColor"), variant);
            }
        }

        if (frame.GetEnumName("TextColor") == "Transparent") {
            context.Report(invisibleTextColor, frame.LocationOf("TextColor"));
        }

        if (variant is not null && frame.GetEnumName("Elevation") is { } elevation && (variant == "Elevated") == (elevation == "None")) {
            context.Report(invalidElevation, frame.LocationOf("Elevation"), variant, elevation);
        }
    }

    /// <summary>True when IsToggleButton and Selected are both constant true.</summary>
    public static bool IsSelectedToggle(ComponentFrame frame) => frame.GetBool("IsToggleButton") == true && frame.GetBool("Selected") == true;
}

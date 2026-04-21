namespace NTComponents;

/// <summary>
///     Represents the Material 3 typography roles exposed by NTComponents.
/// </summary>
/// <remarks>
///     <para>
///         These values map directly to the <c>nt-*</c> typography utility classes emitted by <see cref="TnTMeasurements" />. Each role represents a complete typography recipe, not just a font size.
///     </para>
///     <para>A typography role defines the font reference, font weight, font size, line height, and letter spacing together so type remains consistent across the system.</para>
///     <para>
///         Use <see cref="NTTypographyExt.ToCssClass(NTTypography, bool)" /> to convert a role into its CSS class name. Pass <c>
///         emphasized: true</c> to target the emphasized variant when one is needed.
///     </para>
///     <para>
///         Display, headline, and title roles use the brand typeface token. Body and smaller label roles use the plain typeface token. Emphasized variants keep the same size, line height, and
///         tracking while increasing font weight.
///     </para>
///     <para>
///         Best practice: choose the typography role for visual hierarchy and the HTML element for document semantics. For example, an <c> h1</c> can use <see cref="DisplayLarge" /> or <see
///         cref="HeadlineLarge" /> depending on the visual need, while a <c> p</c> can use <see cref="BodyLarge" /> or <see cref="BodyMedium" />.
///     </para>
/// </remarks>
public enum NTTypography {

    /// <summary>
    ///     Largest display role for hero-level statements and highly prominent page titles.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 57px. Line height: 64px. Letter spacing: -0.25px. Typeface: brand. Default weight: 400. Emphasized
    ///         weight: 500.
    ///     </para>
    ///     <para>Do use this for the most prominent marketing or landing-page headlines. Do not use it for dense application chrome or repeated section headings.</para>
    ///     <para>
    ///         Best practices: reserve this for one primary hero statement per page or view, and pair it with the correct semantic heading level instead of relying on the class to provide document structure.
    ///     </para>
    /// </remarks>
    DisplayLarge,

    /// <summary>
    ///     Medium display role for large page-level statements that still need strong presence.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 45px. Line height: 52px. Letter spacing: 0px. Typeface: brand. Default weight: 400. Emphasized
    ///         weight: 500.
    ///     </para>
    ///     <para>Do use this for large hero copy or major page titles when <see cref="DisplayLarge" /> feels too dominant. Do not use it for ordinary card, dialog, or form headings.</para>
    ///     <para>
    ///         Best practices: use this when a screen still needs a strong page-opening statement but the layout cannot support the largest display role; keep it to top-level headings rather than
    ///         repeating it deep in the page.
    ///     </para>
    /// </remarks>
    DisplayMedium,

    /// <summary>
    ///     Smallest display role for prominent page headings in tighter layouts.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 36px. Line height: 44px. Letter spacing: 0px. Typeface: brand. Default weight: 400. Emphasized
    ///         weight: 500.
    ///     </para>
    ///     <para>Do use this when a layout needs display-level presence but has less space than a full hero treatment. Do not use it for standard section titles when a headline role is sufficient.</para>
    ///     <para>
    ///         Best practices: use this as the smallest display option before stepping down to headline roles, especially on tablet, mobile, or compact marketing layouts where display presence is
    ///         still needed.
    ///     </para>
    /// </remarks>
    DisplaySmall,

    /// <summary>
    ///     Largest headline role for major section headings and important content titles.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 32px. Line height: 40px. Letter spacing: 0px. Typeface: brand. Default weight: 400. Emphasized
    ///         weight: 500.
    ///     </para>
    ///     <para>Do use this for major sections, large dialogs, and editorial content headings. Do not use it when a smaller headline or title role would preserve hierarchy more clearly.</para>
    ///     <para>
    ///         Best
    ///         practices: use this for content hierarchy below the main page hero, such as primary section starts or oversized dialog titles, and avoid stacking several adjacent headline levels that
    ///         are visually too similar.
    ///     </para>
    /// </remarks>
    HeadlineLarge,

    /// <summary>
    ///     Medium headline role for section headings with clear prominence.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 28px. Line height: 36px. Letter spacing: 0px. Typeface: brand. Default weight: 400. Emphasized
    ///         weight: 500.
    ///     </para>
    ///     <para>Do use this for section headers, panel titles, and content group introductions. Do not use it where a title role would better match compact UI structure.</para>
    ///     <para>Best practices: treat this as a strong section heading for application pages and editorial blocks, especially when sections need clear separation but not display-level drama.</para>
    /// </remarks>
    HeadlineMedium,

    /// <summary>
    ///     Smallest headline role for compact but still prominent headings.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 24px. Line height: 32px. Letter spacing: 0px. Typeface: brand. Default weight: 400. Emphasized
    ///         weight: 500.
    ///     </para>
    ///     <para>Do use this for dialogs, cards, drawers, or page sections that need a clear heading without display-scale emphasis. Do not use it for inline labels or body copy.</para>
    ///     <para>
    ///         Best practices: use this as the default larger heading inside surfaces like dialogs, cards, and side panels where a heading should be obvious but compact enough to coexist with
    ///         controls and content.
    ///     </para>
    /// </remarks>
    HeadlineSmall,

    /// <summary>
    ///     Largest title role for medium-emphasis headings inside application surfaces.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 22px. Line height: 28px. Letter spacing: 0px. Typeface: brand. Default weight: 400. Emphasized
    ///         weight: 500.
    ///     </para>
    ///     <para>
    ///         Do use this for card titles, app bars, and component-level titles that should feel structured rather than editorial. Do not use it when a headline role is needed to establish larger hierarchy.
    ///     </para>
    ///     <para>Best practices: use this when a surface needs a stable title that anchors the component without overpowering surrounding content, such as page chrome, cards, or grouped containers.</para>
    /// </remarks>
    TitleLarge,

    /// <summary>
    ///     Medium title role for compact structural headings and emphasized control text.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 16px. Line height: 24px. Letter spacing: 0.15px. Typeface: brand. Default weight: 500. Emphasized
    ///         weight: 700.
    ///     </para>
    ///     <para>
    ///         Do use this for toolbar titles, grouped control headings, and compact structural text that needs stronger weight. Do not use it as a replacement for body text just to make paragraphs
    ///         appear bolder.
    ///     </para>
    ///     <para>
    ///         Best practices: use this for compact headings in UI-heavy layouts, including grouped settings sections, list headers, and top bars, especially where readability and weight matter more
    ///         than absolute size.
    ///     </para>
    /// </remarks>
    TitleMedium,

    /// <summary>
    ///     Smallest title role for dense UI headings and compact metadata labels.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 14px. Line height: 20px. Letter spacing: 0.10px. Typeface: brand. Default weight: 500. Emphasized
    ///         weight: 700.
    ///     </para>
    ///     <para>Do use this for small headings, grouped list metadata, and compact UI substructure. Do not use it where body or label text better reflects the content's role.</para>
    ///     <para>
    ///         Best practices: use this sparingly for the smallest heading layer before moving into label or body text, and prefer it when the text introduces a compact block rather than acting as an
    ///         inline control label.
    ///     </para>
    /// </remarks>
    TitleSmall,

    /// <summary>
    ///     Largest body role for standard reading text and long-form content.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 16px. Line height: 24px. Letter spacing: 0.50px. Typeface: plain. Default weight: 400. Emphasized
    ///         weight: 500.
    ///     </para>
    ///     <para>Do use this for primary paragraphs, rich text, and reading-oriented UI content. Do not use it for headings where hierarchy needs to come from more than a slightly larger size.</para>
    ///     <para>
    ///         Best
    ///         practices: make this the default for long-form reading, descriptive copy, and text-heavy surfaces because it provides the most comfortable reading rhythm in the scale.
    ///     </para>
    /// </remarks>
    BodyLarge,

    /// <summary>
    ///     Medium body role for supporting text and standard application copy.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 14px. Line height: 20px. Letter spacing: 0.25px. Typeface: plain. Default weight: 400. Emphasized
    ///         weight: 500.
    ///     </para>
    ///     <para>Do use this for secondary reading text, form help text, and most compact application body copy. Do not use it for primary page titles or actions that need label semantics.</para>
    ///     <para>
    ///         Best
    ///         practices: use this as the default body size for dense application UI where space is tighter than article-like layouts but users still need to read short paragraphs or supporting explanations.
    ///     </para>
    /// </remarks>
    BodyMedium,

    /// <summary>
    ///     Smallest body role for low-emphasis supporting text.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 12px. Line height: 16px. Letter spacing: 0.40px. Typeface: plain. Default weight: 400. Emphasized
    ///         weight: 500.
    ///     </para>
    ///     <para>
    ///         Do use this for captions, dense supporting text, and low-emphasis supplemental copy. Do not use it for main reading content where legibility and scannability need more breathing room.
    ///     </para>
    ///     <para>
    ///         Best practices: keep this for tertiary information such as timestamps, footnotes, captions, and low-priority support text, and avoid using it for anything users must read repeatedly or
    ///         at length.
    ///     </para>
    /// </remarks>
    BodySmall,

    /// <summary>
    ///     Largest label role for buttons, chips, tabs, and compact action text.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 14px. Line height: 20px. Letter spacing: 0.10px. Typeface: brand. Default weight: 500. Emphasized
    ///         weight: 700.
    ///     </para>
    ///     <para>Do use this for interactive labels and compact UI actions where text needs to stay sturdy and legible. Do not use it for paragraph-style body content.</para>
    ///     <para>Best practices: use this for buttons, chips, tabs, segmented controls, and other interactive text where clarity under tight spacing matters more than reading comfort.</para>
    /// </remarks>
    LabelLarge,

    /// <summary>
    /// Medium label role for dense controls and secondary action labels.
    /// </summary>
    /// <remarks>
    /// <para> Size: 12px. Line height: 16px. Letter spacing: 0.50px. Typeface: plain. Default weight: 500. Emphasized
    /// weight: 700. </para> <para> Do use this for compact control labels, dense metadata, and supporting action text.
    /// Do not use it as a substitute for body text when users are expected to read full sentences comfortably. </para>
    /// <para> Best practices: use this where controls or metadata must stay compact, such as form affordances, small
    /// data labels, and dense utility actions, while keeping full explanatory text on a body role. </para>
    /// </remarks>
    LabelMedium,

    /// <summary>
    ///     Smallest label role for the densest UI labels and tiny metadata.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Size: 11px. Line height: 16px. Letter spacing: 0.50px. Typeface: plain. Default weight: 500. Emphasized
    ///         weight: 700.
    ///     </para>
    ///     <para>Do use this for very compact badges, small annotations, and tiny UI labels with limited text. Do not use it for body copy or important content that needs comfortable readability.</para>
    ///     <para>
    ///         Best practices: reserve this for the tightest UI cases only, such as micro-labels, tiny badges, and small counters, and prefer a larger label or body role whenever readability is even
    ///         slightly in doubt.
    ///     </para>
    /// </remarks>
    LabelSmall
}

/// <summary>
///     Provides extension methods for <see cref="NTTypography" />.
/// </summary>
public static class NTTypographyExt {

    /// <summary>
    ///     Converts the typography role to its corresponding CSS utility class.
    /// </summary>
    /// <param name="typography">The typography role to convert.</param>
    /// <param name="emphasized">When <see langword="true" />, returns the emphasized class variant such as <c>nt-title-medium-emphasized</c> instead of the baseline class.</param>
    /// <returns>The CSS utility class for the typography role.</returns>
    /// <remarks>
    ///     <para>This method is intended for direct use in Razor markup so callers can stay on the tokenized type scale without remembering the raw utility class names.</para>
    ///     <para>
    ///         Example: <c> NTTypography.DisplayLarge.ToCssClass()</c> returns <c>nt-display-large</c>. Example: <c> NTTypography.TitleMedium.ToCssClass(emphasized: true)</c> returns <c>nt-title-medium-emphasized</c>.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="typography" /> is not a valid <see cref="NTTypography" /> value.</exception>
    public static string ToCssClass(this NTTypography typography, bool emphasized = false) {
        var cssClass = typography switch {
            NTTypography.DisplayLarge => "nt-display-large",
            NTTypography.DisplayMedium => "nt-display-medium",
            NTTypography.DisplaySmall => "nt-display-small",
            NTTypography.HeadlineLarge => "nt-headline-large",
            NTTypography.HeadlineMedium => "nt-headline-medium",
            NTTypography.HeadlineSmall => "nt-headline-small",
            NTTypography.TitleLarge => "nt-title-large",
            NTTypography.TitleMedium => "nt-title-medium",
            NTTypography.TitleSmall => "nt-title-small",
            NTTypography.BodyLarge => "nt-body-large",
            NTTypography.BodyMedium => "nt-body-medium",
            NTTypography.BodySmall => "nt-body-small",
            NTTypography.LabelLarge => "nt-label-large",
            NTTypography.LabelMedium => "nt-label-medium",
            NTTypography.LabelSmall => "nt-label-small",
            _ => throw new ArgumentOutOfRangeException(nameof(typography), typography, null)
        };

        return emphasized ? $"{cssClass}-emphasized" : cssClass;
    }
}
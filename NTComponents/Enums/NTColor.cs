using System.ComponentModel.DataAnnotations;

namespace NTComponents;

/// <summary>
///     Represents the Material 3 color roles exposed by NTComponents.
/// </summary>
/// <remarks>
///     <para><c>Surface</c> roles are intended for backgrounds and other large, low-emphasis areas of the UI.</para>
///     <para><c>Primary</c>, <c>Secondary</c>, and <c>Tertiary</c> are accent roles that should be assigned based on the importance of an element and the amount of emphasis it needs.</para>
///     <para>
///         <c>Primary</c> is for the most important actions and elements, <c>Secondary</c> is for supporting elements that do not need immediate attention, and <c>Tertiary</c> is for smaller accents
///         that need distinct emphasis without becoming primary actions.
///     </para>
///     <para>
///         For accent and state roles, names ending in <c>Container</c> indicate fill colors for elements such as buttons, chips, or other foreground containers, and should not be used for text or
///         icons directly.
///     </para>
///     <para>Roles starting with <c>On</c> are the text or icon colors intended to appear on top of their paired parent role, such as <see cref="OnPrimary" /> on <see cref="Primary" />.</para>
///     <para>Roles ending in <c>Variant</c> provide a lower-emphasis alternative to their base role.</para>
///     <para><c>SurfaceContainer</c> is the default container role, while the other <c>SurfaceContainer*</c> roles help create hierarchy and nested containers in larger layouts.</para>
///     <para>A common pairing uses <see cref="Surface" /> for the main background area and <see cref="SurfaceContainer" /> for a navigation or grouped container area.</para>
///     <para>Text and icons on these backgrounds typically use <see cref="OnSurface" /> for standard emphasis and <see cref="OnSurfaceVariant" /> for lower emphasis.</para>
///     <para>
///         Color-role mapping, especially for surface regions, should remain consistent across window size classes. For example, a body area should continue using <see cref="Surface" /> and a
///         navigation area should continue using <see cref="SurfaceContainer" /> on both mobile and larger layouts.
///     </para>
///     <para>
///         Neutral components such as navigation bars, menus, and dialogs are typically mapped to specific <c>SurfaceContainer*</c> roles by default, though those mappings can be remapped to suit
///         product needs.
///     </para>
///     <para>
///         Add-on roles such as the fixed accent colors are intended for products that need extra control over color behavior. If that need is unclear, the standard roles are usually the better choice.
///     </para>
///     <para>Fixed accent roles keep the same tone across light and dark themes, which can introduce contrast issues. Avoid using them where contrast is required for readability or target definition.</para>
///     <para>When accent colors must preserve contrast, prefer the standard <c> Primary</c>, <c>Secondary</c>, and <c>Tertiary</c> roles instead of the fixed accent roles.</para>
///     <para>
///         The add-on surface roles <see cref="SurfaceDim" /> and <see cref="SurfaceBright" /> preserve their relative brightness across light and dark themes, unlike <see cref="Surface" />, which
///         flips from brightest in light theme to dimmest in dark theme.
///     </para>
/// </remarks>
public enum NTColor {

    /// <summary>
    ///     Transparent color, allows underlying elements to show through.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this when the underlying surface or container should remain visible and no explicit fill is needed. Do not use this when a readable foreground depends on a dedicated semantic background.
    ///     </para>
    /// </remarks>
    Transparent,

    /// <summary>
    ///     High-emphasis primary role for prominent text, icons, and fills placed against a surface.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for the most prominent components across the UI, such as high-emphasis actions and active states. Do not use it for recessive or supporting elements that should read as
    ///         secondary emphasis.
    ///     </para>
    /// </remarks>
    Primary,

    /// <summary>
    ///     Tint applied to elevated surfaces to reinforce depth.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this to subtly tint elevated surfaces so their depth remains legible against surrounding surfaces. Do not use it as a general-purpose accent or text color.</para>
    /// </remarks>
    [Display(Name = "Surface Tint")]
    SurfaceTint,

    /// <summary>
    ///     Text and icon color used on top of <see cref="Primary" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="Primary" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Primary")]
    OnPrimary,

    /// <summary>
    ///     Standout fill color against a surface for key components such as FABs and other prominent containers.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this when a prominent component needs a standout container fill against the surrounding surface. Do not use it for text or icons directly.</para>
    /// </remarks>
    [Display(Name = "Primary Container")]
    PrimaryContainer,

    /// <summary>
    ///     Text and icon color used on top of <see cref="PrimaryContainer" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="PrimaryContainer" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Primary Container")]
    OnPrimaryContainer,

    /// <summary>
    ///     Lower-emphasis secondary role for fills, text, and icons placed against a surface.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for less prominent components, such as supporting actions, filter chips, or selected states. Do not use it where the strongest emphasis in the interface is required.</para>
    /// </remarks>
    Secondary,

    /// <summary>
    ///     Text and icon color used on top of <see cref="Secondary" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="Secondary" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Secondary")]
    OnSecondary,

    /// <summary>
    ///     Less prominent fill color against a surface for recessive components such as tonal buttons.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for container fills that should feel quieter than primary containers, such as tonal buttons. Do not use it for text or icons directly.</para>
    /// </remarks>
    [Display(Name = "Secondary Container")]
    SecondaryContainer,

    /// <summary>
    ///     Text and icon color used on top of <see cref="SecondaryContainer" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="SecondaryContainer" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Secondary Container")]
    OnSecondaryContainer,

    /// <summary>
    ///     Complementary tertiary role for fills, text, and icons placed against a surface.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for contrasting accents that balance primary and secondary roles or emphasize a smaller element. Do not use it as a substitute for primary emphasis when the element is the
    ///         main action.
    ///     </para>
    /// </remarks>
    Tertiary,

    /// <summary>
    ///     Text and icon color used on top of <see cref="Tertiary" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="Tertiary" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Tertiary")]
    OnTertiary,

    /// <summary>
    ///     Complementary container color against a surface for components such as input fields.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this when a component needs a complementary container fill that stands apart from primary and secondary accents, such as an emphasized input field. Do not use it for text or
    ///         icons directly.
    ///     </para>
    /// </remarks>
    [Display(Name = "Tertiary Container")]
    TertiaryContainer,

    /// <summary>
    ///     Text and icon color used on top of <see cref="TertiaryContainer" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="TertiaryContainer" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Tertiary Container")]
    OnTertiaryContainer,

    /// <summary>
    ///     Attention-grabbing error role for fills, icons, and text placed against a surface to indicate urgency.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this to communicate error states, such as invalid input, where the UI needs to call attention to a problem. Do not repurpose it for non-error accents. Error roles remain static
    ///         in dynamic color schemes, while still adapting between light and dark themes.
    ///     </para>
    /// </remarks>
    Error,

    /// <summary>
    ///     Text and icon color used on top of <see cref="Error" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="Error" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Error")]
    OnError,

    /// <summary>
    ///     Attention-grabbing error container fill placed against a surface.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for error containers that need to stand out from the surrounding surface. Do not use it for text or icons directly.</para>
    /// </remarks>
    [Display(Name = "Error Container")]
    ErrorContainer,

    /// <summary>
    ///     Text and icon color used on top of <see cref="ErrorContainer" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="ErrorContainer" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Error Container")]
    OnErrorContainer,

    /// <summary>
    ///     Background role used for the main app backdrop behind surfaces and content.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for the main app backdrop behind surfaces and content. Do not use it as the default color for component containers when a surface role is more appropriate.</para>
    /// </remarks>
    Background,

    /// <summary>
    ///     Text and icon color used on top of <see cref="Background" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="Background" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Background")]
    OnBackground,

    /// <summary>
    ///     Default surface role for neutral backgrounds.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this as the default neutral background for body areas and other large surface regions. Do not swap it across window size classes when the same layout region should keep the same
    ///         color mapping.
    ///     </para>
    /// </remarks>
    Surface,

    /// <summary>
    ///     Text and icon color used on top of any surface or surface container color.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for standard-emphasis text and icons on surfaces and surface containers. Do not replace it with accent roles unless the content needs accent emphasis.</para>
    /// </remarks>
    [Display(Name = "On Surface")]
    OnSurface,

    /// <summary>
    ///     Lower-emphasis alternative to <see cref="Surface" /> for backgrounds and large areas.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for alternate low-emphasis surfaces when a neutral background needs subtle differentiation. Do not treat it as the default foreground text color.</para>
    /// </remarks>
    [Display(Name = "Surface Variant")]
    SurfaceVariant,

    /// <summary>
    ///     Lower-emphasis text and icon color used on top of any surface or surface container color.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for lower-emphasis text and icons on surfaces and surface containers. Do not use it where standard-emphasis readability is required.</para>
    /// </remarks>
    [Display(Name = "On Surface Variant")]
    OnSurfaceVariant,

    /// <summary>
    ///     Outline color for important boundaries against a surface, such as a text field outline. Do not use this role for dividers or multi-element components such as cards. Use this role, or
    ///     another color that provides 3:1 contrast with the surface, when defining target boundaries or visual hierarchy.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for important boundaries and target definition against a surface. Do not use it for dividers or card-like multi-element components where <see cref="OutlineVariant" /> is
    ///         the better fit.
    ///     </para>
    /// </remarks>
    Outline,

    /// <summary>
    ///     Lower-emphasis outline color for decorative elements such as dividers, or when other elements already provide sufficient contrast. Use this role instead of <see cref="Outline" /> for
    ///     dividers and multi-element components such as cards. Do not use this role to create visual hierarchy or define the visual boundary of targets unless the target already contains elements,
    ///     such as text or icons, that provide sufficient contrast.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for decorative separators, card borders, and low-emphasis target borders when internal text or icons already provide contrast. Do not use it as the primary boundary color
    ///         for targets or to create hierarchy by itself.
    ///     </para>
    /// </remarks>
    [Display(Name = "Outline Variant")]
    OutlineVariant,

    /// <summary>
    ///     Shadow color, used for shadows to indicate elevation.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this to express elevation through shadows. Do not use it as a substitute for surface, outline, or scrim roles.</para>
    /// </remarks>
    Shadow,

    /// <summary>
    ///     Scrim color, used for dimming backgrounds for modals and dialogs. Scrims use the scrim color role at an opacity of 32%.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this to dim background content behind modal surfaces and dialogs. Do not use it as a standard component background or container fill.</para>
    /// </remarks>
    Scrim,

    /// <summary>
    ///     Background fill for elements that contrast with the surrounding surface-based UI.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this selectively when an element should appear as the inverse of the surrounding surface-based UI. Do not use it as the default surface across a layout.</para>
    /// </remarks>
    [Display(Name = "Inverse Surface")]
    InverseSurface,

    /// <summary>
    ///     Text and icon color used on top of <see cref="InverseSurface" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="InverseSurface" />. Do not use it on regular surfaces or non-inverse containers.</para>
    /// </remarks>
    [Display(Name = "Inverse On Surface")]
    InverseOnSurface,

    /// <summary>
    ///     Actionable accent color, such as for text buttons, used against <see cref="InverseSurface" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for actionable accents placed on <see cref="InverseSurface" />, such as text buttons. Do not use it as a general replacement for <see cref="Primary" /> on regular surfaces.
    ///     </para>
    /// </remarks>
    [Display(Name = "Inverse Primary")]
    InversePrimary,

    /// <summary>
    ///     Primary fixed fill color used against a surface when the same tone should be maintained in both light and dark themes. Avoid using fixed roles where contrast is required.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this when a primary accent container must keep the same tone in light and dark themes. Do not use it where contrast is critical; prefer <see cref="PrimaryContainer" /> or <see
    ///         cref="Primary" /> when adaptive contrast matters.
    ///     </para>
    /// </remarks>
    [Display(Name = "Primary Fixed")]
    PrimaryFixed,

    /// <summary>
    ///     Text and icon color used on top of <see cref="PrimaryFixed" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="PrimaryFixed" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Primary Fixed")]
    OnPrimaryFixed,

    /// <summary>
    ///     More emphasized fixed primary fill with a deeper tone than <see cref="PrimaryFixed" />. Avoid using fixed roles where contrast is required.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this when the fixed primary tone needs stronger emphasis than <see cref="PrimaryFixed" />. Do not use it where adaptive contrast is required.</para>
    /// </remarks>
    [Display(Name = "Primary Fixed Dim")]
    PrimaryFixedDim,

    /// <summary>
    ///     Lower-emphasis text and icon color used on top of <see cref="PrimaryFixed" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for lower-emphasis text and icons displayed against <see cref="PrimaryFixed" />. Do not use it where standard-emphasis readability is required.</para>
    /// </remarks>
    [Display(Name = "On Primary Fixed Variant")]
    OnPrimaryFixedVariant,

    /// <summary>
    ///     Secondary fixed fill color used against a surface when the same tone should be maintained in both light and dark themes. Avoid using fixed roles where contrast is required.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this when a secondary accent container must keep the same tone in light and dark themes. Do not use it where contrast is critical; prefer <see cref="SecondaryContainer" /> or
    ///         <see cref="Secondary" /> when adaptive contrast matters.
    ///     </para>
    /// </remarks>
    [Display(Name = "Secondary Fixed")]
    SecondaryFixed,

    /// <summary>
    ///     Text and icon color used on top of <see cref="SecondaryFixed" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="SecondaryFixed" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Secondary Fixed")]
    OnSecondaryFixed,

    /// <summary>
    ///     More emphasized fixed secondary fill with a deeper tone than <see cref="SecondaryFixed" />. Avoid using fixed roles where contrast is required.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this when the fixed secondary tone needs stronger emphasis than <see cref="SecondaryFixed" />. Do not use it where adaptive contrast is required.</para>
    /// </remarks>
    [Display(Name = "Secondary Fixed Dim")]
    SecondaryFixedDim,

    /// <summary>
    ///     Lower-emphasis text and icon color used on top of <see cref="SecondaryFixed" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for lower-emphasis text and icons displayed against <see cref="SecondaryFixed" />. Do not use it where standard-emphasis readability is required.</para>
    /// </remarks>
    [Display(Name = "On Secondary Fixed Variant")]
    OnSecondaryFixedVariant,

    /// <summary>
    ///     Tertiary fixed fill color used against a surface when the same tone should be maintained in both light and dark themes. Avoid using fixed roles where contrast is required.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this when a tertiary accent container must keep the same tone in light and dark themes. Do not use it where contrast is critical; prefer <see cref="TertiaryContainer" /> or <see
    ///         cref="Tertiary" /> when adaptive contrast matters.
    ///     </para>
    /// </remarks>
    [Display(Name = "Tertiary Fixed")]
    TertiaryFixed,

    /// <summary>
    ///     Text and icon color used on top of <see cref="TertiaryFixed" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="TertiaryFixed" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Tertiary Fixed")]
    OnTertiaryFixed,

    /// <summary>
    ///     More emphasized fixed tertiary fill with a deeper tone than <see cref="TertiaryFixed" />. Avoid using fixed roles where contrast is required.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this when the fixed tertiary tone needs stronger emphasis than <see cref="TertiaryFixed" />. Do not use it where adaptive contrast is required.</para>
    /// </remarks>
    [Display(Name = "Tertiary Fixed Dim")]
    TertiaryFixedDim,

    /// <summary>
    ///     Lower-emphasis text and icon color used on top of <see cref="TertiaryFixed" />.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for lower-emphasis text and icons displayed against <see cref="TertiaryFixed" />. Do not use it where standard-emphasis readability is required.</para>
    /// </remarks>
    [Display(Name = "On Tertiary Fixed Variant")]
    OnTertiaryFixedVariant,

    /// <summary>
    ///     Dimmest add-on surface color in both light and dark themes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this when a surface should remain the dimmest neutral region in both light and dark themes. Do not use it as a drop-in replacement for <see cref="Surface" /> unless that fixed
    ///         relative brightness is intentional.
    ///     </para>
    /// </remarks>
    [Display(Name = "Surface Dim")]
    SurfaceDim,

    /// <summary>
    ///     Brightest add-on surface color in both light and dark themes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this when a surface should remain the brightest neutral region in both light and dark themes. Do not use it as a drop-in replacement for <see cref="Surface" /> unless that fixed
    ///         relative brightness is intentional.
    ///     </para>
    /// </remarks>
    [Display(Name = "Surface Bright")]
    SurfaceBright,

    /// <summary>
    ///     Lowest-emphasis surface container color used to create the deepest level in a nested container hierarchy.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for the deepest nested neutral containers when hierarchy should be subtle. Do not use it as the default container role when <see cref="SurfaceContainer" /> is sufficient.</para>
    /// </remarks>
    [Display(Name = "Surface Container Lowest")]
    SurfaceContainerLowest,

    /// <summary>
    ///     Low-emphasis surface container color used to separate nested containers with subtle hierarchy.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this to separate nested neutral containers with low emphasis. Do not use it to create strong visual separation when a higher-emphasis surface container role is needed.</para>
    /// </remarks>
    [Display(Name = "Surface Container Low")]
    SurfaceContainerLow,

    /// <summary>
    ///     Default surface container color for components such as cards, sheets, and dialogs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this as the default neutral container color for cards, sheets, dialogs, and navigation areas. Do not remap equivalent layout regions across size classes unless the product
    ///         intentionally changes the color system.
    ///     </para>
    /// </remarks>
    [Display(Name = "Surface Container")]
    SurfaceContainer,

    /// <summary>
    ///     High-emphasis surface container color used to raise emphasis within a nested container hierarchy.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this when a nested neutral container needs more separation than <see cref="SurfaceContainer" />. Do not use it where a lower-emphasis container would preserve better visual balance.
    ///     </para>
    /// </remarks>
    [Display(Name = "Surface Container High")]
    SurfaceContainerHigh,

    /// <summary>
    ///     Highest-emphasis surface container color used for the strongest separation in nested layouts.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for the strongest neutral separation in nested layouts. Do not default to it for every container, or hierarchy between container levels will collapse.</para>
    /// </remarks>
    [Display(Name = "Surface Container Highest")]
    SurfaceContainerHighest,

    /// <summary>
    ///     Success color, used for successful states and confirmations.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this to communicate successful states and confirmations. Do not use it for neutral or unrelated accent styling.</para>
    /// </remarks>
    Success,

    /// <summary>
    ///     Text and icon color used on top of <see cref="Success" /> fills.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="Success" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Success")]
    OnSuccess,

    /// <summary>
    ///     Success container fill for lower-emphasis success elements.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for success-related containers that need lower emphasis than <see cref="Success" />. Do not use it for text or icons directly.</para>
    /// </remarks>
    [Display(Name = "Success Container")]
    SuccessContainer,

    /// <summary>
    ///     Text and icon color used on top of <see cref="SuccessContainer" /> fills.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="SuccessContainer" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Success Container")]
    OnSuccessContainer,

    /// <summary>
    ///     Warning color, used for warnings and cautionary messages.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this to communicate warnings and cautionary states. Do not use it for neutral or unrelated accent styling.</para>
    /// </remarks>
    Warning,

    /// <summary>
    ///     Text and icon color used on top of <see cref="Warning" /> fills.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="Warning" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Warning")]
    OnWarning,

    /// <summary>
    ///     Warning container fill for lower-emphasis warning elements.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for warning-related containers that need lower emphasis than <see cref="Warning" />. Do not use it for text or icons directly.</para>
    /// </remarks>
    [Display(Name = "Warning Container")]
    WarningContainer,

    /// <summary>
    ///     Text and icon color used on top of <see cref="WarningContainer" /> fills.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="WarningContainer" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Warning Container")]
    OnWarningContainer,

    /// <summary>
    ///     Information color, used for informational messages and elements.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this to communicate informational states and messages. Do not use it for neutral or unrelated accent styling.</para>
    /// </remarks>
    Info,

    /// <summary>
    ///     Text and icon color used on top of <see cref="Info" /> fills.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="Info" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Info")]
    OnInfo,

    /// <summary>
    ///     Info container fill for lower-emphasis informational elements.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for informational containers that need lower emphasis than <see cref="Info" />. Do not use it for text or icons directly.</para>
    /// </remarks>
    [Display(Name = "Info Container")]
    InfoContainer,

    /// <summary>
    ///     Text and icon color used on top of <see cref="InfoContainer" /> fills.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="InfoContainer" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Info Container")]
    OnInfoContainer,

    /// <summary>
    ///     Assert color, used for assertion messages and indicators.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for assertion-related diagnostics and indicators when that semantic meaning exists in the product. Do not use it as a generic accent if the product does not expose
    ///         assertion states.
    ///     </para>
    /// </remarks>
    Assert,

    /// <summary>
    ///     Text and icon color used on top of <see cref="Assert" /> fills.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="Assert" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Assert")]
    OnAssert,

    /// <summary>
    /// Assert container fill for lower-emphasis assertion elements.
    /// </summary>
    /// <remarks>
    /// <para> Do use this for assertion-related containers that need lower emphasis than <see cref="Assert" />. Do not
    /// use it for text or icons directly.</para>
    /// </remarks>
    [Display(Name = "Assert Container")]
    AssertContainer,

    /// <summary>
    ///     Text and icon color used on top of <see cref="AssertContainer" /> fills.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for text and icons displayed against <see cref="AssertContainer" />. Do not use it on unrelated surfaces or containers.</para>
    /// </remarks>
    [Display(Name = "On Assert Container")]
    OnAssertContainer
}

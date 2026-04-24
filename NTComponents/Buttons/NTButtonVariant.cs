namespace NTComponents;

/// <summary>
///     Defines the Material 3 visual variants available for <see cref="NTButton" />.
/// </summary>
/// <remarks>
///     <para>
///         Material 3 button variants communicate action priority through color, outline, and elevation. Choose the lowest-emphasis variant that still makes the action discoverable, and avoid
///         filling a screen with competing buttons. When several actions are available, reserve the strongest variant for the action that most clearly advances or completes the current flow.
///     </para>
///     <para>
///         Use labels that remain brief, visible, and on one line. Outlined and text variants depend heavily on color contrast to read as buttons, so avoid placing them beside visually similar
///         elements unless the surrounding context still makes the action clear.
///     </para>
/// </remarks>
public enum NTButtonVariant {

    /// <summary>
    ///     Elevated button with a low surface container and shadow.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use elevated buttons only when the action needs separation from a visually prominent background or surface. Treat this variant like a tonal button with a shadow, not as a general
    ///         high-emphasis replacement for <see cref="Filled" />.
    ///     </para>
    ///     <para>
    ///         Do not use elevated buttons just to make an action feel more important. Elevation increases visual emphasis and should stay rare; prefer <see cref="Filled" /> for the main high-emphasis
    ///         action.
    ///     </para>
    /// </remarks>
    Elevated,

    /// <summary>
    ///     Filled button used for high-emphasis actions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use filled buttons for important, final actions that complete or strongly advance a flow, such as Save, Join now, Confirm, or Make payment. This is the highest-emphasis standard button
    ///         style after FABs.
    ///     </para>
    ///     <para>
    ///         Do not use filled buttons for every available action. Because the filled style has strong visual impact, use it sparingly, ideally for only one primary action on a page or region.
    ///     </para>
    /// </remarks>
    Filled,

    /// <summary>
    ///     Tonal button using the secondary color container mapping.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use tonal buttons for lower-priority actions that still need more emphasis than an outline provides, such as a Next action in an onboarding flow. Tonal buttons use secondary color
    ///         mapping and sit between filled and outlined buttons in emphasis.
    ///     </para>
    ///     <para>
    ///         Do not use tonal buttons for the single most important final action when a filled button is available, and do not use them for very low-priority or text-heavy actions where
    ///         <see cref="Text" /> is enough.
    ///     </para>
    /// </remarks>
    Tonal,

    /// <summary>
    ///     Outlined button for medium-emphasis actions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use outlined buttons for medium-emphasis actions that are important but not the primary product action. They pair well with filled buttons for alternative or supporting actions.
    ///     </para>
    ///     <para>
    ///         Do not use outlined buttons on visually prominent image or video backgrounds unless the label remains legible, and use caution near chips or large text because the outline style can
    ///         look visually similar. Prefer <see cref="Filled" /> or <see cref="Tonal" /> when the action needs clearer separation.
    ///     </para>
    /// </remarks>
    Outlined,

    /// <summary>
    ///     Text button for the lowest-emphasis actions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use text buttons for the lowest-priority actions, especially when presenting multiple options or actions inside cards, dialogs, and snackbars. Their lack of a visible resting container
    ///         helps nearby content stay visually dominant.
    ///     </para>
    ///     <para>
    ///         Do not use text buttons on visually prominent backgrounds such as images or videos, do not underline them as links, and do not use them for toggle buttons. Because the container is
    ///         invisible at rest, the label color must remain clearly recognizable as an action.
    ///     </para>
    /// </remarks>
    Text
}

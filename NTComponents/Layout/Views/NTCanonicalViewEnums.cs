namespace NTComponents;

/// <summary>
///     View strategy for list-detail canonical views.
/// </summary>
/// <remarks>
///     Use list-detail views when a selectable collection drives one detail surface. Keep list selection and detail content semantically related, and avoid using this view as a general two-column
///     page layout.
/// </remarks>
public enum NTListDetailViewMode {

    /// <summary>
    ///     Follow Material 3 defaults: one pane on compact and medium widths, two panes on expanded and larger widths.
    /// </summary>
    /// <remarks>
    ///     <para>Best practice: prefer this mode for most list-detail pages so the browser and component can adapt to available width.</para>
    ///     <para>
    ///         Do: keep the list useful as navigation and keep the selected detail meaningful on its own.
    ///     </para>
    ///     <para>Do not: force this mode when the detail pane is required for every task on compact screens; use <see cref="SinglePane" /> or a dedicated workflow instead.</para>
    /// </remarks>
    Auto,

    /// <summary>
    ///     Keep a single-pane presentation at all widths.
    /// </summary>
    /// <remarks>
    ///     <para>Best practice: use when focus, reading order, or task completion is better served by one pane at a time.</para>
    ///     <para>Do: provide a clear way back to the list when detail content replaces it.</para>
    ///     <para>
    ///         Do
    ///         not: use only to avoid designing a responsive two-pane state; expanded screens should normally benefit from preserved list context.
    ///     </para>
    /// </remarks>
    SinglePane,

    /// <summary>
    ///     Use a two-pane presentation when the available width can support it.
    /// </summary>
    /// <remarks>
    ///     <para>Best practice: use when the list and detail must remain visible together for comparison or repeated scanning.</para>
    ///     <para>
    ///         Do: ensure both panes have bounded content and can shrink without horizontal overflow.
    ///     </para>
    ///     <para>Do not: place unrelated tools or independent pages in the detail pane; use a supporting-pane or multi-pane view instead.</para>
    /// </remarks>
    TwoPane
}

/// <summary>
/// View strategy for supporting-pane canonical views.
/// </summary>
/// <remarks>
/// Use supporting-pane views when one primary surface owns the task and a secondary pane provides related context,
/// tools, metadata, or collaboration. Do not use this view for peer content columns; use a multi-pane or feed view when
/// panes have equal importance.
/// </remarks>
public enum NTSupportingPaneViewMode {

    /// <summary>
    ///     Stack panes on compact widths, split them evenly on medium widths, then give the primary pane approximately 70% of the available pane space on expanded and larger widths.
    /// </summary>
    /// <remarks>
    ///     <para>Best practice: prefer this mode when supporting content remains useful at all sizes.</para>
    ///     <para>Do: keep the primary pane first in source order and make the supporting pane supplemental.</para>
    ///     <para>Do not: put required form fields or blocking actions exclusively in the supporting pane unless compact users can still complete the task.</para>
    /// </remarks>
    Auto,

    /// <summary>
    ///     Keep the supporting pane stacked below the primary pane at all widths.
    /// </summary>
    /// <remarks>
    ///     <para>Best practice: use when the supporting content should read as a continuation of the primary content instead of a persistent side panel.</para>
    ///     <para>
    ///         Do: choose this mode for narrative, review, or low-frequency supporting content.
    ///     </para>
    ///     <para>Do not: use it for dense productivity views where expanded screens benefit from side-by-side context.</para>
    /// </remarks>
    Stacked,

    /// <summary>
    ///     Hide the supporting pane on small screens and reveal it at expanded width.
    /// </summary>
    /// <remarks>
    ///     <para>Best practice: use when supporting content is helpful but not necessary on compact or medium screens.</para>
    ///     <para>The supporting pane is hidden below the Material expanded breakpoint and is shown at 840px and wider.</para>
    ///     <para>Do: ensure the primary pane is fully usable without the supporting pane.</para>
    ///     <para>Do not: hide critical navigation, validation, required controls, or status messages in the supporting pane.</para>
    /// </remarks>
    HideOnSmallScreens
}

/// <summary>
///     Minimum pane width token for multi-pane views.
/// </summary>
/// <remarks>
///     Use these fixed tokens instead of arbitrary CSS lengths so the multi-pane view can generate all wrapping behavior in
///     static SCSS. This keeps the layout SSR-friendly, avoids per-instance style tags, and aligns pane widths with
///     NTComponents size variables.
/// </remarks>
public enum NTMultiPaneViewMinPaneWidth {

    /// <summary>
    ///     Compact 240px pane width for dense, text-light dashboard panes.
    /// </summary>
    /// <remarks>
    ///     Best practice: use only when pane content remains readable at compact widths. Do not use for forms, tables, or
    ///     content with long unbroken values.
    /// </remarks>
    Compact,

    /// <summary>
    ///     Medium 320px pane width for the default multi-pane presentation.
    /// </summary>
    /// <remarks>
    ///     Best practice: prefer this value for general summary panes and operational cards.
    /// </remarks>
    Medium,

    /// <summary>
    ///     Large 360px pane width for richer panes that need more breathing room.
    /// </summary>
    /// <remarks>
    ///     Best practice: use when pane content includes denser metadata, short forms, or repeated rows.
    /// </remarks>
    Large,

    /// <summary>
    ///     Extra-large 412px pane width for content-heavy panes.
    /// </summary>
    /// <remarks>
    ///     Best practice: use sparingly because it reduces the number of panes that can share a row on common desktop widths.
    /// </remarks>
    ExtraLarge
}

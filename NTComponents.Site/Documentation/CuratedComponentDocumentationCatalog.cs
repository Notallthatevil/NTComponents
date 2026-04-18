namespace NTComponents.Site.Documentation;

/// <summary>
/// Provides first-pass curated metadata for component documentation pages.
/// </summary>
public static class CuratedComponentDocumentationCatalog {

    /// <summary>
    /// Gets every Razor component type that should receive a documentation page.
    /// </summary>
    public static IReadOnlyList<string> ComponentTypeNames { get; } = [
        "TnTAccordion",
        "TnTAccordionChild",
        "_TnTAccordionChildRender",
        "TnTAnimation",
        "TnTNavLink",
        "TnTBadge",
        "TnTButton",
        "TnTFabButton",
        "TnTFabContainer",
        "TnTImageButton",
        "NTButtonGroup",
        "TnTCard",
        "TnTCarousel",
        "TnTCarouselItem",
        "TnTChip",
        "BasicConfirmationDialog",
        "TnTDialog",
        "TnTDivider",
        "NTRichTextEditor",
        "TnTMarkdownEditor",
        "EditorToolIframeButton",
        "EditorToolIframePanel",
        "EditorToolImageButton",
        "EditorToolImagePanel",
        "EditorToolLinkButton",
        "EditorToolLinkPanel",
        "EditorToolTableButton",
        "EditorToolTablePanel",
        "EditorToolTextColorButton",
        "EditorToolTextColorPanel",
        "TnTInputBase",
        "TnTInputCheckbox",
        "TnTInputCurrency",
        "NTInputDateTime",
        "NTInputFile",
        "NTInputSelect",
        "TnTInputFile",
        "TnTInputRadio",
        "TnTInputRadioGroup",
        "TnTInputSelect",
        "TnTInputSwitch",
        "TnTInputText",
        "TnTInputTextArea",
        "TnTDataGrid",
        "TnTDataGridBody",
        "TnTDataGridBodyCell",
        "TnTDataGridBodyRow",
        "TnTDataGridBodyRowEmpty",
        "TnTDataGridHeaderCell",
        "TnTDataGridHeaderRow",
        "TnTDataGridVirtualizedBody",
        "TnTPaginationButtons",
        "TnTColumnBase",
        "TnTPropertyColumn",
        "TnTTemplateColumn",
        "TnTLazyLoad",
        "TnTBody",
        "TnTColumn",
        "TnTContainer",
        "TnTFooter",
        "TnTHeader",
        "TnTLayout",
        "TnTRow",
        "TnTSideNav",
        "TnTSideNavLink",
        "TnTSideNavMenuGroup",
        "TnTSideNavToggle",
        "TnTProgressIndicator",
        "TnTScheduler",
        "TnTWeekView",
        "TnTSkeleton",
        "NTSnackbar",
        "TnTToast",
        "TnTTooltip",
        "TnTMeasurements",
        "TnTThemeToggle",
        "NTTag",
        "TnTTabChild",
        "TnTTabView",
        "TnTTypeahead",
        "TnTVirtualize",
        "NTVirtualize",
        "TnTWizard",
        "TnTExternalClickHandler",
        "TnTRippleEffect",
    ];

    /// <summary>
    /// Gets category hints for components that do not have curated metadata yet.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ComponentCategories { get; } = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["TnTAccordion"] = "Advanced",
        ["TnTAccordionChild"] = "Advanced",
        ["_TnTAccordionChildRender"] = "Infrastructure",
        ["TnTAnimation"] = "Feedback and display",
        ["TnTNavLink"] = "Layout and navigation",
        ["TnTBadge"] = "Feedback and display",
        ["TnTButton"] = "Buttons",
        ["TnTFabButton"] = "Buttons",
        ["TnTFabContainer"] = "Buttons",
        ["TnTImageButton"] = "Buttons",
        ["NTButtonGroup"] = "Buttons",
        ["TnTCard"] = "Feedback and display",
        ["TnTCarousel"] = "Advanced",
        ["TnTCarouselItem"] = "Advanced",
        ["TnTChip"] = "Feedback and display",
        ["BasicConfirmationDialog"] = "Advanced",
        ["TnTDialog"] = "Advanced",
        ["TnTDivider"] = "Feedback and display",
        ["NTRichTextEditor"] = "Advanced",
        ["TnTMarkdownEditor"] = "Advanced",
        ["EditorToolIframeButton"] = "Editor tooling",
        ["EditorToolIframePanel"] = "Editor tooling",
        ["EditorToolImageButton"] = "Editor tooling",
        ["EditorToolImagePanel"] = "Editor tooling",
        ["EditorToolLinkButton"] = "Editor tooling",
        ["EditorToolLinkPanel"] = "Editor tooling",
        ["EditorToolTableButton"] = "Editor tooling",
        ["EditorToolTablePanel"] = "Editor tooling",
        ["EditorToolTextColorButton"] = "Editor tooling",
        ["EditorToolTextColorPanel"] = "Editor tooling",
        ["TnTInputBase"] = "Forms",
        ["TnTInputCheckbox"] = "Forms",
        ["TnTInputCurrency"] = "Forms",
        ["NTInputDateTime"] = "Forms",
        ["NTInputFile"] = "Forms",
        ["NTInputSelect"] = "Forms",
        ["TnTInputFile"] = "Forms",
        ["TnTInputRadio"] = "Forms",
        ["TnTInputRadioGroup"] = "Forms",
        ["TnTInputSelect"] = "Forms",
        ["TnTInputSwitch"] = "Forms",
        ["TnTInputText"] = "Forms",
        ["TnTInputTextArea"] = "Forms",
        ["TnTDataGrid"] = "Data",
        ["TnTDataGridBody"] = "Data infrastructure",
        ["TnTDataGridBodyCell"] = "Data infrastructure",
        ["TnTDataGridBodyRow"] = "Data infrastructure",
        ["TnTDataGridBodyRowEmpty"] = "Data infrastructure",
        ["TnTDataGridHeaderCell"] = "Data infrastructure",
        ["TnTDataGridHeaderRow"] = "Data infrastructure",
        ["TnTDataGridVirtualizedBody"] = "Data infrastructure",
        ["TnTPaginationButtons"] = "Data",
        ["TnTColumnBase"] = "Data",
        ["TnTPropertyColumn"] = "Data",
        ["TnTTemplateColumn"] = "Data",
        ["TnTLazyLoad"] = "Advanced",
        ["TnTBody"] = "Layout and navigation",
        ["TnTColumn"] = "Layout and navigation",
        ["TnTContainer"] = "Layout and navigation",
        ["TnTFooter"] = "Layout and navigation",
        ["TnTHeader"] = "Layout and navigation",
        ["TnTLayout"] = "Layout and navigation",
        ["TnTRow"] = "Layout and navigation",
        ["TnTSideNav"] = "Layout and navigation",
        ["TnTSideNavLink"] = "Layout and navigation",
        ["TnTSideNavMenuGroup"] = "Layout and navigation",
        ["TnTSideNavToggle"] = "Layout and navigation",
        ["TnTProgressIndicator"] = "Feedback and display",
        ["TnTScheduler"] = "Data",
        ["TnTWeekView"] = "Data",
        ["TnTSkeleton"] = "Feedback and display",
        ["NTSnackbar"] = "Feedback and display",
        ["TnTToast"] = "Feedback and display",
        ["TnTTooltip"] = "Feedback and display",
        ["TnTMeasurements"] = "Theming",
        ["TnTThemeToggle"] = "Theming",
        ["NTTag"] = "Feedback and display",
        ["TnTTabChild"] = "Advanced",
        ["TnTTabView"] = "Advanced",
        ["TnTTypeahead"] = "Forms",
        ["TnTVirtualize"] = "Data",
        ["NTVirtualize"] = "Data",
        ["TnTWizard"] = "Advanced",
        ["TnTExternalClickHandler"] = "Infrastructure",
        ["TnTRippleEffect"] = "Infrastructure",
    };

    /// <summary>
    /// Gets curated component metadata for high-value documentation pages.
    /// </summary>
    public static IReadOnlyList<CuratedComponentDocumentation> Components { get; } = [
        Create("TnTButton", "TnTButton", "Buttons", "Primary action button with Material-inspired color, variant, icon, loading, and disabled states.", true, ["button", "action", "submit"], "Basic action", "<TnTButton Text=\"Save\" />", Color(), Variant(), Disabled()),
        Create("TnTImageButton", "TnTImageButton", "Buttons", "Icon and image button for compact command surfaces.", false, ["icon", "image", "toolbar"], "Icon command", "<TnTImageButton Icon=\"save\" />", Color(), Disabled()),
        Create("TnTFabButton", "TnTFabButton", "Buttons", "Floating action button for a prominent page-level action.", true, ["fab", "floating", "action"], "Floating action", "<TnTFabButton Icon=\"add\" />", Color(), Disabled()),

        Create("TnTLayout", "TnTLayout", "Layout and navigation", "Top-level application layout container for NTComponents applications.", true, ["app shell", "layout"], "App shell", "<TnTLayout>@Body</TnTLayout>", Density()),
        Create("TnTSideNav", "TnTSideNav", "Layout and navigation", "Side navigation surface for persistent or collapsible app navigation.", true, ["navigation", "drawer", "menu"], "Navigation shell", "<TnTSideNav>...</TnTSideNav>", Variant(), Disabled()),
        Create("TnTHeader", "TnTHeader", "Layout and navigation", "Header surface for page and application chrome.", false, ["top bar", "app bar", "header"], "Page header", "<TnTHeader>Documentation</TnTHeader>", Color(), Density()),
        Create("TnTContainer", "TnTContainer", "Layout and navigation", "Responsive layout container for page content.", false, ["container", "responsive", "width"], "Content container", "<TnTContainer>Content</TnTContainer>", Density()),

        Create("TnTInputText", "TnTInputText", "Forms", "Text input component for editable string values.", true, ["input", "text", "forms"], "Text field", "<TnTInputText @bind-Value=\"name\" Label=\"Name\" />", Label(), Disabled()),
        Create("TnTInputSelect", "TnTInputSelect", "Forms", "Select input component for choosing from known values.", true, ["select", "dropdown", "forms"], "Select field", "<TnTInputSelect @bind-Value=\"value\" Label=\"Status\" />", Label(), Disabled()),
        Create("TnTInputCheckbox", "TnTInputCheckbox", "Forms", "Checkbox input for boolean choices.", false, ["checkbox", "boolean", "forms"], "Checkbox", "<TnTInputCheckbox @bind-Value=\"enabled\" Label=\"Enabled\" />", Label(), Disabled()),
        Create("TnTInputSwitch", "TnTInputSwitch", "Forms", "Switch input for immediate boolean settings.", false, ["switch", "toggle", "forms"], "Switch", "<TnTInputSwitch @bind-Value=\"enabled\" Label=\"Enabled\" />", Label(), Disabled()),

        Create("TnTCard", "TnTCard", "Feedback and display", "Content card for grouped information, actions, and supporting media.", true, ["card", "surface", "content"], "Content card", "<TnTCard>Card content</TnTCard>", Variant(), Density()),
        Create("TnTBadge", "TnTBadge", "Feedback and display", "Compact status or count indicator for buttons, icons, and overlays.", false, ["badge", "status", "count"], "Status badge", "<TnTBadge>3</TnTBadge>", Color()),
        Create("NTTag", "NTTag", "Feedback and display", "Bounded label tag for categories, statuses, and metadata that should stay inside parent containers.", false, ["tag", "label", "metadata", "status"], "Category tag", "<NTTag>Documentation</NTTag>", Color(), Label()),
        Create("TnTTooltip", "TnTTooltip", "Feedback and display", "Contextual hover or focus help attached to interactive elements.", false, ["tooltip", "help", "hint"], "Tooltip", "<TnTTooltip Text=\"More information\">...</TnTTooltip>", Placement(), Label()),
        Create("TnTToast", "TnTToast", "Feedback and display", "Transient feedback message for completed user actions.", true, ["toast", "notification", "feedback"], "Toast", "<TnTToast />", Color(), Placement()),
        Create("TnTSnackbar", "TnTSnackbar", "Feedback and display", "Actionable transient message for status and recovery flows.", true, ["snackbar", "notification", "action"], "Snackbar", "<TnTSnackbar />", Color(), Placement()),

        Create("TnTDataGrid", "TnTDataGrid", "Data", "Data grid for tabular display, paging, templates, and property columns.", true, ["grid", "table", "data", "pagination", "columns"], "Data table", "<TnTDataGrid Items=\"items\">...</TnTDataGrid>", Density(), Disabled()),
        Create("TnTPaginationButtons", "TnTPaginationButtons", "Data", "Pagination controls for data-heavy views.", false, ["pagination", "pager", "data"], "Pagination", "<TnTPaginationButtons />", Density(), Disabled()),
        Create("TnTTemplateColumn", "TnTTemplateColumn", "Data", "Template column for custom data grid cell rendering.", false, ["grid", "template", "column"], "Template column", "<TnTTemplateColumn Context=\"item\">...</TnTTemplateColumn>", Label()),
        Create("TnTPropertyColumn", "TnTPropertyColumn", "Data", "Property column for binding data grid cells to item properties.", false, ["grid", "property", "column"], "Property column", "<TnTPropertyColumn Property=\"item => item.Name\" />", Label()),

        Create("TnTDialog", "TnTDialog", "Advanced", "Dialog surface for modal workflows and confirmations.", true, ["dialog", "modal", "overlay"], "Dialog", "<TnTDialog>Dialog content</TnTDialog>", Variant(), Disabled()),
        Create("TnTTabView", "TnTTabView", "Advanced", "Tabbed view container for related panels.", true, ["tabs", "navigation", "panels"], "Tabs", "<TnTTabView>...</TnTTabView>", Density(), Disabled()),
        Create("TnTAccordion", "TnTAccordion", "Advanced", "Accordion container for collapsible content groups.", false, ["accordion", "collapse", "disclosure"], "Accordion", "<TnTAccordion>...</TnTAccordion>", Density(), Disabled()),
        Create("NTRichTextEditor", "NTRichTextEditor", "Advanced", "Rich text editor with toolbar commands and formatted content editing.", true, ["editor", "rich text", "wysiwyg"], "Rich text editing", "<NTRichTextEditor @bind-Value=\"html\" />", Disabled(), Label()),
    ];

    /// <summary>
    /// Creates fallback metadata for a component that does not have hand-authored copy yet.
    /// </summary>
    /// <param name="typeName">The component type name.</param>
    /// <param name="category">The generated category, when available.</param>
    /// <param name="summary">The generated summary, when available.</param>
    /// <returns>The fallback component documentation.</returns>
    public static CuratedComponentDocumentation CreateFallback(string typeName, string? category, string? summary) {
        var resolvedCategory = ResolveCategory(typeName, category);
        var resolvedSummary = string.IsNullOrWhiteSpace(summary)
            ? $"{typeName} component reference, examples, raw Razor snippet, and runtime playground."
            : summary;

        return new CuratedComponentDocumentation(
            typeName,
            typeName,
            resolvedCategory,
            resolvedSummary,
            false,
            CreateKeywords(typeName, resolvedCategory),
            [
                new ComponentExampleDocumentation(
                    "Default playground",
                    "A generated starting point for the component playground.",
                    CreateDefaultExampleCode(typeName),
                    CreateDefaultRuntimeOptions(typeName)),
            ]);
    }

    private static CuratedComponentDocumentation Create(
        string typeName,
        string displayName,
        string category,
        string summary,
        bool isFeatured,
        IReadOnlyList<string> keywords,
        string exampleTitle,
        string exampleCode,
        params ComponentRuntimeOptionDocumentation[] runtimeOptions) =>
        new(typeName, displayName, category, summary, isFeatured, keywords, [
            new ComponentExampleDocumentation(exampleTitle, summary, exampleCode, runtimeOptions),
        ]);

    private static string ResolveCategory(string typeName, string? generatedCategory) {
        if (ComponentCategories.TryGetValue(typeName, out var category)) {
            return category;
        }

        return string.IsNullOrWhiteSpace(generatedCategory) ? "Core" : generatedCategory;
    }

    private static IReadOnlyList<string> CreateKeywords(string typeName, string category) =>
        [
            typeName,
            category,
            typeName.StartsWith("NT", StringComparison.Ordinal) ? "nt" : "tnt",
            "component",
        ];

    private static IReadOnlyList<ComponentRuntimeOptionDocumentation> CreateDefaultRuntimeOptions(string typeName) =>
        typeName switch {
            "TnTButton" or "TnTImageButton" or "TnTFabButton" or "NTButtonGroup" => [Variant(), Disabled()],
            "TnTBadge" or "TnTCard" or "TnTChip" or "TnTProgressIndicator" or "TnTSkeleton" or "NTTag" => [Color(), Variant()],
            "TnTInputCheckbox" or "TnTInputSwitch" or "TnTInputText" or "TnTInputTextArea" or "TnTInputSelect" or "TnTTypeahead" => [Label(), Disabled()],
            "TnTLayout" or "TnTSideNav" or "TnTHeader" or "TnTContainer" or "TnTRow" or "TnTColumn" => [Density()],
            "TnTTooltip" or "TnTToast" or "NTSnackbar" => [Placement()],
            _ => [Density()],
        };

    private static string CreateDefaultExampleCode(string typeName) =>
        typeName switch {
            "TnTAccordion" or "TnTAccordionChild" => """
                <TnTAccordion>
                    <TnTAccordionChild Label="Overview" OpenByDefault="true">
                        Accordion content
                    </TnTAccordionChild>
                </TnTAccordion>
                """,
            "TnTBadge" => "<TnTBadge>Ready</TnTBadge>",
            "NTTag" => "<NTTag>Documentation</NTTag>",
            "TnTButton" => "<TnTButton Appearance=\"ButtonAppearance.Filled\">Save changes</TnTButton>",
            "TnTCard" => """
                <TnTCard Appearance="CardAppearance.Outlined">
                    Card content
                </TnTCard>
                """,
            "TnTCarousel" or "TnTCarouselItem" => """
                <TnTCarousel>
                    <TnTCarouselItem>
                        Carousel item
                    </TnTCarouselItem>
                </TnTCarousel>
                """,
            "TnTChip" => "<TnTChip Label=\"Filter\" @bind-Value=\"selected\" />",
            "TnTDivider" => "<TnTDivider Direction=\"LayoutDirection.Horizontal\" />",
            "TnTImageButton" => "<TnTImageButton Icon=\"@MaterialIcon.Save\" />",
            "TnTFabButton" => "<TnTFabButton><MaterialIcon Icon=\"add\" /></TnTFabButton>",
            "TnTInputCheckbox" => "<TnTInputCheckbox @bind-Value=\"enabled\" Label=\"Enabled\" />",
            "TnTInputRadio" or "TnTInputRadioGroup" => """
                <TnTInputRadioGroup @bind-Value="choice" Label="Choice">
                    <TnTInputRadio Value="@("A")" Label="A" />
                    <TnTInputRadio Value="@("B")" Label="B" />
                </TnTInputRadioGroup>
                """,
            "TnTInputSelect" or "NTInputSelect" => """
                <TnTInputSelect @bind-Value="choice" Label="Choice">
                    <option value="One">One</option>
                    <option value="Two">Two</option>
                </TnTInputSelect>
                """,
            "TnTInputSwitch" => "<TnTInputSwitch @bind-Value=\"enabled\" Label=\"Enabled\" />",
            "TnTInputText" => "<TnTInputText @bind-Value=\"name\" Label=\"Name\" />",
            "TnTInputTextArea" => "<TnTInputTextArea @bind-Value=\"notes\" Label=\"Notes\" />",
            "TnTLayout" => """
                <TnTLayout>
                    <TnTHeader>Header</TnTHeader>
                    <TnTBody>Body content</TnTBody>
                </TnTLayout>
                """,
            "TnTNavLink" => "<TnTNavLink href=\"/docs\">Documentation</TnTNavLink>",
            "TnTProgressIndicator" => "<TnTProgressIndicator Appearance=\"ProgressAppearance.Linear\" Value=\"65\" />",
            "TnTSkeleton" => "<TnTSkeleton Appearance=\"SkeletonAppearance.Square\" />",
            "TnTTabView" or "TnTTabChild" => """
                <TnTTabView>
                    <TnTTabChild Label="Usage">Usage content</TnTTabChild>
                    <TnTTabChild Label="API">API content</TnTTabChild>
                </TnTTabView>
                """,
            "TnTThemeToggle" => "<TnTThemeToggle />",
            "TnTTooltip" => """
                <TnTButton>
                    Hover for help
                    <TnTTooltip>Tooltip content</TnTTooltip>
                </TnTButton>
                """,
            _ => $"<{typeName} />",
        };

    private static ComponentRuntimeOptionDocumentation Color() => new("Color", "Select", ["Primary", "Secondary", "Tertiary", "Error"]);

    private static ComponentRuntimeOptionDocumentation Variant() => new("Variant", "Select", ["Filled", "Outlined", "Text", "Elevated"]);

    private static ComponentRuntimeOptionDocumentation Disabled() => new("Disabled", "Toggle", ["true", "false"]);

    private static ComponentRuntimeOptionDocumentation Label() => new("Label", "Text", []);

    private static ComponentRuntimeOptionDocumentation Placement() => new("Placement", "Select", ["Top", "Right", "Bottom", "Left"]);

    private static ComponentRuntimeOptionDocumentation Density() => new("Density", "Select", ["Default", "Comfortable", "Compact"]);
}

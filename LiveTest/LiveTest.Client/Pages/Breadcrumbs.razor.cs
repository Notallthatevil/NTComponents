using NTComponents;

namespace LiveTest.Client.Pages;

/// <summary>
///     Demo page for the breadcrumb component.
/// </summary>
public partial class Breadcrumbs {
    private static IReadOnlyList<TnTBreadcrumbItem> IconOnlyItems { get; } = new[] {
        new TnTBreadcrumbItem { Icon = MaterialIcon.Home, Href = "/", AriaLabel = "Home" },
        new TnTBreadcrumbItem { Text = "Workspace", Href = "/workspace" },
        new TnTBreadcrumbItem { Text = "Design System" }
    };

    private static IReadOnlyList<TnTBreadcrumbItem> LongLabelItems { get; } = new[] {
        new TnTBreadcrumbItem { Text = "Home", Href = "/" },
        new TnTBreadcrumbItem { Text = "Enterprise Resource Planning", Href = "/erp" },
        new TnTBreadcrumbItem { Text = "Quarterly Budget Review and Forecasting" }
    };

    private bool DisableIntermediate { get; set; }

    private bool IncludeHomeIcon { get; set; } = true;

    private IReadOnlyList<TnTBreadcrumbItem> PreviewItems => BuildPreviewItems();

    private BreadcrumbDemoPreset SelectedPreset { get; set; } = BreadcrumbDemoPreset.Products;

    private bool UseExplicitCurrent { get; set; }

    private IReadOnlyList<TnTBreadcrumbItem> BuildPreviewItems() {
        var items = SelectedPreset switch {
            BreadcrumbDemoPreset.Products => new[] {
                CreateLeadingItem("Home", "/"),
                new TnTBreadcrumbItem { Text = "Products", Href = "/products", Disabled = DisableIntermediate, IsCurrent = UseExplicitCurrent && DisableIntermediate },
                new TnTBreadcrumbItem { Text = "Details", IsCurrent = UseExplicitCurrent && !DisableIntermediate }
            },
            BreadcrumbDemoPreset.Reports => new[] {
                CreateLeadingItem("Home", "/"),
                new TnTBreadcrumbItem { Text = "Reports", Href = "/reports", Disabled = DisableIntermediate, IsCurrent = UseExplicitCurrent && DisableIntermediate },
                new TnTBreadcrumbItem { Text = "Quarterly", IsCurrent = UseExplicitCurrent && !DisableIntermediate }
            },
            _ => new[] {
                CreateLeadingItem("Home", "/"),
                new TnTBreadcrumbItem { Text = "Admin", Href = "/admin", Disabled = DisableIntermediate, IsCurrent = UseExplicitCurrent && DisableIntermediate },
                new TnTBreadcrumbItem { Text = "Users", IsCurrent = UseExplicitCurrent && !DisableIntermediate }
            }
        };

        return items;
    }

    private TnTBreadcrumbItem CreateLeadingItem(string text, string href) => IncludeHomeIcon
        ? new TnTBreadcrumbItem { Icon = MaterialIcon.Home, Href = href, AriaLabel = text }
        : new TnTBreadcrumbItem { Text = text, Href = href };
}

/// <summary>
///     Presets used by the breadcrumb demo page.
/// </summary>
public enum BreadcrumbDemoPreset {
    /// <summary>
    ///     Product navigation example.
    /// </summary>
    Products,

    /// <summary>
    ///     Reporting navigation example.
    /// </summary>
    Reports,

    /// <summary>
    ///     Administration navigation example.
    /// </summary>
    Admin
}

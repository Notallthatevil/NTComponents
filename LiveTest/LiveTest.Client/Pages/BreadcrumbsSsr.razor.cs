using NTComponents;

namespace LiveTest.Client.Pages;

/// <summary>
///     Static SSR page for breadcrumb browser verification.
/// </summary>
public partial class BreadcrumbsSsr {
    private static IReadOnlyList<TnTBreadcrumbItem> DisabledItems { get; } = new[] {
        new TnTBreadcrumbItem { Icon = MaterialIcon.Home, Href = "/", AriaLabel = "Home" },
        new TnTBreadcrumbItem { Text = "Admin", Href = "/admin" },
        new TnTBreadcrumbItem { Text = "Archived", Href = "/admin/archived", Disabled = true }
    };

    private static IReadOnlyList<TnTBreadcrumbItem> HomeItems { get; } = new[] {
        new TnTBreadcrumbItem { Icon = MaterialIcon.Home, Href = "/", AriaLabel = "Home" },
        new TnTBreadcrumbItem { Text = "Reports", Href = "/reports" },
        new TnTBreadcrumbItem { Text = "Quarterly" }
    };

    private static IReadOnlyList<TnTBreadcrumbItem> StandardItems { get; } = new[] {
        new TnTBreadcrumbItem { Text = "Home", Href = "/" },
        new TnTBreadcrumbItem { Text = "Products", Href = "/products" },
        new TnTBreadcrumbItem { Text = "Details" }
    };
}

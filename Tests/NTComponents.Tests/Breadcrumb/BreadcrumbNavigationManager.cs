using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Breadcrumb;

internal sealed class BreadcrumbNavigationManager : NavigationManager {
    public BreadcrumbNavigationManager(string baseUri, string uri) {
        Initialize(baseUri, uri);
    }
}

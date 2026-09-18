using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System.Globalization;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Displays a hierarchy of native links using Material 3 theme colors and typography.
/// </summary>
/// <remarks>Builds the trail from the URL relative to the application base. Parent URL paths must be valid destinations in the application.</remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Native breadcrumb links work in static SSR without JavaScript or Blazor interactivity.",
    CompatibilityDetails = "Builds items from the request URL in static SSR and follows location changes when interactive.")]
public partial class NTBreadcrumb : IAsyncDisposable {

    private IReadOnlyList<NTBreadcrumbItem> _items = [];
    private bool _disposed;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>Gets or sets the accessible navigation label. Localize this value when needed.</summary>
    [Parameter]
    public string AccessibleLabel { get; set; } = "Breadcrumb";

    /// <summary>Gets or sets the label for the application root.</summary>
    [Parameter]
    public string HomeLabel { get; set; } = "Home";

    /// <summary>Gets or sets the size of the links and current-page label.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Small;

    /// <summary>Gets or sets the ancestor link text color. Defaults to the navigation link's primary color.</summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    /// <summary>Gets or sets the current-page text color.</summary>
    [Parameter]
    public TnTColor? CurrentTextColor { get; set; }

    /// <summary>Gets or sets the current-page container color.</summary>
    [Parameter]
    public TnTColor? CurrentBackgroundColor { get; set; }

    /// <summary>Gets or sets the separator color.</summary>
    [Parameter]
    public TnTColor? SeparatorColor { get; set; }

    /// <summary>Gets or sets decorative content rendered between items. Null uses the default chevron.</summary>
    /// <remarks>Divider content is hidden from assistive technology and should not contain interactive elements.</remarks>
    [Parameter]
    public RenderFragment? DividerTemplate { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-breadcrumb")
        .AddSize(Size)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-breadcrumb-current-fg", CurrentTextColor.ToCssTnTColorVariable(), CurrentTextColor.HasValue)
        .AddVariable("nt-breadcrumb-current-bg", CurrentBackgroundColor.ToCssTnTColorVariable(), CurrentBackgroundColor.HasValue)
        .AddVariable("nt-breadcrumb-separator", SeparatorColor.ToCssTnTColorVariable(), SeparatorColor.HasValue)
        .Build();

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        BuildItems();
    }

    private void BuildItems() {
        var path = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Split('?', '#')[0];
        var items = new List<NTBreadcrumbItem> { new(HomeLabel, NavigationManager.BaseUri) };
        var href = NavigationManager.BaseUri;
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries)) {
            href += segment + "/";
            var label = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Uri.UnescapeDataString(segment).Replace('-', ' ').Replace('_', ' '));
            items.Add(new(label, href.TrimEnd('/')));
        }
        _items = items;
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs args) {
        try {
            await InvokeAsync(() => {
                if (!_disposed) {
                    BuildItems();
                    StateHasChanged();
                }
            });
        }
        catch (Exception exception) {
            await DispatchExceptionAsync(exception);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() {
        _disposed = true;
        NavigationManager.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

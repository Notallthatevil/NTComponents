using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Renders a Material 3 inspired breadcrumb trail using semantic HTML.
/// </summary>
public partial class TnTBreadcrumbs {
    private IReadOnlyList<BreadcrumbRenderItem> _resolvedItems = Array.Empty<BreadcrumbRenderItem>();

    /// <summary>
    ///     Gets or sets the accessible label applied to the breadcrumb navigation landmark.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Breadcrumb";

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-breadcrumbs")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Gets or sets the breadcrumb items to render.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TnTBreadcrumbItem> Items { get; set; } = default!;

    private IReadOnlyList<BreadcrumbRenderItem> ResolvedItems => _resolvedItems;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        ArgumentNullException.ThrowIfNull(Items);

        if (Items.Count == 0) {
            throw new InvalidOperationException($"{nameof(TnTBreadcrumbs)} requires at least one breadcrumb item.");
        }

        ValidateItems(Items);
        _resolvedItems = CreateResolvedItems(Items);
    }

    private static IReadOnlyList<BreadcrumbRenderItem> CreateResolvedItems(IReadOnlyList<TnTBreadcrumbItem> items) {
        var currentIndex = GetCurrentIndex(items);
        var resolvedItems = new BreadcrumbRenderItem[items.Count];

        for (var index = 0; index < items.Count; index++) {
            var item = items[index];
            var text = NormalizeText(item.Text);

            resolvedItems[index] = new BreadcrumbRenderItem(
                index,
                text,
                item.Href,
                item.Icon,
                item.Disabled,
                currentIndex == index,
                NormalizeText(item.AriaLabel));
        }

        return resolvedItems;
    }

    private static int GetCurrentIndex(IReadOnlyList<TnTBreadcrumbItem> items) {
        for (var index = 0; index < items.Count; index++) {
            if (items[index].IsCurrent) {
                return index;
            }
        }

        return items.Count - 1;
    }

    private static string? GetAriaCurrent(BreadcrumbRenderItem item) => item.IsCurrent ? "page" : null;

    private static string? GetAriaDisabled(BreadcrumbRenderItem item) => item.Disabled ? "true" : null;

    private static string GetCrumbClass(BreadcrumbRenderItem item) => CssClassBuilder.Create()
        .AddClass("tnt-breadcrumbs__crumb")
        .AddClass("tnt-breadcrumbs__crumb--link", item.RenderAsLink)
        .AddClass("tnt-breadcrumbs__crumb--current", item.IsCurrent)
        .AddClass("tnt-breadcrumbs__crumb--disabled", item.Disabled)
        .AddClass("tnt-breadcrumbs__crumb--icon-only", !item.HasText)
        .Build();

    private static string? NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateItems(IReadOnlyList<TnTBreadcrumbItem> items) {
        var currentCount = 0;

        for (var index = 0; index < items.Count; index++) {
            var item = items[index] ?? throw new InvalidOperationException($"Breadcrumb item at index {index} is null.");

            if (item.IsCurrent) {
                currentCount++;
            }

            var hasText = !string.IsNullOrWhiteSpace(item.Text);
            var hasAriaLabel = !string.IsNullOrWhiteSpace(item.AriaLabel);
            if (!hasText && item.Icon is null) {
                throw new InvalidOperationException(
                    $"Breadcrumb item at index {index} must provide visible content via {nameof(TnTBreadcrumbItem.Text)} or {nameof(TnTBreadcrumbItem.Icon)}.");
            }

            if (!hasText && !hasAriaLabel) {
                throw new InvalidOperationException(
                    $"Breadcrumb item at index {index} must provide {nameof(TnTBreadcrumbItem.Text)} or {nameof(TnTBreadcrumbItem.AriaLabel)}.");
            }
        }

        if (currentCount > 1) {
            throw new InvalidOperationException($"Only one {nameof(TnTBreadcrumbItem)} may be marked as current.");
        }
    }

    private sealed record BreadcrumbRenderItem(
        int Index,
        string? Text,
        string? Href,
        TnTIcon? Icon,
        bool Disabled,
        bool IsCurrent,
        string? AriaLabel) {

        public bool HasText => !string.IsNullOrWhiteSpace(Text);

        public bool RenderAsLink => !IsCurrent && !Disabled && !string.IsNullOrWhiteSpace(Href);
    }
}

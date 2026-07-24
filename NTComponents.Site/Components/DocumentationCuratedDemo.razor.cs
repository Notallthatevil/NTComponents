using Microsoft.AspNetCore.Components;
using NTComponents.Site.Documentation;
using NTComponents.Virtualization;
using System.Globalization;

namespace NTComponents.Site.Components;

public partial class DocumentationCuratedDemo {
    private static readonly string[] SupportedComponentNames = ["NTDialog", "NTMenu", "NTSnackbar", "NTToast", "NTTooltip", "NTVirtualize"];
    private static readonly string[] Items = [.. Enumerable.Range(1, 100).Select(index => $"Item {index}")];
    private NTDialog? _dialog;

    [Parameter, EditorRequired]
    public ComponentDocumentationEntry Component { get; set; } = default!;

    [Parameter, EditorRequired]
    public Func<string, object?> ValueProvider { get; set; } = default!;

    public string GeneratedRazorMarkup => GetGeneratedRazorMarkup(Component.Type.Name);

    private string MenuAnchorStyle => $"anchor-name: {TextValue("AnchorName", "--docs-menu-anchor")};";

    public static bool Supports(string? componentName) => componentName is not null && SupportedComponentNames.Contains(componentName, StringComparer.Ordinal);

    public static string GetGeneratedRazorMarkup(string? componentName) => GetGeneratedRazorMarkup(componentName, parameterName => GetDefaultMarkupAttribute(componentName, parameterName));

    public static string GetGeneratedRazorMarkup(string? componentName, Func<string, string?> attributeFormatter) {
        var (markup, marker, parameterNames) = componentName switch {
            "NTDialog" => (DialogMarkup, "<NTDialog @ref=\"_dialog\"", new[] { "Id", "Title", "SupportingText", "CloseButtonAriaLabel", "ButtonSpacing", "CloseOnBackdrop", "CloseOnEscape", "Elevation", "Open", "ShowCloseButton" }),
            "NTMenu" => (MenuMarkup, "<NTMenu ElementId=\"docs-example-menu\"", new[] { "AnchorName", "AnchorSelector", "Appearance", "AriaLabel", "CloseOnContentClick", "ContainerColor", "Disabled", "Elevation", "IsSubMenu", "Popover", "Role", "SelectedContainerColor", "SelectedTextColor", "TextColor" }),
            "NTSnackbar" => (SnackbarMarkup, "<NTSnackbar", new[] { "Position" }),
            "NTToast" => (ToastMarkup, "<NTToast", new[] { "Position" }),
            "NTTooltip" => (TooltipMarkup, "<NTTooltip", new[] { "BackgroundColor", "BorderColor", "ShowDelay", "HideDelay", "TextColor", "Variant" }),
            "NTVirtualize" => (VirtualizeMarkup, "<NTVirtualize TItem=\"string\" ItemsProvider=\"LoadItemsAsync\"", new[] { "ItemSize", "MaxItemCount", "OverscanCount", "PlaceholderPreloadWindowCount", "BackgroundPreloadWindowCount", "RevalidateCachedItems", "ScrollRestorationKey", "MaxCachedItemCount", "SpacerElement" }),
            _ => (string.Empty, string.Empty, Array.Empty<string>())
        };
        if (string.IsNullOrEmpty(markup)) {
            return markup;
        }

        var attributes = parameterNames.Select(attributeFormatter).Where(attribute => !string.IsNullOrWhiteSpace(attribute)).ToArray();
        return attributes.Length == 0 ? markup : markup.Replace(marker, $"{marker}\r\n          {string.Join("\r\n          ", attributes)}", StringComparison.Ordinal);
    }

    private static ValueTask<TnTItemsProviderResult<string>> LoadItemsAsync(NTVirtualizeItemsProviderRequest<string> request) {
        var count = request.Count ?? 20;
        var page = Items.Skip(request.StartIndex).Take(count).ToArray();
        return ValueTask.FromResult(new TnTItemsProviderResult<string>(page, Items.Length));
    }

    private async Task OpenDialogAsync() {
        if (_dialog is not null) {
            await _dialog.OpenAsync();
        }
    }

    private Task ShowSnackbarAsync() => SnackbarService.ShowAsync("Changes saved", "Undo", () => Task.CompletedTask, timeout: 8, showClose: true);

    private Task ShowToastAsync() => ToastService.ShowSuccessAsync("Saved", "Your changes were saved.", timeout: 8);

    private T Value<T>(string parameterName, T fallback) {
        var value = ValueProvider(parameterName);
        if (value is T typedValue) {
            return typedValue;
        }

        if (value is null) {
            return fallback;
        }

        try {
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (InvalidCastException) {
            return fallback;
        }
        catch (FormatException) {
            return fallback;
        }
    }

    private string TextValue(string parameterName, string fallback) {
        var value = ValueProvider(parameterName) as string;
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private TnTColor? ColorValue(string parameterName) => Value<TnTColor?>(parameterName, null);

    private static string? GetDefaultMarkupAttribute(string? componentName, string parameterName) => (componentName, parameterName) switch {
        ("NTDialog", "Id") => "Id=\"docs-example-dialog\"",
        ("NTDialog", "Title") => "Title=\"Review changes\"",
        ("NTDialog", "SupportingText") => "SupportingText=\"Confirm the changes before continuing.\"",
        ("NTDialog", "ShowCloseButton") => "ShowCloseButton=\"true\"",
        ("NTMenu", "AnchorName") => "AnchorName=\"--docs-menu-anchor\"",
        ("NTMenu", "AnchorSelector") => "AnchorSelector=\"#docs-menu-trigger\"",
        ("NTMenu", "AriaLabel") => "AriaLabel=\"Example actions\"",
        ("NTSnackbar", "Position") => "Position=\"BottomCenter\"",
        ("NTToast", "Position") => "Position=\"BottomRightCorner\"",
        ("NTTooltip", "ShowDelay") => "ShowDelay=\"500\"",
        ("NTTooltip", "HideDelay") => "HideDelay=\"200\"",
        ("NTVirtualize", "ItemSize") => "ItemSize=\"50\"",
        _ => null
    };

    private const string DialogMarkup = """
        <NTButton Label="Open example dialog" OnClickCallback="OpenDialogAsync" />

        <NTDialog @ref="_dialog">
            <ChildContent>
                <p>Dialogs keep focused content and actions together.</p>
            </ChildContent>
            <Buttons>
                <button type="button" class="nt-dialog-button" command="request-close" commandfor="docs-example-dialog" value="cancel">Cancel</button>
                <button type="button" class="nt-dialog-button" command="request-close" commandfor="docs-example-dialog" value="confirm">Confirm</button>
            </Buttons>
        </NTDialog>

        @code {
            private NTDialog? _dialog;

            private async Task OpenDialogAsync() {
                if (_dialog is not null) {
                    await _dialog.OpenAsync();
                }
            }
        }
        """;

    private const string MenuMarkup = """
        <NTButton ElementId="docs-menu-trigger"
                  Label="Open example menu"
                  LeadingIcon="MaterialIcon.Menu"
                  style="anchor-name: --docs-menu-anchor;"
                  popovertarget="docs-example-menu"
                  popovertargetaction="toggle" />

        <NTMenu ElementId="docs-example-menu">
            <NTMenuLabelItem Label="Actions" />
            <NTMenuButtonItem Label="Edit" Icon="MaterialIcon.Edit" />
            <NTMenuDividerItem />
            <NTMenuAnchorItem Label="View details" Icon="MaterialIcon.Info" Href="#example" />
        </NTMenu>
        """;

    private const string SnackbarMarkup = """
        @inject INTSnackbarService SnackbarService

        <NTButton Label="Show example snackbar" OnClickCallback="ShowSnackbarAsync" />
        <NTSnackbar />

        @code {
            private Task ShowSnackbarAsync() =>
                SnackbarService.ShowAsync("Changes saved", "Undo", () => Task.CompletedTask, timeout: 8, showClose: true);
        }
        """;

    private const string ToastMarkup = """
        @inject INTToastService ToastService

        <NTButton Label="Show example toast" OnClickCallback="ShowToastAsync" />
        <NTToast />

        @code {
            private Task ShowToastAsync() =>
                ToastService.ShowSuccessAsync("Saved", "Your changes were saved.", timeout: 8);
        }
        """;

    private const string TooltipMarkup = """
        <button type="button" style="position: relative;">
            Hover or focus for help
            <NTTooltip>
                This is a live tooltip.
            </NTTooltip>
        </button>
        """;

    private const string VirtualizeMarkup = """
        @using NTComponents.Virtualization

        <div style="block-size: 15rem; overflow: auto;">
            <NTVirtualize TItem="string" ItemsProvider="LoadItemsAsync">
                <ItemTemplate Context="item">
                    <div style="min-block-size: 48px;">@item</div>
                </ItemTemplate>
                <EmptyTemplate>
                    <p>No items are available.</p>
                </EmptyTemplate>
            </NTVirtualize>
        </div>

        @code {
            private static readonly string[] Items = Enumerable.Range(1, 100).Select(index => $"Item {index}").ToArray();

            private static ValueTask<TnTItemsProviderResult<string>> LoadItemsAsync(NTVirtualizeItemsProviderRequest<string> request) {
                var count = request.Count ?? 20;
                return ValueTask.FromResult(new TnTItemsProviderResult<string>(Items.Skip(request.StartIndex).Take(count).ToArray(), Items.Length));
            }
        }
        """;
}

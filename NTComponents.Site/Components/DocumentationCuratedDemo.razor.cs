using Microsoft.AspNetCore.Components;
using NTComponents.Site.Documentation;
using NTComponents.Virtualization;

namespace NTComponents.Site.Components;

public partial class DocumentationCuratedDemo {
    private static readonly string[] SupportedComponentNames = ["NTDialog", "NTMenu", "NTSnackbar", "NTToast", "NTTooltip", "NTVirtualize"];
    private static readonly string[] Items = [.. Enumerable.Range(1, 100).Select(index => $"Item {index}")];
    private NTDialog? _dialog;

    [Parameter, EditorRequired]
    public ComponentDocumentationEntry Component { get; set; } = default!;

    public string GeneratedRazorMarkup => GetGeneratedRazorMarkup(Component.Type.Name);

    public static bool Supports(string? componentName) => componentName is not null && SupportedComponentNames.Contains(componentName, StringComparer.Ordinal);

    public static string GetGeneratedRazorMarkup(string? componentName) =>
        componentName switch {
            "NTDialog" => DialogMarkup,
            "NTMenu" => MenuMarkup,
            "NTSnackbar" => SnackbarMarkup,
            "NTToast" => ToastMarkup,
            "NTTooltip" => TooltipMarkup,
            "NTVirtualize" => VirtualizeMarkup,
            _ => string.Empty
        };

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

    private const string DialogMarkup = """
        <NTButton Label="Open example dialog" OnClickCallback="OpenDialogAsync" />

        <NTDialog @ref="_dialog"
                  Id="example-dialog"
                  Title="Review changes"
                  SupportingText="Confirm the documentation example."
                  ShowCloseButton="true">
            <ChildContent>
                <p>Dialogs keep focused content and actions together.</p>
            </ChildContent>
            <Buttons>
                <button type="button" class="nt-dialog-button" command="request-close" commandfor="example-dialog" value="cancel">Cancel</button>
                <button type="button" class="nt-dialog-button" command="request-close" commandfor="example-dialog" value="confirm">Confirm</button>
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
        <NTButton ElementId="menu-trigger"
                  Label="Open example menu"
                  LeadingIcon="MaterialIcon.Menu"
                  style="anchor-name: --menu-anchor;"
                  popovertarget="example-menu"
                  popovertargetaction="toggle" />

        <NTMenu ElementId="example-menu"
                AnchorName="--menu-anchor"
                AnchorSelector="#menu-trigger"
                AriaLabel="Example actions">
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
            <NTTooltip ShowDelay="100" HideDelay="100">
                This is a live tooltip.
            </NTTooltip>
        </button>
        """;

    private const string VirtualizeMarkup = """
        @using NTComponents.Virtualization

        <div style="block-size: 15rem; overflow: auto;">
            <NTVirtualize TItem="string" ItemsProvider="LoadItemsAsync" ItemSize="48">
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

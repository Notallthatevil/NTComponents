using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using NTComponents.Site.Documentation;
using NTComponents.Virtualization;
using System.Globalization;

namespace NTComponents.Site.Components;

public partial class DocumentationCuratedDemo {
    private static readonly string[] SupportedComponentNames = ["NTContextMenu", "NTDialog", "NTFileUpload", "NTForm", "NTMenu", "NTSnackbar", "NTToast", "NTTooltip", "NTVirtualize", "NTWindowHost"];
    private static readonly string[] Items = [.. Enumerable.Range(1, 100).Select(index => $"Item {index}")];
    private NTDialog? _dialog;
    private readonly DocumentationContactModel _contact = new();
    private INTWindow? _managedWindow;
    private string? _formResult;
    private string? _uploadResult;
    private string? _contextResult;
    private IReadOnlyList<IBrowserFile>? _files;

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
            "NTForm" => (FormMarkup, "<NTForm Model=\"_contact\"", new[] { "FormName", "Enhance", "Appearance", "Density", "BindOnInput", "Disabled", "ReadOnly", "ShowRequiredSupportingText", "RequiredSupportingText" }),
            "NTFileUpload" => (FileUploadMarkup, "<NTFileUpload", new[] { "Label", "ChooseButtonText", "Multiple", "Accept", "MaximumFileCount", "MaximumFileSize", "AutoUpload", "ShowUploadButton", "UploadButtonText", "ReadyText", "UploadingText", "UploadCompleteText", "Disabled", "ReadOnly", "Required", "SupportingText" }),
            "NTContextMenu" => (ContextMenuMarkup, "<NTContextMenu", new[] { "AriaLabel", "Appearance", "CloseOnContentClick", "Disabled", "LongPressDelay", "Elevation", "ContainerColor", "TextColor", "SelectedContainerColor", "SelectedTextColor" }),
            "NTWindowHost" => (WindowHostMarkup, "<NTWindowHost", new[] { "AriaLabel" }),
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

    private void SubmitContact() => _formResult = $"Submitted {_contact.Name} ({_contact.Email}).";

    private async Task ReadSampleFileAsync(NTFileUploadEventArgs args) {
        if (args.Stream is not null) {
            await args.Stream.CopyToAsync(Stream.Null);
            _uploadResult = $"Read {args.Name}: {args.Size} bytes.";
        }
    }

    private void OpenManagedWindow() {
        if (_managedWindow is not null) {
            WindowService.Close(_managedWindow);
        }

        _managedWindow = WindowService.Open("Managed project notes", builder => builder.AddContent(0, "This window is rendered by NTWindowHost."));
    }

    public void Dispose() {
        if (_managedWindow is not null) {
            WindowService.Close(_managedWindow);
        }

        GC.SuppressFinalize(this);
    }

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
        return value ?? fallback;
    }

    private NTColor? ColorValue(string parameterName) => Value<NTColor?>(parameterName, null);

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

    private const string FormMarkup = """
        @using System.ComponentModel.DataAnnotations

        <NTForm Model="_contact" OnValidSubmit="SubmitContact">
            <DataAnnotationsValidator />
            <ValidationSummary />
            <NTInputText Label="Name" @bind-Value="_contact.Name" />
            <NTInputText Label="Email" InputType="TextInputType.Email" @bind-Value="_contact.Email" />
            <button type="submit">Submit example form</button>
            <output>@_formResult</output>
        </NTForm>

        @code {
            private readonly ContactModel _contact = new();
            private string? _formResult;
            private void SubmitContact() => _formResult = $"Submitted {_contact.Name} ({_contact.Email}).";
            private sealed class ContactModel {
                [Required] public string Name { get; set; } = "Ada Lovelace";
                [Required, EmailAddress] public string Email { get; set; } = "ada@example.com";
            }
        }
        """;

    private const string FileUploadMarkup = """
        @using Microsoft.AspNetCore.Components.Forms

        <NTFileUpload @bind-Value="_files" OnUploadFile="ReadSampleFileAsync" OnFileError="args => _uploadResult = args.ErrorMessage" />
        <output>@_uploadResult</output>
        <NTFileUploadItem Name="example.txt" Status="Ready" Percent="100" ShouldShowPercent="true" />

        @code {
            private string? _uploadResult;
            private IReadOnlyList<IBrowserFile>? _files;
            private async Task ReadSampleFileAsync(NTFileUploadEventArgs args) {
                if (args.Stream is not null) {
                    await args.Stream.CopyToAsync(Stream.Null);
                    _uploadResult = $"Read {args.Name}: {args.Size} bytes.";
                }
            }
        }
        """;

    private const string ContextMenuMarkup = """
        <NTContextMenu>
            <TargetContent><button type="button">Right-click or long-press for actions</button></TargetContent>
            <MenuContent>
                <NTMenuButtonItem Label="Archive example" Icon="MaterialIcon.Archive" OnClickCallback='_ => _contextResult = "Example archived"' />
            </MenuContent>
        </NTContextMenu>
        <output>@_contextResult</output>

        @code {
            private string? _contextResult;
        }
        """;

    private const string WindowHostMarkup = """
        @inject INTWindowService WindowService
        @implements IDisposable

        <NTButton Label="Open managed window" OnClickCallback="_ => OpenManagedWindow()" />
        <NTWindowHost />

        @code {
            private INTWindow? _managedWindow;
            private void OpenManagedWindow() {
                if (_managedWindow is not null) {
                    WindowService.Close(_managedWindow);
                }
                _managedWindow = WindowService.Open("Managed project notes", builder => builder.AddContent(0, "This window is rendered by NTWindowHost."));
            }
            public void Dispose() {
                if (_managedWindow is not null) {
                    WindowService.Close(_managedWindow);
                }
            }
        }
        """;

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

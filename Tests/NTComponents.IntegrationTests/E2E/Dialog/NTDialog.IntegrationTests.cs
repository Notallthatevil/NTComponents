using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Dialog;

/// <summary>
///     Browser-level coverage for owned NTDialog markup, native command handlers, Blazor ref opening, and Material 3 appearance.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTDialog_IntegrationTests : IAsyncLifetime {
    private PlaywrightFixture? _fixture;
    private IPage? _page;
    private string AppBaseUrl = default!;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        AppBaseUrl = _fixture.ServerAddress;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Dialog_Page_Verifies_Native_Interactive_And_Appearance_Behavior() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToDialogAsync();

        await VerifyDialogsAreClosedAndHiddenByDefaultAsync();
        await VerifyNativeCommandDialogCanOpenAndCloseAsync();
        await VerifyLargeBodyDialogCanOpenAndScrollAsync();
        await VerifyIconDialogCanOpenAndCloseAsync();
        await VerifyStackedDialogCanOpenAboveParentAsync();
        await VerifyDialogCanOpenFromJavaScriptAsync();
        await VerifyRenderModeDialogCanOpenAndCloseAsync("Open NT dialog", "NT dialog", "Opened through a component reference");
        await VerifyRenderModeDialogCanOpenAndCloseAsync("Open server NT dialog", "Server NT dialog", "Interactive Server");
        await VerifyRenderModeDialogCanOpenAndCloseAsync("Open WASM NT dialog", "WASM NT dialog", "Interactive WebAssembly");
    }

    [Fact]
    public async Task Dialog_Page_Keeps_Toast_And_Snackbar_In_The_Foreground_Layer() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToDialogAsync();

        await VerifyNotificationForegroundLayerAsync("Dialog then toast", ".nt-toast", "dialog-then-toast-demo");
        await VerifyNotificationForegroundLayerAsync("Toast then dialog", ".nt-toast", "toast-then-dialog-demo");
        await VerifyNotificationForegroundLayerAsync("Dialog then snackbar", ".nt-snackbar", "dialog-then-snackbar-demo");
        await VerifyNotificationForegroundLayerAsync("Snackbar then dialog", ".nt-snackbar", "snackbar-then-dialog-demo");
    }

    private async Task AssertDialogAppearanceAsync(ILocator dialog, string expectedTitle, bool hasIcon = false) {
        await dialog.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await dialog.EvaluateAsync(
            """
            async dialog => {
                await Promise.allSettled(dialog.getAnimations({ subtree: false }).map(animation => animation.finished));
            }
            """);

        var appearance = await dialog.EvaluateAsync<DialogAppearance>(
            """
            dialog => {
                const styles = getComputedStyle(dialog);
                const rect = dialog.getBoundingClientRect();
                const actionButton = dialog.querySelector('.nt-dialog-actions button');
                const actionButtonRect = actionButton?.getBoundingClientRect();
                const actions = dialog.querySelector('.nt-dialog-actions');
                const actionButtons = Array.from(dialog.querySelectorAll('.nt-dialog-actions button'));
                const actionButtonGap = actionButtons.length > 1
                    ? actionButtons[1].getBoundingClientRect().left - actionButtons[0].getBoundingClientRect().right
                    : Number.parseFloat(getComputedStyle(actions).gap);
                const content = dialog.querySelector('.nt-dialog-content');
                const contentStyles = content ? getComputedStyle(content) : null;
                const title = dialog.querySelector('.nt-dialog-title');
                const titleStyles = title ? getComputedStyle(title) : null;

                return {
                    actionButtonHeight: actionButtonRect?.height ?? 0,
                    actionButtonWidth: actionButtonRect?.width ?? 0,
                    actionGap: actionButtonGap,
                    actionsMarginTop: actions ? Number.parseFloat(getComputedStyle(actions).marginTop) : 0,
                    backgroundColor: styles.backgroundColor,
                    borderRadius: styles.borderRadius,
                    color: styles.color,
                    contentMarginTop: contentStyles ? Number.parseFloat(contentStyles.marginTop) : 0,
                    inlineSize: rect.width,
                    isOpen: dialog.open === true,
                    paddingBottom: Number.parseFloat(styles.paddingBottom),
                    paddingLeft: Number.parseFloat(styles.paddingLeft),
                    paddingRight: Number.parseFloat(styles.paddingRight),
                    paddingTop: Number.parseFloat(styles.paddingTop),
                    titleFontSize: titleStyles?.fontSize ?? '',
                    titleTextAlign: titleStyles?.textAlign ?? '',
                    titleText: title?.textContent?.trim() ?? ''
                };
            }
            """);

        appearance.IsOpen.Should().BeTrue("NTDialog must use the native modal <dialog> open state");
        appearance.TitleText.Should().Be(expectedTitle);
        appearance.BorderRadius.Should().Be("28px", "basic Material 3 dialogs use increased rounded corners");
        appearance.BackgroundColor.Should().NotBe("rgba(0, 0, 0, 0)", "the dialog container must render as a visible surface");
        appearance.Color.Should().NotBe("rgba(0, 0, 0, 0)", "dialog content must have a visible foreground color");
        appearance.InlineSize.Should().BeGreaterThan(250).And.BeLessThanOrEqualTo(560);
        appearance.PaddingTop.Should().BeApproximately(24, 0.5);
        appearance.PaddingRight.Should().BeApproximately(24, 0.5);
        appearance.PaddingBottom.Should().BeApproximately(24, 0.5);
        appearance.PaddingLeft.Should().BeApproximately(24, 0.5);
        appearance.ContentMarginTop.Should().BeApproximately(16, 0.5, "title-to-body spacing should match the dialog spec");
        appearance.ActionsMarginTop.Should().BeApproximately(24, 0.5, "body-to-actions spacing should match the dialog spec");
        appearance.ActionGap.Should().BeApproximately(8, 0.5, "button spacing should match the dialog spec");
        appearance.ActionButtonHeight.Should().BeApproximately(40, 0.5, "M3 dialog actions should preserve a 40px minimum touch target");
        appearance.ActionButtonWidth.Should().BeGreaterThan(40);
        appearance.TitleFontSize.Should().Be("24px", "dialog title should use the M3 headline-small scale");
        if (hasIcon) {
            appearance.TitleTextAlign.Should().Be("center", "dialogs with an icon should be center-aligned");
        } else {
            appearance.TitleTextAlign.Should().BeOneOf(["start", "left"], "dialogs without an icon should be start-aligned");
        }
    }

    private async Task NavigateToDialogAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await _page.GotoAsync($"{AppBaseUrl}/dialog", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForFunctionAsync("() => document.getElementById('native-nt-dialog') instanceof HTMLDialogElement", new PageWaitForFunctionOptions { Timeout = 10000 });
        await ClosePreopenedDialogAsync();
    }

    private async Task ClosePreopenedDialogAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        var preopenedDialog = _page.Locator("#preopened-nt-dialog");
        if (!await preopenedDialog.EvaluateAsync<bool>("dialog => dialog.open === true")) {
            return;
        }

        await preopenedDialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Close" }).ClickAsync();
        await _page.WaitForFunctionAsync("() => document.getElementById('preopened-nt-dialog')?.open === false");
    }

    private async Task VerifyDialogCanOpenFromJavaScriptAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.EvaluateAsync("() => document.getElementById('native-nt-dialog').showModal()");
        var dialog = _page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Exact = true, Name = "Native NT dialog" });
        await AssertDialogAppearanceAsync(dialog, "Native NT dialog");

        Directory.CreateDirectory(Path.Combine("output", "playwright"));
        await _page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true, Path = Path.Combine("output", "playwright", "ntdialog-open.png") });

        await _page.EvaluateAsync("() => document.getElementById('native-nt-dialog').close()");
        await _page.WaitForFunctionAsync("() => document.getElementById('native-nt-dialog')?.open === false");
    }

    private async Task VerifyNativeCommandDialogCanOpenAndCloseAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Exact = true, Name = "Open native NT dialog" }).ClickAsync();
        var dialog = _page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Exact = true, Name = "Native NT dialog" });
        await AssertDialogAppearanceAsync(dialog, "Native NT dialog");
        var dialogId = await dialog.GetAttributeAsync("id");
        dialogId.Should().Be("native-nt-dialog");
        await _page.Mouse.ClickAsync(10, 10);
        await _page.WaitForTimeoutAsync(100);
        await _page.WaitForFunctionAsync("() => document.getElementById('native-nt-dialog')?.open === true");

        await dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Close" }).ClickAsync();
        await _page.WaitForFunctionAsync("() => document.getElementById('native-nt-dialog')?.open === false");
    }

    private async Task VerifyDialogsAreClosedAndHiddenByDefaultAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        var closedState = await _page.EvaluateAsync<DialogClosedState[]>(
            """
            () => Array.from(document.querySelectorAll('dialog[data-nt-dialog="true"]')).map(dialog => {
                const rect = dialog.getBoundingClientRect();
                const styles = getComputedStyle(dialog);

                return {
                    display: styles.display,
                    height: rect.height,
                    id: dialog.id,
                    isOpen: dialog.open,
                    width: rect.width
                };
            })
            """);

        closedState.Should().NotBeEmpty();
        closedState.Should().OnlyContain(state => !state.IsOpen, "NTDialog instances should be closed on initial page load");
        closedState.Should().OnlyContain(state => state.Display == "none", "closed native dialogs should not render on the page");
        closedState.Should().OnlyContain(state => state.Width == 0 && state.Height == 0, "closed native dialogs should not occupy layout space");
    }

    private async Task VerifyLargeBodyDialogCanOpenAndScrollAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Exact = true, Name = "Open large NT dialog" }).ClickAsync();
        var dialog = _page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Exact = true, Name = "Large body dialog" });
        await AssertDialogAppearanceAsync(dialog, "Large body dialog");
        (await dialog.Locator(".nt-dialog-content p").CountAsync()).Should().BeGreaterThan(12);
        var scrollMetrics = await dialog.EvaluateAsync<DialogScrollMetrics>(
            """
            dialog => {
                const content = dialog.querySelector('.nt-dialog-content');
                const actions = dialog.querySelector('.nt-dialog-actions');
                const header = dialog.querySelector('.nt-dialog-header');
                const actionTopBefore = actions.getBoundingClientRect().top;
                const headerTopBefore = header.getBoundingClientRect().top;
                content.scrollTop = content.scrollHeight;
                const styles = getComputedStyle(content);

                return {
                    actionTopAfterScroll: actions.getBoundingClientRect().top,
                    actionTopBeforeScroll: actionTopBefore,
                    borderBottomWidth: Number.parseFloat(styles.borderBottomWidth),
                    borderTopWidth: Number.parseFloat(styles.borderTopWidth),
                    clientHeight: content.clientHeight,
                    headerTopAfterScroll: header.getBoundingClientRect().top,
                    headerTopBeforeScroll: headerTopBefore,
                    scrollHeight: content.scrollHeight,
                    scrollbarColor: styles.scrollbarColor,
                    scrollbarWidth: styles.scrollbarWidth
                };
            }
            """);

        scrollMetrics.ScrollHeight.Should().BeGreaterThan(scrollMetrics.ClientHeight, "the large-body demo should exercise the dialog body scroll container");
        scrollMetrics.ActionTopAfterScroll.Should().BeApproximately(scrollMetrics.ActionTopBeforeScroll, 0.5, "actions should stay outside the scrolling body");
        scrollMetrics.HeaderTopAfterScroll.Should().BeApproximately(scrollMetrics.HeaderTopBeforeScroll, 0.5, "header should stay outside the scrolling body");
        scrollMetrics.BorderTopWidth.Should().BeApproximately(1, 0.5, "scrollable dialog content should use 1dp dividers");
        scrollMetrics.BorderBottomWidth.Should().BeApproximately(1, 0.5, "scrollable dialog content should use 1dp dividers");
        scrollMetrics.ScrollbarWidth.Should().Be("thin", "dialog body should use the shared NT scrollbar styling");
        scrollMetrics.ScrollbarColor.Should().Contain("rgba(0, 0, 0, 0)", "dialog body should use the shared NT scrollbar track styling");

        await dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Close" }).ClickAsync();
        await _page.WaitForFunctionAsync("() => document.getElementById('large-nt-dialog')?.open === false");
    }

    private async Task VerifyIconDialogCanOpenAndCloseAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Exact = true, Name = "Open icon NT dialog" }).ClickAsync();
        var dialog = _page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Exact = true, Name = "Icon dialog" });
        await AssertDialogAppearanceAsync(dialog, "Icon dialog", hasIcon: true);
        var icon = dialog.Locator(".nt-dialog-icon .material-symbols-sharp");
        await icon.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
        var iconMetrics = await dialog.EvaluateAsync<DialogIconMetrics>(
            """
            dialog => {
                const icon = dialog.querySelector('.nt-dialog-icon .material-symbols-sharp');
                const iconContainer = dialog.querySelector('.nt-dialog-icon');
                const title = dialog.querySelector('.nt-dialog-title');
                const iconRect = icon.getBoundingClientRect();
                const iconContainerRect = iconContainer.getBoundingClientRect();
                const dialogRect = dialog.getBoundingClientRect();
                const titleRect = title.getBoundingClientRect();

                return {
                    iconFontSize: getComputedStyle(icon).fontSize,
                    iconHeight: iconRect.height,
                    iconToTitleGap: titleRect.top - iconContainerRect.bottom,
                    titleTextAlign: getComputedStyle(title).textAlign,
                    iconCenterOffset: Math.abs((iconContainerRect.left + iconContainerRect.width / 2) - (dialogRect.left + dialogRect.width / 2))
                };
            }
            """);

        iconMetrics.IconFontSize.Should().Be("24px", "icon size should match the dialog spec");
        iconMetrics.IconHeight.Should().BeApproximately(24, 0.5);
        iconMetrics.IconToTitleGap.Should().BeApproximately(16, 0.5, "icon-to-title spacing should match the dialog spec");
        iconMetrics.IconCenterOffset.Should().BeLessThan(1, "dialogs with an icon should be center-aligned");
        iconMetrics.TitleTextAlign.Should().Be("center", "dialogs with an icon should be center-aligned");
        (await dialog.Locator(".nt-dialog-icon").GetAttributeAsync("aria-hidden")).Should().Be("true");

        await dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Got it" }).ClickAsync();
        await _page.WaitForFunctionAsync("() => document.getElementById('icon-nt-dialog')?.open === false");
    }

    private async Task VerifyNotificationForegroundLayerAsync(string buttonName, string notificationSelector, string dialogId) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Exact = true, Name = buttonName }).ClickAsync();
        await _page.WaitForFunctionAsync(
            """
            ([selector, id]) => document.getElementById(id)?.open === true && document.querySelector(selector) !== null
            """,
            new object[] { notificationSelector, dialogId },
            new PageWaitForFunctionOptions { Timeout = 10000 });

        var foregroundState = await _page.EvaluateAsync<NotificationForegroundState>(
            """
            ([selector, id]) => {
                const notification = document.querySelector(selector);
                const dialog = document.getElementById(id);
                const rect = notification.getBoundingClientRect();
                const topElement = document.elementFromPoint(rect.left + rect.width / 2, rect.top + rect.height / 2);

                return {
                    dialogOpen: dialog?.open === true,
                    notificationVisible: rect.width > 0 && rect.height > 0,
                    topElementClass: topElement?.className?.toString() ?? '',
                    topElementTag: topElement?.tagName ?? '',
                    topMatchesNotification: topElement?.closest(selector) !== null || topElement?.querySelector(selector) !== null
                };
            }
            """,
            new object[] { notificationSelector, dialogId });

        foregroundState.DialogOpen.Should().BeTrue($"{dialogId} should be open for the foreground-layer assertion");
        foregroundState.NotificationVisible.Should().BeTrue($"{notificationSelector} should be visible while the dialog is open");
        foregroundState.TopMatchesNotification.Should().BeTrue($"{notificationSelector} should be above the modal dialog top-layer entry, but the top element was {foregroundState.TopElementTag} {foregroundState.TopElementClass}");

        await _page.Locator($"#{dialogId}").GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Close" }).ClickAsync();
        await _page.WaitForFunctionAsync("id => document.getElementById(id)?.open === false", dialogId);
        await _page.EvaluateAsync("() => { window.NTToast?.clearToasts(); window.NTSnackbar?.clearSnackbars(); }");
    }

    private async Task VerifyStackedDialogCanOpenAboveParentAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Exact = true, Name = "Open stacked NT dialog" }).ClickAsync();
        var parentDialog = _page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Exact = true, Name = "Stacked parent dialog" });
        await AssertDialogAppearanceAsync(parentDialog, "Stacked parent dialog");

        await parentDialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Open child dialog" }).ClickAsync();
        var childDialog = _page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Exact = true, Name = "Stacked child dialog" });
        await AssertDialogAppearanceAsync(childDialog, "Stacked child dialog");

        var stackedState = await _page.EvaluateAsync<StackedDialogState>(
            """
            () => {
                const parent = document.getElementById('stacked-parent-nt-dialog');
                const child = document.getElementById('stacked-child-nt-dialog');
                const childRect = child.getBoundingClientRect();
                const topElement = document.elementFromPoint(childRect.left + childRect.width / 2, childRect.top + Math.min(childRect.height / 2, 40));

                return {
                    childOpen: child.open === true,
                    parentOpen: parent.open === true,
                    topDialogId: topElement?.closest('dialog')?.id ?? ''
                };
            }
            """);

        stackedState.ParentOpen.Should().BeTrue("opening the child dialog should not close the parent dialog");
        stackedState.ChildOpen.Should().BeTrue("the child dialog should open as its own native modal dialog");
        stackedState.TopDialogId.Should().Be("stacked-child-nt-dialog", "the child dialog should render above the parent dialog");

        await childDialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Close child" }).ClickAsync();
        await _page.WaitForFunctionAsync("() => document.getElementById('stacked-child-nt-dialog')?.open === false && document.getElementById('stacked-parent-nt-dialog')?.open === true");

        await parentDialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Close parent" }).ClickAsync();
        await _page.WaitForFunctionAsync("() => document.getElementById('stacked-parent-nt-dialog')?.open === false");
    }

    private async Task VerifyRenderModeDialogCanOpenAndCloseAsync(string buttonName, string expectedTitle, string expectedSupportingText) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Exact = true, Name = buttonName }).ClickAsync();
        var dialog = _page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Exact = true, Name = expectedTitle });
        await AssertDialogAppearanceAsync(dialog, expectedTitle);
        (await dialog.Locator(".nt-dialog-supporting-text").TextContentAsync()).Should().Contain(expectedSupportingText);
        var dialogId = await dialog.GetAttributeAsync("id");
        dialogId.Should().NotBeNullOrWhiteSpace();

        await dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "OK" }).ClickAsync();
        await _page.WaitForFunctionAsync($"() => document.getElementById('{dialogId}')?.open === false");
        await _page.Locator(".nt-dialog-demo-status").Filter(new LocatorFilterOptions { HasText = $"OnClosed {dialogId} ok" }).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
    }

    private sealed class DialogAppearance {
        public double ActionButtonHeight { get; set; }
        public double ActionButtonWidth { get; set; }
        public double ActionGap { get; set; }
        public double ActionsMarginTop { get; set; }
        public string BackgroundColor { get; set; } = string.Empty;
        public string BorderRadius { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public double ContentMarginTop { get; set; }
        public double InlineSize { get; set; }
        public bool IsOpen { get; set; }
        public double PaddingBottom { get; set; }
        public double PaddingLeft { get; set; }
        public double PaddingRight { get; set; }
        public double PaddingTop { get; set; }
        public string TitleFontSize { get; set; } = string.Empty;
        public string TitleTextAlign { get; set; } = string.Empty;
        public string TitleText { get; set; } = string.Empty;
    }

    private sealed class DialogClosedState {
        public string Display { get; set; } = string.Empty;
        public double Height { get; set; }
        public string Id { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public double Width { get; set; }
    }

    private sealed class DialogIconMetrics {
        public double IconCenterOffset { get; set; }
        public string IconFontSize { get; set; } = string.Empty;
        public double IconHeight { get; set; }
        public double IconToTitleGap { get; set; }
        public string TitleTextAlign { get; set; } = string.Empty;
    }

    private sealed class DialogScrollMetrics {
        public double ActionTopAfterScroll { get; set; }
        public double ActionTopBeforeScroll { get; set; }
        public double BorderBottomWidth { get; set; }
        public double BorderTopWidth { get; set; }
        public double ClientHeight { get; set; }
        public double HeaderTopAfterScroll { get; set; }
        public double HeaderTopBeforeScroll { get; set; }
        public double ScrollHeight { get; set; }
        public string ScrollbarColor { get; set; } = string.Empty;
        public string ScrollbarWidth { get; set; } = string.Empty;
    }

    private sealed class StackedDialogState {
        public bool ChildOpen { get; set; }
        public bool ParentOpen { get; set; }
        public string TopDialogId { get; set; } = string.Empty;
    }

    private sealed class NotificationForegroundState {
        public bool DialogOpen { get; set; }
        public bool NotificationVisible { get; set; }
        public string TopElementClass { get; set; } = string.Empty;
        public string TopElementTag { get; set; } = string.Empty;
        public bool TopMatchesNotification { get; set; }
    }
}

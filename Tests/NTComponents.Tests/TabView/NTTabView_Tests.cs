using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace NTComponents.Tests.TabView;

public class NTTabView_Tests : BunitContext {
    public NTTabView_Tests() {
        Renderer.SetRendererInfo(new RendererInfo("Static", false));
        var tabViewModule = JSInterop.SetupModule("./_content/NTComponents/TabView/NTTabView.razor.js");
        tabViewModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        tabViewModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        tabViewModule.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public async Task SelectTabAsync_AfterInteractiveRender_ReturnsBrowserSelectionResult() {
        Renderer.SetRendererInfo(new RendererInfo("Server", true));
        var module = JSInterop.SetupModule(NTTabView.JsModulePathValue);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.Setup<bool>("selectTabByValue", invocation => invocation.Arguments[1] as string == "specs").SetResult(true);
        module.Setup<bool>("selectTabByValue", invocation => invocation.Arguments[1] as string == "missing").SetResult(false);
        var cut = RenderTabView();
        cut.WaitForAssertion(() => cut.Instance.IsolatedJsModule.Should().NotBeNull());
        var importCount = JSInterop.Invocations["import"].Count;

        (await cut.Instance.SelectTabAsync("specs", Xunit.TestContext.Current.CancellationToken)).Should().BeTrue();
        (await cut.Instance.SelectTabAsync("missing", Xunit.TestContext.Current.CancellationToken)).Should().BeFalse();
        module.Invocations["selectTabByValue"][0].Arguments[0].Should().Be(cut.Instance.Element);
        module.Invocations["selectTabByValue"][0].CancellationToken.Should().Be(Xunit.TestContext.Current.CancellationToken);
        JSInterop.Invocations["import"].Count.Should().Be(importCount);
    }

    [Fact]
    public async Task SelectTabAsync_WhileModuleImportIsPending_ThrowsHelpfulError() {
        using var context = new BunitContext();
        var import = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        var jsRuntime = Substitute.For<IJSRuntime>();
#pragma warning disable BL0016 // Configures a substitute; no browser interop executes here.
        jsRuntime.InvokeAsync<IJSObjectReference>("import", Arg.Any<object?[]>()).Returns(new ValueTask<IJSObjectReference>(import.Task));
#pragma warning restore BL0016
        context.Services.AddSingleton(jsRuntime);
        context.Renderer.SetRendererInfo(new RendererInfo("Server", true));
        var cut = context.Render<NTTabView>();

        var act = async () => await cut.Instance.SelectTabAsync("specs", Xunit.TestContext.Current.CancellationToken);

        try {
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*rendered interactively*");
        }
        finally {
            import.SetResult(Substitute.For<IJSObjectReference>());
        }
        cut.WaitForAssertion(() => cut.Instance.IsolatedJsModule.Should().NotBeNull());
    }

    [Fact]
    public async Task SelectTabAsync_AfterDisposal_RejectsSelectionWithoutCallingBrowser() {
        Renderer.SetRendererInfo(new RendererInfo("Server", true));
        var cut = RenderTabView();
        cut.WaitForAssertion(() => cut.Instance.IsolatedJsModule.Should().NotBeNull());
        await cut.Instance.DisposeAsync();

        var act = async () => await cut.Instance.SelectTabAsync("specs", Xunit.TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*rendered interactively*");
        JSInterop.VerifyNotInvoke("selectTabByValue");
        JSInterop.VerifyInvoke("onDispose", 1);
    }

    [Fact]
    public async Task SelectTabAsync_BrowserFailure_PropagatesErrorToCaller() {
        Renderer.SetRendererInfo(new RendererInfo("Server", true));
        var module = JSInterop.SetupModule(NTTabView.JsModulePathValue);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        var failure = new JSException("Tab selection failed");
        module.Setup<bool>("selectTabByValue", _ => true).SetException(failure);
        var cut = RenderTabView();
        cut.WaitForAssertion(() => cut.Instance.IsolatedJsModule.Should().NotBeNull());

        var act = async () => await cut.Instance.SelectTabAsync("specs", Xunit.TestContext.Current.CancellationToken);

        (await act.Should().ThrowExactlyAsync<JSException>()).Which.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task SelectTabAsync_StaticRendering_ThrowsHelpfulError() {
        var cut = RenderTabView();

        var act = async () => await cut.Instance.SelectTabAsync("specs", Xunit.TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*rendered interactively*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SelectTabAsync_EmptyValue_ThrowsArgumentException(string? value) {
        var cut = RenderTabView();

        var act = async () => await cut.Instance.SelectTabAsync(value!, Xunit.TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void RendersAccessibleTabsAndPanels() {
        var cut = RenderTabView();

        cut.Find("nt-tab-view").GetAttribute("class").Should().Contain("nt-tab-view");
        cut.Find("[role='tablist']").GetAttribute("aria-label").Should().Be("Tabs");
        cut.FindAll("[role='tab']").Should().HaveCount(2);
        cut.FindAll("[role='tabpanel']").Should().HaveCount(2);
        cut.FindAll("[role='tab'] > .nt-button-ripple-host").Should().HaveCount(2);
        cut.Find("[data-nt-tab-indicator]").Should().NotBeNull();
    }

    [Fact]
    public void Name_SetsQueryParameterMetadata() {
        var cut = RenderTabView(parameters => parameters.Add(p => p.Name, "details"));
        var tabView = cut.Find("nt-tab-view");

        tabView.GetAttribute("name").Should().Be("details");
        tabView.GetAttribute("data-nt-tab-view-name").Should().Be("details");
        tabView.GetAttribute("data-nt-tab-query-parameter").Should().Be("details");
    }

    [Fact]
    public void QueryParameterName_OverridesNameForQueryMetadata() {
        var cut = RenderTabView(parameters => parameters.Add(p => p.Name, "details").Add(p => p.QueryParameterName, "tab"));

        cut.Find("nt-tab-view").GetAttribute("data-nt-tab-query-parameter").Should().Be("tab");
    }

    [Fact]
    public void QueryParameter_SelectsMatchingPanelInStaticMarkup() {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/tabview?details=specs");

        var cut = RenderTabView(parameters => parameters.Add(p => p.Name, "details"));
        var tabs = cut.FindAll("[role='tab']");
        var panels = cut.FindAll("[role='tabpanel']");

        tabs[0].GetAttribute("aria-selected").Should().Be("false");
        tabs[1].GetAttribute("aria-selected").Should().Be("true");
        panels[0].HasAttribute("hidden").Should().BeTrue();
        panels[1].HasAttribute("hidden").Should().BeFalse();
    }

    [Fact]
    public void SelectedValue_SelectsMatchingPanelInStaticMarkup() {
        var cut = RenderTabView(parameters => parameters.Add(p => p.SelectedValue, "specs"));
        var tabs = cut.FindAll("[role='tab']");
        var panels = cut.FindAll("[role='tabpanel']");

        tabs[0].GetAttribute("aria-selected").Should().Be("false");
        tabs[1].GetAttribute("aria-selected").Should().Be("true");
        panels[0].HasAttribute("hidden").Should().BeTrue();
        panels[1].HasAttribute("hidden").Should().BeFalse();
    }

    [Fact]
    public void InvalidSelectedValue_FallsBackToFirstEnabledTabInStaticMarkup() {
        var cut = RenderTabView(parameters => parameters.Add(p => p.SelectedValue, "missing"));
        var tabs = cut.FindAll("[role='tab']");
        var panels = cut.FindAll("[role='tabpanel']");

        tabs[0].GetAttribute("aria-selected").Should().Be("true");
        tabs[1].GetAttribute("aria-selected").Should().Be("false");
        panels[0].HasAttribute("hidden").Should().BeFalse();
        panels[1].HasAttribute("hidden").Should().BeTrue();
    }

    [Fact]
    public void DisabledSelectedValue_FallsBackToFirstEnabledTabInStaticMarkup() {
        var cut = Render<NTTabView>(parameters => parameters
            .Add(p => p.SelectedValue, "overview")
            .AddChildContent(builder => {
                builder.OpenComponent<NTTab>(0);
                builder.AddAttribute(1, nameof(NTTab.Label), "Overview");
                builder.AddAttribute(2, nameof(NTTab.Value), "overview");
                builder.AddAttribute(3, nameof(NTTab.Disabled), true);
                builder.AddAttribute(4, nameof(NTTab.ChildContent), (RenderFragment)(child => child.AddContent(0, "Overview content")));
                builder.CloseComponent();
                builder.OpenComponent<NTTab>(5);
                builder.AddAttribute(6, nameof(NTTab.Label), "Specs");
                builder.AddAttribute(7, nameof(NTTab.Value), "specs");
                builder.AddAttribute(8, nameof(NTTab.ChildContent), (RenderFragment)(child => child.AddContent(0, "Specs content")));
                builder.CloseComponent();
            }));
        WaitForTabs(cut, 2);

        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").Should().Be("false");
        tabs[1].GetAttribute("aria-selected").Should().Be("true");
    }

    [Fact]
    public void DuplicateValues_RenderUniqueTabAndPanelIds() {
        var cut = Render<NTTabView>(parameters => parameters.Add(p => p.Id, "duplicate-tabs").AddChildContent(builder => {
            builder.OpenComponent<NTTab>(0);
            builder.AddAttribute(1, nameof(NTTab.Label), "Details");
            builder.AddAttribute(2, nameof(NTTab.Value), "details");
            builder.AddAttribute(3, nameof(NTTab.ChildContent), (RenderFragment)(child => child.AddContent(0, "First content")));
            builder.CloseComponent();
            builder.OpenComponent<NTTab>(4);
            builder.AddAttribute(5, nameof(NTTab.Label), "Details");
            builder.AddAttribute(6, nameof(NTTab.Value), "details");
            builder.AddAttribute(7, nameof(NTTab.ChildContent), (RenderFragment)(child => child.AddContent(0, "Second content")));
            builder.CloseComponent();
        }));
        WaitForTabs(cut, 2);

        var tabs = cut.FindAll("[role='tab']");
        var panels = cut.FindAll("[role='tabpanel']");

        tabs.Select(tab => tab.Id).Should().OnlyHaveUniqueItems();
        panels.Select(panel => panel.Id).Should().OnlyHaveUniqueItems();
        tabs[0].GetAttribute("aria-controls").Should().Be(panels[0].Id);
        panels[0].GetAttribute("aria-labelledby").Should().Be(tabs[0].Id);
    }

    [Fact]
    public void TabAriaLabel_RendersPreferredAriaLabel() {
        var cut = Render<NTTabView>(parameters => parameters.AddChildContent(builder => {
            builder.OpenComponent<NTTab>(0);
            builder.AddAttribute(1, nameof(NTTab.Label), "Info");
            builder.AddAttribute(2, nameof(NTTab.AriaLabel), "Information");
            builder.AddAttribute(3, nameof(NTTab.AccessibilityLabel), "Legacy information");
            builder.AddAttribute(4, nameof(NTTab.ChildContent), (RenderFragment)(child => child.AddContent(0, "Info content")));
            builder.CloseComponent();
        }));
        WaitForTabs(cut, 1);

        cut.Find("[role='tab']").GetAttribute("aria-label").Should().Be("Information");
    }

    [Fact]
    public void DisabledFirstTab_DoesNotBecomeInitialSelection() {
        var cut = Render<NTTabView>(parameters => parameters.AddChildContent(builder => {
            builder.OpenComponent<NTTab>(0);
            builder.AddAttribute(1, nameof(NTTab.Label), "Overview");
            builder.AddAttribute(2, nameof(NTTab.Value), "overview");
            builder.AddAttribute(3, nameof(NTTab.Disabled), true);
            builder.AddAttribute(4, nameof(NTTab.ChildContent), (RenderFragment)(child => child.AddContent(0, "Overview content")));
            builder.CloseComponent();
            builder.OpenComponent<NTTab>(5);
            builder.AddAttribute(6, nameof(NTTab.Label), "Specs");
            builder.AddAttribute(7, nameof(NTTab.Value), "specs");
            builder.AddAttribute(8, nameof(NTTab.ChildContent), (RenderFragment)(child => child.AddContent(0, "Specs content")));
            builder.CloseComponent();
        }));
        WaitForTabs(cut, 2);

        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").Should().Be("false");
        tabs[1].GetAttribute("aria-selected").Should().Be("true");
    }

    [Fact]
    public void ZeroTabs_CompletesRegistrationWithoutRenderingGhostTabsOrPanels() {
        var cut = Render<NTTabView>();

        cut.WaitForAssertion(() => {
            cut.Find("[role='tablist']").Should().NotBeNull();
            cut.FindAll("[role='tab']").Should().BeEmpty();
            cut.FindAll("[role='tabpanel']").Should().BeEmpty();
        });
    }

    [Fact]
    public void TabAddedAfterRegistration_RestartsRegistrationAndRendersCompleteHeader() {
        var cut = Render<NTTabView>(parameters => parameters.Add(p => p.ChildContent, RenderTabs(new TabDefinition("overview", "Overview"))));
        WaitForTabs(cut, 1);

        cut.Render(parameters => parameters.Add(p => p.ChildContent, RenderTabs(new TabDefinition("overview", "Overview"), new TabDefinition("specs", "Specifications"))));

        cut.WaitForAssertion(() => {
            cut.FindAll(".nt-tab-view-label").Select(label => label.TextContent).Should().Equal("Overview", "Specifications");
            cut.FindAll("[role='tabpanel']").Select(panel => panel.TextContent).Should().Equal("Overview content", "Specifications content");
            cut.FindAll("[role='tab'][aria-selected='true']").Should().ContainSingle();
        });
    }

    [Fact]
    public void RemovedAndReorderedTabs_DoNotLeaveStaleHeadersOrSelection() {
        var cut = Render<NTTabView>(parameters => parameters
            .Add(p => p.SelectedValue, "specs")
            .Add(p => p.ChildContent, RenderTabs(new TabDefinition("overview", "Overview"), new TabDefinition("specs", "Specifications"), new TabDefinition("history", "History"))));
        WaitForTabs(cut, 3);

        cut.Render(parameters => parameters
            .Add(p => p.SelectedValue, "specs")
            .Add(p => p.ChildContent, RenderTabs(new TabDefinition("history", "History"), new TabDefinition("overview", "Overview"))));

        cut.WaitForAssertion(() => {
            var tabs = cut.FindAll("[role='tab']");
            var panels = cut.FindAll("[role='tabpanel']");
            cut.FindAll(".nt-tab-view-label").Select(label => label.TextContent).Should().Equal("History", "Overview");
            panels.Select(panel => panel.TextContent).Should().Equal("History content", "Overview content");
            tabs[0].GetAttribute("aria-selected").Should().Be("true");
            panels[0].HasAttribute("hidden").Should().BeFalse();
            tabs[1].GetAttribute("aria-selected").Should().Be("false");
            panels[1].HasAttribute("hidden").Should().BeTrue();
        });
    }

    [Fact]
    public void SelectedTabBecomingDisabled_UpdatesHeaderAndPanelSelectionTogether() {
        var cut = Render<NTTabView>(parameters => parameters
            .Add(p => p.SelectedValue, "overview")
            .Add(p => p.ChildContent, RenderTabs(new TabDefinition("overview", "Overview"), new TabDefinition("specs", "Specifications"))));
        WaitForTabs(cut, 2);

        cut.Render(parameters => parameters
            .Add(p => p.SelectedValue, "overview")
            .Add(p => p.ChildContent, RenderTabs(new TabDefinition("overview", "Overview", true), new TabDefinition("specs", "Specifications"))));

        cut.WaitForAssertion(() => {
            var tabs = cut.FindAll("[role='tab']");
            var panels = cut.FindAll("[role='tabpanel']");
            tabs[0].GetAttribute("aria-disabled").Should().Be("true");
            tabs[0].GetAttribute("aria-selected").Should().Be("false");
            panels[0].HasAttribute("hidden").Should().BeTrue();
            tabs[1].GetAttribute("aria-selected").Should().Be("true");
            panels[1].HasAttribute("hidden").Should().BeFalse();
        });
    }

    [Fact]
    public void TabDisposedDuringInteractiveRegistration_DoesNotReappearAfterReplacementRegisters() {
        Renderer.SetRendererInfo(new RendererInfo("Server", true));
        var cut = Render<NTTabView>(parameters => parameters.Add(p => p.ChildContent, RenderTabs(new TabDefinition("temporary", "Temporary"))));

        cut.Render(parameters => parameters.Add(p => p.ChildContent, RenderTabs()));
        cut.Render(parameters => parameters.Add(p => p.ChildContent, RenderTabs(new TabDefinition("replacement", "Replacement"))));

        cut.WaitForAssertion(() => {
            cut.FindAll(".nt-tab-view-label").Select(label => label.TextContent).Should().Equal("Replacement");
            cut.FindAll("[role='tabpanel']").Select(panel => panel.TextContent).Should().Equal("Replacement content");
            cut.Markup.Should().NotContain("Temporary");
        });
    }

    [Fact]
    public void VariantSecondary_AppliesSecondaryClass() {
        var cut = RenderTabView(parameters => parameters.Add(p => p.Variant, NTTabViewVariant.Secondary));

        cut.Find("nt-tab-view").GetAttribute("class").Should().Contain("nt-tab-view-secondary");
    }

    [Fact]
    public void FullWidth_AppliesFullWidthClass() {
        var cut = RenderTabView(parameters => parameters.Add(p => p.FullWidth, true));

        cut.Find("nt-tab-view").GetAttribute("class").Should().Contain("nt-tab-view-full-width");
    }

    [Theory]
    [InlineData(NTTabViewTabAlignment.Center, "nt-tab-view-align-center")]
    [InlineData(NTTabViewTabAlignment.End, "nt-tab-view-align-end")]
    public void TabAlignment_AppliesAlignmentClass(NTTabViewTabAlignment alignment, string expectedClass) {
        var cut = RenderTabView(parameters => parameters.Add(p => p.TabAlignment, alignment));

        cut.Find("nt-tab-view").GetAttribute("class").Should().Contain(expectedClass);
    }

    [Fact]
    public void Compact_AppliesCompactClass() {
        var cut = RenderTabView(parameters => parameters.Add(p => p.Compact, true));

        cut.Find("nt-tab-view").GetAttribute("class").Should().Contain("nt-tab-view-compact");
    }

    [Fact]
    public void TabGap_RendersGapCssVariable() {
        var cut = RenderTabView(parameters => parameters.Add(p => p.TabGap, "8px"));

        cut.Find("nt-tab-view").GetAttribute("style").Should().Contain("--nt-tab-view-tab-gap:8px");
    }

    [Fact]
    public void ChildOutsideTabView_ThrowsHelpfulError() {
        var act = () => Render<NTTab>(parameters => parameters.Add(p => p.Label, "Orphan"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*NTTab*NTTabView*");
    }

    [Fact]
    public void InlineIcon_RendersAndSettlesWithEquivalentReplacements() {
        var evaluations = 0;
        var cut = RenderIconTab(() => {
            (++evaluations).Should().BeLessThan(20, "equivalent inline icons must not cause a render loop");
            return MaterialIcon.ReceiptLong;
        });
        cut.Find(".nt-tab-view-icon").TextContent.Should().Be("receipt_long");
        var previousEvaluations = evaluations;

        cut.Render();

        evaluations.Should().Be(previousEvaluations + 1, "an equivalent replacement needs no header refresh");
        cut.Find("[role='tab']").GetAttribute("aria-selected").Should().Be("true");
        cut.Find("[role='tabpanel']").TextContent.Should().Be("Preview content");
    }

    [Theory]
    [InlineData("glyph", null, "home")]
    [InlineData("appearance", "style", "'FILL' 1")]
    [InlineData("size", "class", "mi-large")]
    [InlineData("class", "class", "custom-icon")]
    [InlineData("style", "style", "color:red")]
    [InlineData("title", "title", "Updated title")]
    public void IconAppearanceChanged_RefreshesHeader(string property, string? attribute, string expected) {
        var icon = MaterialIcon.ReceiptLong;
        var cut = RenderIconTab(() => icon);
        var previousMarkup = cut.Find(".nt-tab-view-icon").InnerHtml;
        switch (property) {
            case "glyph": icon.Icon = "home"; break;
            case "appearance": icon.Appearance = IconAppearance.Filled; break;
            case "size": icon.Size = IconSize.Large; break;
            case "class": icon.AdditionalAttributes = new Dictionary<string, object> { ["class"] = "custom-icon" }; break;
            case "style": icon.AdditionalAttributes = new Dictionary<string, object> { ["style"] = "color:red" }; break;
            case "title": icon.ElementTitle = "Updated title"; break;
        }

        // Update the child alone: the header can change only if NTTab notifies NTTabView.
        cut.FindComponent<NTTab>().Render(parameters => parameters.Add(p => p.Icon, icon));

        cut.WaitForAssertion(() => cut.Find(".nt-tab-view-icon").InnerHtml.Should().NotBe(previousMarkup));
        var renderedIcon = cut.Find(".nt-tab-view-icon > span");
        (attribute is null ? renderedIcon.TextContent : renderedIcon.GetAttribute(attribute)).Should().Contain(expected);
    }

    [Fact]
    public void IconAddedAndRemoved_RefreshesHeader() {
        TnTIcon? icon = null;
        var cut = RenderIconTab(() => icon);
        cut.FindAll(".nt-tab-view-icon").Should().BeEmpty();

        icon = MaterialIcon.ReceiptLong;
        cut.FindComponent<NTTab>().Render(parameters => parameters.Add(p => p.Icon, icon));
        cut.WaitForAssertion(() => cut.Find(".nt-tab-view-icon").TextContent.Should().Be("receipt_long"));

        icon = null;
        cut.FindComponent<NTTab>().Render(parameters => parameters.Add(p => p.Icon, icon));
        cut.WaitForAssertion(() => cut.FindAll(".nt-tab-view-icon").Should().BeEmpty());
    }

    [Fact]
    public void InlineIcon_LabelAndSelectionChanges_UpdateHeaderAndPanel() {
        var label = "Preview";
        var cut = RenderIconTab(() => MaterialIcon.ReceiptLong, () => label);
        label = "Updated preview";

        cut.Render(parameters => parameters.Add(p => p.SelectedValue, "other"));

        cut.WaitForAssertion(() => {
            cut.Find(".nt-tab-view-label").TextContent.Should().Be(label);
            cut.Find("[data-nt-tab-value='other']").GetAttribute("aria-selected").Should().Be("true");
            cut.FindAll("[role='tabpanel']")[0].HasAttribute("hidden").Should().BeTrue();
            cut.FindAll("[role='tabpanel']")[1].HasAttribute("hidden").Should().BeFalse();
        });
    }

    private IRenderedComponent<NTTabView> RenderIconTab(Func<TnTIcon?> icon, Func<string>? label = null) {
        var renders = 0;
        var cut = Render<NTTabView>(parameters => parameters.Add(p => p.SelectedValue, "preview").AddChildContent(builder => {
            (++renders).Should().BeLessThan(30, "tab rendering must settle");
            builder.OpenComponent<NTTab>(0);
            builder.AddAttribute(1, nameof(NTTab.Label), label?.Invoke() ?? "Preview");
            builder.AddAttribute(2, nameof(NTTab.Value), "preview");
            builder.AddAttribute(3, nameof(NTTab.Icon), (object?)icon());
            builder.AddAttribute(4, nameof(NTTab.ChildContent), (RenderFragment)(content => content.AddContent(0, "Preview content")));
            builder.CloseComponent();
            builder.OpenComponent<NTTab>(5);
            builder.AddAttribute(6, nameof(NTTab.Label), "Other");
            builder.AddAttribute(7, nameof(NTTab.Value), "other");
            builder.AddAttribute(8, nameof(NTTab.ChildContent), (RenderFragment)(content => content.AddContent(0, "Other content")));
            builder.CloseComponent();
        }));
        WaitForTabs(cut, 2);
        return cut;
    }

    private IRenderedComponent<NTTabView> RenderTabView(Action<ComponentParameterCollectionBuilder<NTTabView>>? parameterBuilder = null) {
        var cut = Render<NTTabView>(parameters => {
            parameterBuilder?.Invoke(parameters);
            parameters.AddChildContent(builder => {
                builder.OpenComponent<NTTab>(0);
                builder.AddAttribute(1, nameof(NTTab.Label), "Overview");
                builder.AddAttribute(2, nameof(NTTab.Value), "overview");
                builder.AddAttribute(3, nameof(NTTab.ChildContent), (RenderFragment)(child => child.AddContent(0, "Overview content")));
                builder.CloseComponent();
                builder.OpenComponent<NTTab>(4);
                builder.AddAttribute(5, nameof(NTTab.Label), "Specifications");
                builder.AddAttribute(6, nameof(NTTab.Value), "specs");
                builder.AddAttribute(7, nameof(NTTab.ChildContent), (RenderFragment)(child => child.AddContent(0, "Specs content")));
                builder.CloseComponent();
            });
        });
        WaitForTabs(cut, 2);
        return cut;
    }

    private static RenderFragment RenderTabs(params TabDefinition[] tabs) => builder => {
        foreach (var tab in tabs) {
            builder.AddContent(0, RenderTab(tab));
        }
    };

    private static RenderFragment RenderTab(TabDefinition tab) => builder => {
        builder.OpenComponent<NTTab>(0);
        builder.SetKey(tab.Value);
        builder.AddAttribute(1, nameof(NTTab.Label), tab.Label);
        builder.AddAttribute(2, nameof(NTTab.Value), tab.Value);
        builder.AddAttribute(3, nameof(NTTab.Disabled), tab.Disabled);
        builder.AddAttribute(4, nameof(NTTab.ChildContent), (RenderFragment)(content => content.AddContent(0, $"{tab.Label} content")));
        builder.CloseComponent();
    };

    private static void WaitForTabs(IRenderedComponent<NTTabView> cut, int expectedCount) => cut.WaitForAssertion(() => cut.FindAll("[role='tab']").Should().HaveCount(expectedCount));

    private sealed record TabDefinition(string Value, string Label, bool Disabled = false);
}

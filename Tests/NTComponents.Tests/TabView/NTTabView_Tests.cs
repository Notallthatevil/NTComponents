using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace NTComponents.Tests.TabView;

public class NTTabView_Tests : BunitContext {
    public NTTabView_Tests() {
        var tabViewModule = JSInterop.SetupModule("./_content/NTComponents/TabView/NTTabView.razor.js");
        tabViewModule.SetupVoid("onLoad", _ => true);
        tabViewModule.SetupVoid("onUpdate", _ => true);
        tabViewModule.SetupVoid("onDispose", _ => true);
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

        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").Should().Be("false");
        tabs[1].GetAttribute("aria-selected").Should().Be("true");
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

    private IRenderedComponent<NTTabView> RenderTabView(Action<ComponentParameterCollectionBuilder<NTTabView>>? parameterBuilder = null) {
        return Render<NTTabView>(parameters => {
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
    }
}

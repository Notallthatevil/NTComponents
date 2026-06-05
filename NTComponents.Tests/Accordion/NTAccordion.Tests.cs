using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents.Tests.Accordion;

public class NTAccordion_Tests : BunitContext {

    [Fact]
    public void Default_Render_Uses_Native_Details_Without_Js_Wiring() {
        var cut = Render<NTAccordion>(p => p.AddChildContent(Items(Item("Summary", "Body"))));

        cut.Find("div.nt-accordion").GetAttribute("class")!.Should().Contain("nt-accordion-filled");
        cut.Find("details.nt-accordion-item");
        cut.Find("summary.nt-accordion-item-summary").TextContent.Should().Contain("Summary");
        cut.Markup.Should().Contain("Body");
        cut.Markup.Should().NotContain("onclick");
        cut.Markup.Should().NotContain("data-accordion");
    }

    [Fact]
    public void LimitToOneExpanded_Renders_Shared_Native_Details_Name() {
        var cut = Render<NTAccordion>(p => p
            .Add(a => a.LimitToOneExpanded, true)
            .Add(a => a.GroupName, "settings")
            .AddChildContent(Items(Item("First", "One"), Item("Second", "Two"))));

        var details = cut.FindAll("details.nt-accordion-item");

        details.Should().HaveCount(2);
        details.Select(x => x.GetAttribute("name")).Should().OnlyContain(name => name == "settings");
    }

    [Fact]
    public void LimitToOneExpanded_With_Multiple_Open_Items_Renders_Only_First_Open() {
        var cut = Render<NTAccordion>(p => p
            .Add(a => a.LimitToOneExpanded, true)
            .AddChildContent(Items(Item("First", "One", open: true), Item("Second", "Two", open: true))));

        cut.FindAll("details.nt-accordion-item[open]").Should().HaveCount(1);
        cut.Find("details.nt-accordion-item[open] summary").TextContent.Should().Contain("First");
    }

    [Fact]
    public void Multiple_Open_Items_Are_Allowed_When_Not_Limited() {
        var cut = Render<NTAccordion>(p => p.AddChildContent(Items(Item("First", "One", open: true), Item("Second", "Two", open: true))));

        cut.FindAll("details.nt-accordion-item[open]").Should().HaveCount(2);
    }

    [Fact]
    public void Open_Item_Uses_Native_Open_Attribute_Without_Static_Open_Class() {
        var cut = Render<NTAccordion>(p => p.AddChildContent(Items(Item("Summary", "Body", open: true))));

        cut.Find("details.nt-accordion-item[open]").GetAttribute("class")!.Should().NotContain("nt-accordion-item-open");
    }

    [Fact]
    public void Compact_Appearance_Adds_Compact_Class() {
        var cut = Render<NTAccordion>(p => p
            .Add(a => a.Appearance, NTAccordionAppearance.Compact)
            .AddChildContent(Items(Item("Summary", "Body"))));

        cut.Find(".nt-accordion").GetAttribute("class")!.Should().Contain("nt-accordion-compact");
        cut.Find("details.nt-accordion-item").GetAttribute("class")!.Should().Contain("nt-accordion-item-compact");
    }

    [Fact]
    public void Outlined_Variant_Does_Not_Emit_Filled_Surface_Color_Overrides() {
        var cut = Render<NTAccordion>(p => p
            .Add(a => a.Variant, NTAccordionVariant.Outlined)
            .AddChildContent(Items(Item("Summary", "Body"))));

        var root = cut.Find(".nt-accordion");

        root.GetAttribute("class")!.Should().Contain("nt-accordion-outlined");
        root.GetAttribute("style")!.Should().Contain("--nt-accordion-outline-color:var(--tnt-color-outline)");
        root.GetAttribute("style")!.Should().NotContain("--nt-accordion-header-color");
        root.GetAttribute("style")!.Should().NotContain("--nt-accordion-content-color");
    }

    [Fact]
    public void Disabled_Item_Renders_Static_NonInteractive_Surface() {
        var cut = Render<NTAccordion>(p => p.AddChildContent(Items(Item("Disabled", "Hidden", disabled: true))));

        var item = cut.Find("div.nt-accordion-item-disabled");

        item.GetAttribute("aria-disabled").Should().Be("true");
        cut.FindAll("details").Should().BeEmpty();
        cut.Markup.Should().NotContain("Hidden");
    }

    [Fact]
    public void Disabled_Open_Item_Renders_Static_Open_Class_And_Content() {
        var cut = Render<NTAccordion>(p => p.AddChildContent(Items(Item("Disabled", "Visible", open: true, disabled: true))));

        var item = cut.Find("div.nt-accordion-item-disabled");

        item.GetAttribute("class")!.Should().Contain("nt-accordion-item-open");
        cut.Markup.Should().Contain("Visible");
    }

    [Fact]
    public void Item_GroupName_Renders_Native_Details_Name_Without_Parent_Group() {
        var cut = Render<NTAccordion>(p => p.AddChildContent(builder => {
            builder.OpenComponent<NTAccordionItem>(0);
            builder.AddAttribute(1, nameof(NTAccordionItem.Label), "Standalone group");
            builder.AddAttribute(2, nameof(NTAccordionItem.GroupName), "item-group");
            builder.AddAttribute(3, nameof(NTAccordionItem.ChildContent), (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "Body")));
            builder.CloseComponent();
        }));

        cut.Find("details.nt-accordion-item").GetAttribute("name").Should().Be("item-group");
    }

    [Fact]
    public void Parent_LimitToOneExpanded_Overrides_Item_GroupName() {
        var cut = Render<NTAccordion>(p => p
            .Add(a => a.LimitToOneExpanded, true)
            .Add(a => a.GroupName, "parent-group")
            .AddChildContent(builder => {
                builder.OpenComponent<NTAccordionItem>(0);
                builder.AddAttribute(1, nameof(NTAccordionItem.Label), "Child group");
                builder.AddAttribute(2, nameof(NTAccordionItem.GroupName), "item-group");
                builder.AddAttribute(3, nameof(NTAccordionItem.ChildContent), (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "Body")));
                builder.CloseComponent();
            }));

        cut.Find("details.nt-accordion-item").GetAttribute("name").Should().Be("parent-group");
    }

    [Fact]
    public void UseRegionRole_Emits_Labelled_Content_Region() {
        var cut = Render<NTAccordion>(p => p.AddChildContent(builder => {
            builder.OpenComponent<NTAccordionItem>(0);
            builder.AddAttribute(1, nameof(NTAccordionItem.Label), "Region");
            builder.AddAttribute(2, nameof(NTAccordionItem.UseRegionRole), true);
            builder.AddAttribute(3, nameof(NTAccordionItem.ChildContent), (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "Body")));
            builder.CloseComponent();
        }));

        var summaryId = cut.Find("summary.nt-accordion-item-summary").GetAttribute("id");
        var content = cut.Find(".nt-accordion-item-content");

        content.GetAttribute("role").Should().Be("region");
        content.GetAttribute("aria-labelledby").Should().Be(summaryId);
    }

    [Fact]
    public void Grouping_Parameters_Update_On_Rerender() {
        var cut = Render<GroupingHost>();

        cut.Find("details.nt-accordion-item").HasAttribute("name").Should().BeFalse();

        cut.Instance.EnableGrouping();

        cut.WaitForAssertion(() => cut.Find("details.nt-accordion-item").GetAttribute("name").Should().Be("updated-group"));
    }

    [Fact]
    public void Item_Supports_HeaderContent_SupportingText_And_LeadingIcon() {
        TnTIcon leadingIcon = MaterialIcon.Info;

        var cut = Render<NTAccordion>(p => p.AddChildContent(builder => {
            builder.OpenComponent<NTAccordionItem>(0);
            builder.AddAttribute(1, nameof(NTAccordionItem.HeaderContent), (RenderFragment)(headerBuilder => headerBuilder.AddContent(0, "Custom header")));
            builder.AddAttribute(2, nameof(NTAccordionItem.SupportingText), "Secondary line");
            builder.AddComponentParameter(3, nameof(NTAccordionItem.LeadingIcon), leadingIcon);
            builder.AddAttribute(4, nameof(NTAccordionItem.ChildContent), (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "Body")));
            builder.CloseComponent();
        }));

        cut.Find(".nt-accordion-item-with-leading-icon");
        cut.Find(".nt-accordion-item-supporting-text").TextContent.Should().Be("Secondary line");
        cut.Find("summary").TextContent.Should().Contain("Custom header");
        cut.Find("summary").TextContent.Should().Contain("info");
    }

    [Fact]
    public void Custom_Colors_Are_Emitted_As_Css_Variables() {
        var cut = Render<NTAccordion>(p => p
            .Add(a => a.HeaderColor, TnTColor.PrimaryContainer)
            .Add(a => a.HeaderTextColor, TnTColor.OnPrimaryContainer)
            .Add(a => a.ContentColor, TnTColor.Surface)
            .Add(a => a.ContentTextColor, TnTColor.OnSurface)
            .Add(a => a.OutlineColor, TnTColor.Outline)
            .Add(a => a.StateLayerColor, TnTColor.OnPrimaryContainer)
            .AddChildContent(Items(Item("Summary", "Body"))));

        var style = cut.Find(".nt-accordion").GetAttribute("style")!;

        style.Should().Contain("--nt-accordion-header-color:var(--tnt-color-primary-container)");
        style.Should().Contain("--nt-accordion-header-text-color:var(--tnt-color-on-primary-container)");
        style.Should().Contain("--nt-accordion-content-color:var(--tnt-color-surface)");
        style.Should().Contain("--nt-accordion-content-text-color:var(--tnt-color-on-surface)");
        style.Should().Contain("--nt-accordion-outline-color:var(--tnt-color-outline)");
        style.Should().Contain("--nt-accordion-state-layer-color:var(--tnt-color-on-primary-container)");
    }

    [Fact]
    public void Item_HeaderTextColor_Does_Not_Override_Explicit_Parent_StateLayer_Color() {
        var cut = Render<NTAccordion>(p => p
            .Add(a => a.StateLayerColor, TnTColor.OnPrimaryContainer)
            .AddChildContent(builder => {
                builder.OpenComponent<NTAccordionItem>(0);
                builder.AddAttribute(1, nameof(NTAccordionItem.Label), "Custom state");
                builder.AddAttribute(2, nameof(NTAccordionItem.HeaderTextColor), TnTColor.OnSecondaryContainer);
                builder.AddAttribute(3, nameof(NTAccordionItem.ChildContent), (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "Body")));
                builder.CloseComponent();
            }));

        cut.Find(".nt-accordion").GetAttribute("style")!.Should().Contain("--nt-accordion-state-layer-color:var(--tnt-color-on-primary-container)");
        cut.Find("details.nt-accordion-item").GetAttribute("style")!.Should().NotContain("--nt-accordion-state-layer-color");
    }

    [Fact]
    public void Item_Requires_Label_Or_HeaderContent() {
        Action render = () => Render<NTAccordionItem>();

        render.Should().Throw<InvalidOperationException>().WithMessage("*Label*HeaderContent*");
    }

    private static Action<RenderTreeBuilder> Item(string label, string body, bool open = false, bool disabled = false) => builder => {
        builder.OpenComponent<NTAccordionItem>(0);
        builder.AddAttribute(1, nameof(NTAccordionItem.Label), label);
        builder.AddAttribute(2, nameof(NTAccordionItem.Open), open);
        builder.AddAttribute(3, nameof(NTAccordionItem.Disabled), disabled);
        builder.AddAttribute(4, nameof(NTAccordionItem.ChildContent), (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, body)));
        builder.CloseComponent();
    };

    private static RenderFragment Items(params Action<RenderTreeBuilder>[] items) => builder => {
        for (var i = 0; i < items.Length; i++) {
            items[i](builder);
        }
    };

    private sealed class GroupingHost : ComponentBase {
        private bool _limitToOneExpanded;

        public void EnableGrouping() {
            _limitToOneExpanded = true;
            InvokeAsync(StateHasChanged).GetAwaiter().GetResult();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTAccordion>(0);
            builder.AddAttribute(1, nameof(NTAccordion.LimitToOneExpanded), _limitToOneExpanded);
            builder.AddAttribute(2, nameof(NTAccordion.GroupName), "updated-group");
            builder.AddAttribute(3, nameof(NTAccordion.ChildContent), (RenderFragment)(contentBuilder => {
                contentBuilder.OpenComponent<NTAccordionItem>(0);
                contentBuilder.AddAttribute(1, nameof(NTAccordionItem.Label), "Summary");
                contentBuilder.AddAttribute(2, nameof(NTAccordionItem.ChildContent), (RenderFragment)(panelBuilder => panelBuilder.AddContent(0, "Body")));
                contentBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}

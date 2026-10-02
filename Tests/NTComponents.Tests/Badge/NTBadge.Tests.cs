using Microsoft.AspNetCore.Components;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Badge;

public class NTBadge_Tests : BunitContext {

    [Fact]
    public void Without_Content_Renders_Small_Badge_With_No_Label() {
        var cut = Render<NTBadge>();

        var badge = cut.Find("span.nt-badge");
        badge.ClassList.Should().Contain("nt-badge-small").And.NotContain("nt-badge-large");
        badge.TextContent.Trim().Should().BeEmpty();
        cut.FindAll(".nt-badge-label").Should().BeEmpty();
    }

    [Fact]
    public void Whitespace_Content_Renders_Small_Badge() {
        var cut = Render<NTBadge>(p => p.Add(x => x.Content, "  "));

        cut.Find("span.nt-badge").ClassList.Should().Contain("nt-badge-small");
        cut.FindAll(".nt-badge-label").Should().BeEmpty();
    }

    [Fact]
    public void With_Content_Renders_Large_Badge_Label() {
        var cut = Render<NTBadge>(p => p.Add(x => x.Content, "999+"));

        var badge = cut.Find("span.nt-badge");
        badge.ClassList.Should().Contain("nt-badge-large").And.NotContain("nt-badge-small");
        cut.Find(".nt-badge-label").TextContent.Should().Be("999+");
    }

    [Fact]
    public void Without_AriaLabel_Visible_Label_Is_Exposed_To_Assistive_Technology() {
        var cut = Render<NTBadge>(p => p.Add(x => x.Content, "3"));

        cut.Find(".nt-badge-label").HasAttribute("aria-hidden").Should().BeFalse();
        cut.FindAll(".nt-badge-accessible-label").Should().BeEmpty();
    }

    [Fact]
    public void With_AriaLabel_Hides_Visible_Label_And_Renders_Accessible_Label() {
        var cut = Render<NTBadge>(p => p
            .Add(x => x.Content, "3")
            .Add(x => x.AriaLabel, "3 unread messages"));

        cut.Find(".nt-badge-label").GetAttribute("aria-hidden").Should().Be("true");
        cut.Find(".nt-badge-accessible-label").TextContent.Should().Be("3 unread messages");
    }

    [Fact]
    public void Small_Badge_Renders_Accessible_Label() {
        var cut = Render<NTBadge>(p => p.Add(x => x.AriaLabel, "New activity"));

        cut.Find(".nt-badge-accessible-label").TextContent.Should().Be("New activity");
    }

    [Theory]
    [InlineData(NTBadgePlacement.Icon, null)]
    [InlineData(NTBadgePlacement.Edge, "nt-badge-edge")]
    [InlineData(NTBadgePlacement.Inline, "nt-badge-inline")]
    public void Placement_Adds_Matching_Class(NTBadgePlacement placement, string? expectedClass) {
        var cut = Render<NTBadge>(p => p.Add(x => x.Placement, placement));

        var classes = cut.Find("span.nt-badge").ClassList;
        classes.Where(c => c is "nt-badge-edge" or "nt-badge-inline").Should().Equal(expectedClass is null ? [] : [expectedClass]);
    }

    [Theory]
    [InlineData(null, false, null)]
    [InlineData("help", false, "help")]
    [InlineData(null, true, "badge")]
    [InlineData("  ", true, "badge")]
    [InlineData("help", true, "help badge")]
    public void GetDescribedBy_Appends_Badge_Id_Only_When_Shown(string? existing, bool showBadge, string? expected) {
        NTBadge.GetDescribedBy(existing, showBadge, "badge").Should().Be(expected);
    }

    [Fact]
    public void Applies_ElementId_And_Additional_Class() {
        var cut = Render<NTBadge>(p => p
            .Add(x => x.ElementId, "inbox-badge")
            .Add(x => x.AdditionalAttributes, new Dictionary<string, object> { ["class"] = "custom-badge" }));

        var badge = cut.Find("span.nt-badge");
        badge.Id.Should().Be("inbox-badge");
        badge.ClassList.Should().Contain("custom-badge");
    }

    [Fact]
    public void Badgeable_Components_Expose_Badge_Members_As_Parameters() {
        var implementations = typeof(NTBadge).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(INTBadgeable).IsAssignableFrom(type))
            .ToList();

        implementations.Should().Contain([typeof(NTIconButton), typeof(NTButton), typeof(NTFabButton), typeof(NTChip), typeof(NTNavigationRailItem), typeof(NTTab), typeof(NTMenuButtonItem), typeof(NTMenuAnchorItem), typeof(NTMenuSubMenuItem)]);
        foreach (var type in implementations) {
            foreach (var member in typeof(INTBadgeable).GetProperties()) {
                type.GetProperty(member.Name)!.IsDefined(typeof(ParameterAttribute), true).Should().BeTrue($"{type.Name}.{member.Name} must be a [Parameter]");
            }
        }
    }
}

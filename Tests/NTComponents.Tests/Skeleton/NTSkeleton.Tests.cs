namespace NTComponents.Tests.Skeleton;

public class NTSkeleton_Tests : BunitContext {

    [Fact]
    public void Default_Render_Is_Hidden_Wave_Text_Skeleton() {
        var cut = Render<NTSkeleton>();

        var skeleton = cut.Find("div.nt-skeleton");
        var cls = skeleton.GetAttribute("class")!;
        var style = skeleton.GetAttribute("style")!;

        cls.Should().Contain("nt-skeleton-text");
        cls.Should().Contain("nt-skeleton-wave");
        cls.Should().Contain("nt-corner-radius-small");
        skeleton.GetAttribute("aria-hidden").Should().Be("true");
        style.Should().Contain("--nt-skeleton-container-color:var(--tnt-color-surface-container-highest)");
        style.Should().NotContain("--nt-skeleton-highlight-color");
    }

    [Fact]
    public void AdditionalAttributes_Class_And_Style_Are_Merged() {
        var attrs = new Dictionary<string, object> {
            ["class"] = "custom-loading",
            ["style"] = "margin:4px"
        };

        var cut = Render<NTSkeleton>(parameters => parameters.Add(x => x.AdditionalAttributes, attrs));
        var skeleton = cut.Find("div.nt-skeleton");

        skeleton.GetAttribute("class")!.Should().Contain("custom-loading");
        skeleton.GetAttribute("style")!.Should().Contain("margin:4px");
        skeleton.GetAttribute("style")!.Should().Contain("--nt-skeleton-container-color");
    }

    [Theory]
    [InlineData(NTSkeletonShape.Text, "nt-skeleton-text", "nt-corner-radius-large")]
    [InlineData(NTSkeletonShape.Rectangle, "nt-skeleton-rectangle", "nt-corner-radius-large")]
    [InlineData(NTSkeletonShape.Circle, "nt-skeleton-circle", "nt-corner-radius-full")]
    public void Shape_Adds_Shape_And_Corner_Classes(NTSkeletonShape shape, string expectedShapeClass, string expectedCornerClass) {
        var cut = Render<NTSkeleton>(parameters => parameters
            .Add(x => x.Shape, shape)
            .Add(x => x.CornerRadius, NTCornerRadius.Large));

        var cls = cut.Find("div.nt-skeleton").GetAttribute("class")!;

        cls.Should().Contain(expectedShapeClass);
        cls.Should().Contain(expectedCornerClass);
    }

    [Theory]
    [InlineData(NTSkeletonAnimation.None, "nt-skeleton-wave", "nt-skeleton-pulse", false, false)]
    [InlineData(NTSkeletonAnimation.Pulse, "nt-skeleton-wave", "nt-skeleton-pulse", false, true)]
    [InlineData(NTSkeletonAnimation.Wave, "nt-skeleton-wave", "nt-skeleton-pulse", true, false)]
    public void Animation_Adds_Only_Selected_Animation_Class(NTSkeletonAnimation animation, string waveClass, string pulseClass, bool hasWave, bool hasPulse) {
        var cut = Render<NTSkeleton>(parameters => parameters.Add(x => x.Animation, animation));

        var cls = cut.Find("div.nt-skeleton").GetAttribute("class")!;

        cls.Contains(waveClass).Should().Be(hasWave);
        cls.Contains(pulseClass).Should().Be(hasPulse);
    }

    [Fact]
    public void Colors_And_Size_Render_As_Css_Variables() {
        var cut = Render<NTSkeleton>(parameters => parameters
            .Add(x => x.ContainerColor, TnTColor.PrimaryContainer)
            .Add(x => x.HighlightColor, TnTColor.OnPrimaryContainer)
            .Add(x => x.Width, "12rem")
            .Add(x => x.Height, "3rem"));

        var style = cut.Find("div.nt-skeleton").GetAttribute("style")!;

        style.Should().Contain("--nt-skeleton-container-color:var(--tnt-color-primary-container)");
        style.Should().Contain("--nt-skeleton-highlight-color:var(--tnt-color-on-primary-container)");
        style.Should().Contain("--nt-skeleton-width:12rem");
        style.Should().Contain("--nt-skeleton-height:3rem");
    }

    [Fact]
    public void ChildContent_Renders_In_Hidden_Content_Host() {
        var cut = Render<NTSkeleton>(parameters => parameters.AddChildContent("<strong>Loaded text</strong>"));

        var skeleton = cut.Find("div.nt-skeleton");

        skeleton.GetAttribute("class")!.Should().Contain("nt-skeleton-has-content");
        skeleton.QuerySelector(".nt-skeleton-content")!.InnerHtml.Should().Contain("<strong>Loaded text</strong>");
    }

    [Fact]
    public void HideFromAssistiveTechnology_False_Does_Not_Render_AriaHidden() {
        var cut = Render<NTSkeleton>(parameters => parameters.Add(x => x.HideFromAssistiveTechnology, false));

        cut.Find("div.nt-skeleton").HasAttribute("aria-hidden").Should().BeFalse();
    }

    [Fact]
    public void Show_False_Renders_Only_ChildContent() {
        var cut = Render<NTSkeleton>(parameters => parameters
            .Add(x => x.Show, false)
            .Add(x => x.AdditionalAttributes, new Dictionary<string, object> { ["class"] = "placeholder-shell" })
            .AddChildContent("<p class=\"loaded\">Loaded content</p>"));

        cut.FindAll(".nt-skeleton").Should().BeEmpty();
        cut.Find("p.loaded").TextContent.Should().Be("Loaded content");
        cut.Markup.Should().NotContain("placeholder-shell");
    }

    [Fact]
    public void Show_False_With_No_ChildContent_Renders_Nothing() {
        var cut = Render<NTSkeleton>(parameters => parameters.Add(x => x.Show, false));

        cut.Markup.Should().BeEmpty();
    }
}

namespace NTComponents.Tests.Animation;

public class NTAnimation_Tests : BunitContext {
    public static IEnumerable<object[]> AllAnimationTypes => Enum.GetValues<NTAnimationType>().Select(value => new object[] { value });

    [Theory]
    [MemberData(nameof(AllAnimationTypes))]
    public void Adds_Animation_Class(NTAnimationType animation) {
        var cut = Render<NTAnimation>(parameters => parameters.Add(component => component.Animation, animation).AddChildContent("Content"));

        cut.Find(".nt-animation").ClassList.Should().Contain(animation.ToCssClass());
    }

    [Fact]
    public void Renders_Child_Content() {
        var cut = Render<NTAnimation>(parameters => parameters.AddChildContent("<span>Animated</span>"));

        cut.Markup.Should().Contain("Animated");
    }

    [Fact]
    public void Renders_Static_Enhancement_Attributes() {
        var cut = Render<NTAnimation>(parameters => parameters
            .Add(component => component.AnimateOut, true)
            .Add(component => component.Once, false)
            .Add(component => component.RootMargin, "0px 0px -10% 0px")
            .Add(component => component.Threshold, 0.75)
            .AddChildContent("Content"));

        var element = cut.Find(".nt-animation");
        element.GetAttribute("data-nt-animation").Should().Be("true");
        element.GetAttribute("data-nt-animation-animate-out").Should().Be("true");
        element.GetAttribute("data-nt-animation-once").Should().Be("false");
        element.GetAttribute("data-nt-animation-root-margin").Should().Be("0px 0px -10% 0px");
        element.GetAttribute("data-nt-animation-threshold").Should().Be("0.75");
    }

    [Fact]
    public void Renders_Motion_Token_Styles() {
        var cut = Render<NTAnimation>(parameters => parameters
            .Add(component => component.EnterDuration, NTMotionDuration.Ms500)
            .Add(component => component.ExitDuration, NTMotionDuration.Ms150)
            .Add(component => component.EnterEasing, NTMotionEasing.EmphasizedDecelerate)
            .Add(component => component.ExitEasing, NTMotionEasing.StandardAccelerate)
            .Add(component => component.Delay, TimeSpan.FromMilliseconds(75))
            .Add(component => component.Distance, "32px")
            .AddChildContent("Content"));

        var style = cut.Find(".nt-animation").GetAttribute("style")!.Replace(" ", string.Empty);
        style.Should().Contain("--nt-animation-enter-duration:500ms");
        style.Should().Contain("--nt-animation-exit-duration:150ms");
        style.Should().Contain("--nt-animation-enter-easing:cubic-bezier(0.05,0.7,0.1,1)");
        style.Should().Contain("--nt-animation-exit-easing:cubic-bezier(0.3,0,1,1)");
        style.Should().Contain("--nt-animation-delay:75ms");
        style.Should().Contain("--nt-animation-distance:32px");
    }

    [Fact]
    public void Renders_Page_Script_For_Static_Enhancement() {
        var cut = Render<NTAnimation>(parameters => parameters.AddChildContent("Content"));

        cut.Find("tnt-page-script").GetAttribute("src").Should().Be("./_content/NTComponents/Animation/NTAnimation.razor.js");
    }

    [Fact]
    public void Clamps_Threshold_Attribute() {
        var cut = Render<NTAnimation>(parameters => parameters.Add(component => component.Threshold, 2).AddChildContent("Content"));

        cut.Find(".nt-animation").GetAttribute("data-nt-animation-threshold").Should().Be("1");
    }
}

namespace NTComponents.Tests.Core;

public class TnTHeadDependencies_Tests : BunitContext {

    [Fact]
    public void Render_Includes_MaterialSymbolsSharp_With_Fill_Axis_And_Widgets_Icon() {
        var cut = Render<TnTHeadDependencies>();

        cut.Markup.Should().Contain("https://fonts.googleapis.com/css2?family=Material+Symbols+Sharp:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200&amp;icon_names=widgets");
    }
}

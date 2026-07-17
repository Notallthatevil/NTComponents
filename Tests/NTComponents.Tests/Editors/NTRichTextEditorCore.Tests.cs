using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Editors;

public class NTRichTextEditorCore_Tests {

    [Fact]
    public void Button_Stores_All_Configured_Properties() {
        var button = new NTRichTextEditorButton(
            action: "heading",
            title: "Heading",
            ariaLabel: "Heading label",
            icon: MaterialIcon.LooksTwo,
            text: "H2",
            value: "2",
            shortcut: "Ctrl+Alt+2",
            cssClass: "custom-button");

        button.IsDivider.Should().BeFalse();
        button.Action.Should().Be("heading");
        button.Title.Should().Be("Heading");
        button.AriaLabel.Should().Be("Heading label");
        button.Icon.Should().BeOfType<MaterialIcon>();
        ((MaterialIcon)button.Icon!).Icon.Should().Be(MaterialIcon.LooksTwo.Icon);
        button.Text.Should().Be("H2");
        button.Value.Should().Be("2");
        button.Shortcut.Should().Be("Ctrl+Alt+2");
        button.CssClass.Should().Be("custom-button");
    }

    [Fact]
    public void Button_Falls_Back_To_Title_For_AriaLabel() {
        var button = new NTRichTextEditorButton("bold", "Bold title", ariaLabel: null, text: "Bold");

        button.AriaLabel.Should().Be("Bold title");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Button_Requires_Action(string? action) {
        var act = () => new NTRichTextEditorButton(action!, "Title", text: "X");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Button_Requires_Title(string? title) {
        var act = () => new NTRichTextEditorButton("bold", title!, text: "X");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Button_Requires_Icon_Or_Text() {
        var act = () => new NTRichTextEditorButton("bold", "Bold");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("text");
    }

    [Fact]
    public void Divider_Exposes_Null_Button_Metadata_And_CssClass() {
        var divider = new NTRichTextEditorButtonDivider("toolbar-divider");

        divider.IsDivider.Should().BeTrue();
        divider.Action.Should().BeNull();
        divider.Value.Should().BeNull();
        divider.Title.Should().BeNull();
        divider.Shortcut.Should().BeNull();
        divider.AriaLabel.Should().BeNull();
        divider.Icon.Should().BeNull();
        divider.Text.Should().BeNull();
        divider.CssClass.Should().Be("toolbar-divider");
    }

    [Fact]
    public void Tool_Stores_Configured_Metadata_And_Template() {
        RenderFragment<bool> template = disabled => builder => {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "data-disabled", disabled);
            builder.CloseElement();
        };

        var tool = new NTRichTextEditorTool("image", template, "./tool.js");

        tool.Action.Should().Be("image");
        tool.PanelTemplate.Should().BeSameAs(template);
        tool.JsModulePath.Should().Be("./tool.js");
    }

    [Fact]
    public void Tool_Allows_Null_Template_And_ModulePath() {
        var tool = new NTRichTextEditorTool("link");

        tool.Action.Should().Be("link");
        tool.PanelTemplate.Should().BeNull();
        tool.JsModulePath.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Tool_Requires_Action(string? action) {
        var act = () => new NTRichTextEditorTool(action!);

        act.Should().Throw<ArgumentException>();
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Buttons;

public class NTSplitButton_Tests : BunitContext {

    public NTSplitButton_Tests() {
        RippleTestingUtility.SetupRippleEffectModule(this);

        var splitButtonModule = JSInterop.SetupModule("./_content/NTComponents/Buttons/NTSplitButton.razor.js");
        splitButtonModule.SetupVoid("onLoad", _ => true);
        splitButtonModule.SetupVoid("onUpdate", _ => true);
        splitButtonModule.SetupVoid("onDispose", _ => true);
    }

    [Fact]
    public void Default_Render_Uses_Filled_Variant_And_Button_Type() {
        var cut = Render<ValidSplitButton>();

        var actionButton = cut.Find(".nt-split-button-leading");

        cut.Find("nt-split-button").GetAttribute("class")!.Should().Contain("nt-split-button-filled");
        actionButton.GetAttribute("type")!.Should().Be("button");
        actionButton.TextContent.Should().Contain("Save");
    }

    [Fact]
    public void Empty_Label_Without_LeadingIcon_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, string.Empty)
            .AddChildContent<NTSplitButtonButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*requires a non-empty Label unless a LeadingIcon is supplied*");
    }

    [Fact]
    public void Icon_Only_Action_Without_ActionAriaLabel_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.LeadingIcon, MaterialIcon.Save)
            .AddChildContent<NTSplitButtonButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*Icon-only NTSplitButton actions require a non-empty ActionAriaLabel*");
    }

    [Fact]
    public void Icon_Only_Action_With_ActionAriaLabel_Renders_Accessible_Label() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.LeadingIcon, MaterialIcon.Save)
            .Add(x => x.ActionAriaLabel, "Save")
            .AddChildContent<NTSplitButtonButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        var actionButton = cut.Find(".nt-split-button-leading");

        actionButton.GetAttribute("aria-label")!.Should().Be("Save");
        actionButton.QuerySelector(".nt-split-button-label").Should().BeNull();
    }

    [Theory]
    [InlineData(NTButtonVariant.Text)]
    [InlineData(NTButtonVariant.Outlined)]
    public void Transparent_Variants_With_Visible_BackgroundColor_Throw(NTButtonVariant variant) {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, variant)
            .Add(x => x.BackgroundColor, TnTColor.Primary)
            .AddChildContent<NTSplitButtonButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{variant} split buttons must use a transparent BackgroundColor*");
    }

    [Theory]
    [InlineData(NTButtonVariant.Elevated)]
    [InlineData(NTButtonVariant.Filled)]
    [InlineData(NTButtonVariant.Tonal)]
    public void Contained_Variants_With_Transparent_BackgroundColor_Throw(NTButtonVariant variant) {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, variant)
            .Add(x => x.BackgroundColor, TnTColor.Transparent)
            .AddChildContent<NTSplitButtonButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{variant} split buttons must use a visible container BackgroundColor*");
    }

    [Fact]
    public void Filled_Button_With_Elevation_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Filled)
            .Add(x => x.Elevation, NTElevation.Lowest)
            .AddChildContent<NTSplitButtonButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Filled split buttons must use None Elevation*");
    }

    [Fact]
    public void Elevated_Button_With_No_Elevation_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Elevated)
            .Add(x => x.Elevation, NTElevation.None)
            .AddChildContent<NTSplitButtonButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Elevated split buttons must use a non-zero Elevation*");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Menu_Colors_Must_Be_Visible(bool validateBackground) {
        var render = () => Render<NTSplitButton>(parameters => {
            parameters.Add(x => x.Label, "Invalid");

            if (validateBackground) {
                parameters.Add(x => x.MenuBackgroundColor, TnTColor.Transparent);
            }
            else {
                parameters.Add(x => x.MenuTextColor, TnTColor.Transparent);
            }

            parameters.AddChildContent<NTSplitButtonButtonItem>(item => item.Add(x => x.Label, "Save draft"));
        });

        render.Should().Throw<InvalidOperationException>()
            .WithMessage(validateBackground
                ? "*MenuBackgroundColor must be a visible menu container color*"
                : "*MenuTextColor must be a visible menu content color*");
    }

    [Fact]
    public void Requires_At_Least_One_Actionable_Menu_Item() {
        var render = () => Render<NTSplitButton>(parameters => parameters.Add(x => x.Label, "Save"));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires at least one NTSplitButtonButtonItem or NTSplitButtonAnchorItem child*");
    }

    [Fact]
    public void Divider_Only_Menu_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTSplitButtonDividerItem>());

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires at least one NTSplitButtonButtonItem or NTSplitButtonAnchorItem child*");
    }

    [Fact]
    public void Button_Item_Requires_Label() {
        var item = new NTSplitButtonButtonItem {
            Parent = new NTSplitButton()
        };
        var parameters = ParameterView.Empty;

        var render = () => item.SetParametersAsync(parameters).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTSplitButtonButtonItem requires a non-empty Label*");
    }

    [Fact]
    public void Anchor_Item_Requires_Href() {
        var item = new NTSplitButtonAnchorItem {
            Parent = new NTSplitButton()
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> {
            [nameof(NTSplitButtonAnchorItem.Label)] = "Export"
        });

        var render = () => item.SetParametersAsync(parameters).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTSplitButtonAnchorItem requires a non-empty Href*");
    }

    [Fact]
    public void Menu_Item_Whitespace_AriaLabel_Falls_Back_To_Visible_Label() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTSplitButtonButtonItem>(item => item
                .Add(x => x.Label, "Save draft")
                .Add(x => x.AriaLabel, "   ")));

        cut.Find(".nt-split-button-menu-item").GetAttribute("aria-label")!.Should().Be("Save draft");
    }

    private sealed class ValidSplitButton : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTSplitButton>(0);
            builder.AddAttribute(1, nameof(NTSplitButton.Label), "Save");
            builder.AddAttribute(2, nameof(NTSplitButton.ChildContent), (RenderFragment)(childBuilder => {
                childBuilder.OpenComponent<NTSplitButtonButtonItem>(0);
                childBuilder.AddAttribute(1, nameof(NTSplitButtonButtonItem.Label), "Save draft");
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}

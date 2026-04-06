using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Chips;

public class TnTChip_Tests : BunitContext {
    private const string RippleJsModulePath = "./_content/NTComponents/Core/TnTRippleEffect.razor.js";

    public TnTChip_Tests() {
        // Arrange (global) & Act: JS module setup for ripple in constructor
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.AutoFocus, true));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void CloseButton_Click_Invokes_Callback() {
        // Arrange
        var closeClicked = false;
        var callback = EventCallback.Factory.Create<MouseEventArgs>(this, () => closeClicked = true);
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.CloseButtonClicked, callback));

        // Act
        cut.Find("button").Click(); // Close button is rendered as TnTImageButton which renders a button

        // Assert
        closeClicked.Should().BeTrue();
    }

    [Fact]
    public void CloseButton_TextColor_Changes_Based_On_Value_State_False() {
        // Arrange
        var callback = EventCallback.Factory.Create<MouseEventArgs>(this, () => { });

        // Act - Value is false (default)
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.CloseButtonClicked, callback));

        // Assert - Should use TextColor when value is false
        var closeButton = cut.FindComponent<TnTImageButton>();
        closeButton.Instance.TextColor.Should().Be(TnTColor.OnSecondaryContainer); // default TextColor
    }

    [Fact]
    public void CloseButton_TextColor_Changes_Based_On_Value_State_True() {
        // Arrange
        var callback = EventCallback.Factory.Create<MouseEventArgs>(this, () => { });

        // Act - Value is true
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.Value, true).Add(c => c.CloseButtonClicked, callback));

        // Assert - Should use OnTintColor when value is true
        var closeButton = cut.FindComponent<TnTImageButton>();
        closeButton.Instance.TextColor.Should().Be(TnTColor.OnPrimary); // default OnTintColor
    }

    [Fact]
    public void CloseButtonClicked_Delegate_Renders_Close_Button() {
        // Arrange
        var callback = EventCallback.Factory.Create<MouseEventArgs>(this, () => { });

        // Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.CloseButtonClicked, callback));

        // Assert
        cut.Markup.Should().Contain("tnt-image-button");
        cut.Markup.Should().Contain("close");
    }

    [Fact]
    public void Custom_Colors_Set_Style_Variables() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.BackgroundColor, TnTColor.Primary)
            .Add(c => c.TextColor, TnTColor.OnPrimary)
            .Add(c => c.TintColor, TnTColor.Secondary)
            .Add(c => c.OnTintColor, TnTColor.OnSecondary));
        var style = cut.Find("label").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-chip-background-color:var(--tnt-color-primary)");
        style.Should().Contain("--tnt-chip-text-color:var(--tnt-color-on-primary)");
        style.Should().Contain("--tnt-chip-on-tint-color:var(--tnt-color-on-secondary)");
        style.Should().Contain("--tnt-chip-tint-color:var(--tnt-color-secondary)");
    }

    [Fact]
    public void Default_Style_Variables_Are_Set() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test"));
        var style = cut.Find("label").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-chip-background-color:var(--tnt-color-secondary-container)");
        style.Should().Contain("--tnt-chip-text-color:var(--tnt-color-on-secondary-container)");
        style.Should().Contain("--tnt-chip-on-tint-color:var(--tnt-color-on-primary)");
        style.Should().Contain("--tnt-chip-tint-color:var(--tnt-color-surface-tint)");
    }

    [Fact]
    public void Disabled_Adds_Disabled_Class_And_Attribute() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.Disabled, true));
        var root = cut.Find("label");
        var input = cut.Find("input");

        // Assert
        root.GetAttribute("class")!.Should().Contain("tnt-disabled");
        input.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Disabled_Input_Still_Renders_Ripple_Effect() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.Disabled, true));

        // Assert
        cut.Markup.Should().Contain("tnt-ripple-effect");
    }

    [Fact]
    public void DisableToggle_Makes_Input_ReadOnly() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.DisableToggle, true));
        var root = cut.Find("label");
        var input = cut.Find("input");

        // Assert
        root.GetAttribute("class")!.Should().NotContain("tnt-interactable");
        input.HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.ElementId, "chip-id"));
        var root = cut.Find("label");

        // Assert
        root.GetAttribute("id").Should().Be("chip-id");
    }

    [Fact]
    public void ElementLang_Sets_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.ElementLang, "en-US"));
        var root = cut.Find("label");

        // Assert
        root.GetAttribute("lang").Should().Be("en-US");
    }

    [Fact]
    public void ElementName_Sets_Input_Name_Attribute() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.ElementName, "test-chip"));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("name").Should().Be("test-chip");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.ElementTitle, "Chip Tooltip"));
        var root = cut.Find("label");

        // Assert
        root.GetAttribute("title").Should().Be("Chip Tooltip");
    }

    [Fact]
    public void Empty_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, ""));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void EnableRipple_False_Removes_Ripple_Class_And_Component() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.EnableRipple, false));
        var cls = cut.Find("label").GetAttribute("class")!;

        // Assert
        cls.Should().NotContain("tnt-ripple");
        cut.Markup.Should().NotContain("tnt-ripple-effect");
    }

    [Fact]
    public void EnableRipple_True_Renders_Ripple_Component() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.Markup.Should().Contain("tnt-ripple-effect");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test"));
        var root = cut.Find("label.tnt-chip input");

        // Assert
        root.HasAttribute("tntid").Should().BeTrue();
        root.GetAttribute("tntid").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Input_Change_Invokes_BindAfter_Callback() {
        // Arrange
        var bindAfterCalled = false;
        var bindAfterValue = false;
        var callback = EventCallback.Factory.Create<bool>(this, (value) => {
            bindAfterCalled = true;
            bindAfterValue = value;
        });
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.BindAfter, callback));
        var input = cut.Find("input");

        // Act
        input.Change(true);

        // Assert
        bindAfterCalled.Should().BeTrue();
        bindAfterValue.Should().BeTrue();
    }

    [Fact]
    public void Input_Change_Updates_Value_And_Invokes_ValueChanged() {
        // Arrange
        var valueChanged = false;
        var newValue = false;
        var callback = EventCallback.Factory.Create<bool>(this, (value) => {
            valueChanged = true;
            newValue = value;
        });
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.ValueChanged, callback));
        var input = cut.Find("input");

        // Act
        input.Change(true);

        // Assert
        valueChanged.Should().BeTrue();
        newValue.Should().BeTrue();
    }

    [Fact]
    public void Input_Change_With_False_Value_Updates_Correctly() {
        // Arrange
        var valueChanged = false;
        var newValue = true;
        var callback = EventCallback.Factory.Create<bool>(this, (value) => {
            valueChanged = true;
            newValue = value;
        });
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.Value, true).Add(c => c.ValueChanged, callback));
        var input = cut.Find("input");

        // Act
        input.Change(false);

        // Assert
        valueChanged.Should().BeTrue();
        newValue.Should().BeFalse();
    }

    [Fact]
    public void Input_Passes_Additional_Attributes_To_Input_Element() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "data-test", "chip-input" },
            { "aria-label", "Custom chip" }
        };

        // Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.AdditionalAttributes, attrs));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("data-test").Should().Be("chip-input");
        input.GetAttribute("aria-label").Should().Be("Custom chip");
    }

    [Fact]
    public void Merges_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-class" } };

        // Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("label").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-class");
        cls.Should().Contain("tnt-chip");
    }

    [Fact]
    public void Merges_Custom_Style_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:5px" } };

        // Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("label").GetAttribute("style")!;

        // Assert
        style.Should().Contain("margin:5px");
        style.Should().Contain("--tnt-chip-background-color");
    }

    [Fact]
    public void No_CloseButtonClicked_Delegate_Does_Not_Render_Close_Button() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.Markup.Should().NotContain("close");
    }

    [Fact]
    public void No_StartIcon_Does_Not_Render_Icon() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.Markup.Should().NotContain("tnt-start-icon");
    }

    [Fact]
    public void Null_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, null!));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void ReadOnly_Does_Not_Affect_Input_Readonly_Attribute() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.ReadOnly, true));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("readonly").Should().BeFalse();
    }

    [Fact]
    public void Renders_Checkbox_Input_With_Correct_Attributes() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.Value, true));
        var input = cut.Find("input[type=checkbox]");

        // Assert
        input.GetAttribute("type").Should().Be("checkbox");
        input.GetAttribute("value").Should().Be("True");
        input.HasAttribute("checked").Should().BeTrue();
        input.GetAttribute("name").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Renders_Label_With_Default_Classes() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test Chip"));
        var root = cut.Find("label.tnt-chip");

        // Assert
        var cls = root.GetAttribute("class")!;
        cls.Should().Contain("tnt-chip");
        cls.Should().Contain("tnt-components");
        cls.Should().Contain("tnt-interactable");
        cls.Should().Contain("tnt-ripple");
        cut.Find(".tnt-label").TextContent.Should().Be("Test Chip");
    }

    [Fact]
    public void StartIcon_Renders_Icon_Component() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.StartIcon, MaterialIcon.Star));

        // Assert
        cut.Markup.Should().Contain("tnt-start-icon");
        cut.Markup.Should().Contain("star");
    }

    [Fact]
    public void Two_Way_Binding_Works_Correctly() {
        // Arrange
        var component = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.Value, false));
        var input = component.Find("input");

        // Act
        input.Change(true);

        // Assert
        component.Instance.Value.Should().BeTrue();
        input.HasAttribute("checked").Should().BeTrue();
    }

    [Fact]
    public void Value_False_Checkbox_Not_Checked() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.Value, false));
        var input = cut.Find("input[type=checkbox]");

        // Assert
        input.HasAttribute("checked").Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Value_Property_Reflects_Checkbox_State(bool initialValue) {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.Value, initialValue));
        var input = cut.Find("input");

        // Assert
        cut.Instance.Value.Should().Be(initialValue);
        if (initialValue) {
            input.HasAttribute("checked").Should().BeTrue();
        }
        else {
            input.HasAttribute("checked").Should().BeFalse();
        }
    }

    [Fact]
    public void Whitespace_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "   "));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void SubLabel_Null_Does_Not_Render_SubLabel_Span() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.FindAll(".tnt-sub-label").Should().BeEmpty();
    }

    [Fact]
    public void SubLabel_Empty_Does_Not_Render_SubLabel_Span() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.SubLabel, ""));

        // Assert
        cut.FindAll(".tnt-sub-label").Should().BeEmpty();
    }

    [Fact]
    public void SubLabel_Whitespace_Does_Not_Render_SubLabel_Span() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.SubLabel, "   "));

        // Assert
        cut.FindAll(".tnt-sub-label").Should().BeEmpty();
    }

    [Fact]
    public void SubLabel_Set_Renders_SubLabel_Span_Inside_Label() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.SubLabel, "Details"));

        // Assert
        cut.Find(".tnt-label .tnt-sub-label").TextContent.Should().Be("Details");
    }

    [Fact]
    public void SubLabel_Set_Label_Text_Still_Renders() {
        // Arrange & Act
        var cut = Render<TnTChip>(p => p.Add(c => c.Label, "Test").Add(c => c.SubLabel, "Details"));

        // Assert
        cut.Find(".tnt-label").TextContent.Should().Contain("Test");
        cut.Find(".tnt-label").TextContent.Should().Contain("Details");
    }
}

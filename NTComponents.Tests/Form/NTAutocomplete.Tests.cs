using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTAutocomplete_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/NTAutocomplete.razor.js";

    private static readonly IReadOnlyList<AutocompleteOption> Options = [
        new("Austin", "Austin", "Texas"),
        new("Boston", "Boston", "Massachusetts"),
        new("Dallas", "Dallas", "Texas")
    ];

    public NTAutocomplete_Tests() {
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    private sealed record AutocompleteOption(string Value, string Label, string? SupportingText = null, bool Disabled = false, TnTIcon? LeadingIcon = null);

    private sealed class RequiredModel {
        [Required]
        public string? City { get; set; }
    }

    private sealed class TestModel {
        public string? City { get; set; }
    }

    [Fact]
    public void Renders_Text_Input_And_Closed_Menu_Shell() {
        var cut = RenderAutocomplete(configure: parameters => parameters
            .Add(p => p.ElementId, "city-autocomplete")
            .Add(p => p.Label, "City"));

        var input = cut.Find("input[role='combobox']");
        input.GetAttribute("id").Should().Be("city-autocomplete");
        input.GetAttribute("name").Should().Be("model.City");
        input.HasAttribute("list").Should().BeFalse();
        input.GetAttribute("value").Should().BeNull();
        input.GetAttribute("aria-controls").Should().Be("city-autocomplete-listbox");
        input.GetAttribute("data-nt-autocomplete-input").Should().Be("true");
        cut.Find("ul[role='listbox']").GetAttribute("id").Should().Be("city-autocomplete-listbox");
        cut.FindAll(".nt-combobox-list > .nt-combobox-list-item [data-nt-autocomplete-option='true']").Should().BeEmpty();
        cut.FindAll("script[type='application/json'][data-nt-autocomplete-option-definition='true']").Should().HaveCount(4);
        cut.Find(".nt-combobox-menu").GetAttribute("popover").Should().Be("manual");
        cut.FindAll("datalist").Should().BeEmpty();
    }

    [Fact]
    public void Renders_Static_Option_Metadata_For_Typescript_Enhancement() {
        var cut = RenderAutocomplete();

        var metadata = RenderedOptionMetadata(cut);

        cut.FindAll(".nt-combobox-list > .nt-combobox-list-item").Should().BeEmpty();
        metadata.Should().Contain("Austin");
        metadata.Should().Contain("customFormat");
    }

    [Fact]
    public void Renders_Option_Group_Metadata_From_ChildContent() {
        var model = new TestModel {
            City = "Austin"
        };

        var cut = Render<NTAutocomplete>(parameters => parameters
            .Add(p => p.Value, model.City)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.City = value))
            .Add(p => p.ValueExpression, (Expression<Func<string?>>)(() => model.City))
            .Add(p => p.AllowCustomValue, false)
            .Add(p => p.ChildContent, builder => {
                builder.OpenComponent<NTAutocompleteOptionGroup>(0);
                builder.AddAttribute(1, nameof(NTAutocompleteOptionGroup.Label), "Texas");
                builder.AddAttribute(2, nameof(NTAutocompleteOptionGroup.ChildContent), (RenderFragment)(groupBuilder => {
                    groupBuilder.OpenComponent<NTAutocompleteOption>(0);
                    groupBuilder.AddAttribute(1, nameof(NTAutocompleteOption.Value), "Austin");
                    groupBuilder.AddAttribute(2, nameof(NTAutocompleteOption.Label), "Austin");
                    groupBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }));

        var metadata = RenderedOptionMetadata(cut);

        metadata.Should().Contain("\"group\":\"Texas\"");
    }

    [Fact]
    public void Native_Change_Updates_Bound_Value_Without_Js_Selection() {
        var model = new TestModel();
        string? bindAfterValue = null;

        var cut = RenderAutocomplete(model, parameters => parameters
            .Add(p => p.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfterValue = value)));

        cut.Find("input[role='combobox']").Change("Austin");

        model.City.Should().Be("Austin");
        bindAfterValue.Should().Be("Austin");
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Austin");
    }

    [Fact]
    public void Custom_Value_Option_Renders_By_Default() {
        var cut = RenderAutocomplete(configure: parameters => parameters
            .Add(p => p.CustomValueOptionFormat, "Add {0}"));

        var metadata = RenderedOptionMetadata(cut);

        metadata.Should().Contain("\"isCustom\":true");
        metadata.Should().Contain("\"customFormat\":\"Add {0}\"");
    }

    [Fact]
    public void Custom_Value_Option_Does_Not_Render_When_Custom_Values_Are_Disallowed() {
        var cut = RenderAutocomplete(configure: parameters => parameters
            .Add(p => p.AllowCustomValue, false));

        var metadata = RenderedOptionMetadata(cut);

        metadata.Should().NotContain("\"isCustom\":true");
    }

    [Theory]
    [InlineData("Use {0", "Use {0")]
    [InlineData("Use {1}", "Use {1}")]
    [InlineData("Use {0} or {0}", "Use  or {0}")]
    public void Custom_Value_Option_Format_Uses_Safe_Literal_Placeholder_Replacement(string format, string expectedText) {
        var cut = RenderAutocomplete(configure: parameters => parameters
            .Add(p => p.CustomValueOptionFormat, format));

        var metadata = RenderedOptionMetadata(cut);

        metadata.Should().Contain(format);
        FormatCustomValueOptionTextForTest(format, string.Empty).Should().Be(expectedText);
    }

    [Fact]
    public void Condensed_MenuItemAppearance_Renders_Root_Class() {
        var cut = RenderAutocomplete(configure: parameters => parameters
            .Add(p => p.MenuItemAppearance, NTMenuItemAppearance.Condensed));

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-autocomplete-menu-items-condensed");
    }

    [Fact]
    public void Renders_Only_Local_Autocomplete_Metadata() {
        var cut = RenderAutocomplete();

        var input = cut.Find("input[role='combobox']");

        input.HasAttribute("data-nt-autocomplete-http-url").Should().BeFalse();
        input.HasAttribute("data-nt-autocomplete-http-parameters").Should().BeFalse();
        input.HasAttribute("data-nt-autocomplete-http-min-search-length").Should().BeFalse();
        cut.FindAll("[data-nt-autocomplete-form-value='true']").Should().BeEmpty();
    }

    [Fact]
    public void Allows_Custom_Value_Does_Not_Constrain_Form_Post_Parameter_Binding() {
        var cut = RenderAutocomplete(configure: parameters => parameters
            .AddUnmatched("name", "Input.City"));

        var input = cut.Find("input[role='combobox']");

        input.GetAttribute("name").Should().Be("Input.City");
        input.HasAttribute("pattern").Should().BeFalse();
    }

    [Fact]
    public void Disallow_Custom_Value_Renders_Option_Metadata_For_Form_Post_Parameter_Binding() {
        var model = new TestModel();
        var options = new[] {
            new AutocompleteOption("Austin", "Austin"),
            new AutocompleteOption("A/B (North)", "A/B (North)"),
            new AutocompleteOption("Phoenix", "Phoenix", Disabled: true)
        };

        var cut = RenderAutocomplete(model, parameters => parameters
            .Add(p => p.ChildContent, RenderOptions(options))
            .Add(p => p.AllowCustomValue, false)
            .AddUnmatched("name", "Input.City"));

        var input = cut.Find("input[role='combobox']");

        input.GetAttribute("name").Should().Be("Input.City");
        RenderedOptionMetadata(cut).Should().Contain("A/B (North)");
    }

    [Fact]
    public void Disallow_Custom_Value_Rejects_Non_Item_Value() {
        var model = new TestModel();
        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTAutocomplete>(0);
                builder.AddAttribute(1, nameof(NTAutocomplete.Value), model.City);
                builder.AddAttribute(2, nameof(NTAutocomplete.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.City = value));
                builder.AddAttribute(3, nameof(NTAutocomplete.ValueExpression), (Expression<Func<string?>>)(() => model.City));
                builder.AddAttribute(4, nameof(NTAutocomplete.ChildContent), RenderOptions(Options));
                builder.AddAttribute(5, nameof(NTAutocomplete.AllowCustomValue), false);
                builder.CloseComponent();
            }));

        cut.Find("input[role='combobox']").Change("Phoenix");
        cut.Find("input[role='combobox']").Blur();

        model.City.Should().BeNull();
        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-invalid");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The City field must match one of the available options.");
    }

    [Fact]
    public void Disallow_Custom_Value_Accepts_Item_Value() {
        var model = new TestModel();

        var cut = RenderAutocomplete(model, parameters => parameters
            .Add(p => p.AllowCustomValue, false));

        cut.Find("input[role='combobox']").Change("Austin");

        model.City.Should().Be("Austin");
        cut.Find(".nt-input").GetAttribute("class").Should().NotContain("nt-invalid");
    }

    [Fact]
    public void Enhancement_JsException_Keeps_Native_Input_Usable() {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetException(new JSException("Enhancement failed"));
        var model = new TestModel();

        var cut = context.Render<NTAutocomplete>(parameters => parameters
            .Add(p => p.ElementId, "city-autocomplete")
            .Add(p => p.Value, model.City)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.City = value))
            .Add(p => p.ValueExpression, (Expression<Func<string?>>)(() => model.City))
            .Add(p => p.ChildContent, RenderOptions(Options)));

        var input = cut.Find("input[role='combobox']");

        input.HasAttribute("list").Should().BeFalse();
        input.GetAttribute("name").Should().Be("model.City");
        cut.FindAll("datalist").Should().BeEmpty();
    }

    [Fact]
    public async Task NotifyValueChanged_Updates_Bound_Value_And_Invokes_BindAfter() {
        var model = new TestModel();
        string? bindAfterValue = null;

        var cut = RenderAutocomplete(model, parameters => parameters
            .Add(p => p.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfterValue = value)));

        await cut.InvokeAsync(() => cut.Instance.NotifyAutocompleteValueChanged("Boston", closeMenu: true));

        model.City.Should().Be("Boston");
        bindAfterValue.Should().Be("Boston");
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Boston");
    }

    [Fact]
    public void Inherits_Form_Appearance_Density_And_Disabled_State() {
        var model = new TestModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Appearance, NTFormAppearance.Filled)
            .Add(p => p.Density, NTFormDensity.Dense)
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTAutocomplete>(0);
                builder.AddAttribute(1, nameof(NTAutocomplete.Value), model.City);
                builder.AddAttribute(2, nameof(NTAutocomplete.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.City = value));
                builder.AddAttribute(3, nameof(NTAutocomplete.ValueExpression), (Expression<Func<string?>>)(() => model.City));
                builder.AddAttribute(4, nameof(NTAutocomplete.ChildContent), RenderOptions(Options));
                builder.CloseComponent();
            }));

        var rootClass = cut.Find(".nt-input").GetAttribute("class")!;
        rootClass.Should().Contain("nt-input-filled");
        rootClass.Should().Contain("nt-input-dense");
        rootClass.Should().Contain("nt-input-disabled");
        cut.Find("input[role='combobox']").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Blur_Validates_Required_Field_And_Renders_Error_State() {
        var model = new RequiredModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTAutocomplete>(1);
                builder.AddAttribute(2, nameof(NTAutocomplete.Value), model.City);
                builder.AddAttribute(3, nameof(NTAutocomplete.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.City = value));
                builder.AddAttribute(4, nameof(NTAutocomplete.ValueExpression), (Expression<Func<string?>>)(() => model.City));
                builder.AddAttribute(5, nameof(NTAutocomplete.ChildContent), RenderOptions(Options));
                builder.CloseComponent();
            }));

        cut.Find("input[role='combobox']").Blur();

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-invalid");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The City field is required.");
        cut.Find("input[role='combobox']").GetAttribute("aria-invalid").Should().Be("true");
    }

    private IRenderedComponent<NTAutocomplete> RenderAutocomplete(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTAutocomplete>>? configure = null) {
        model ??= new TestModel();
        return Render<NTAutocomplete>(parameters => {
            parameters
                .Add(p => p.Value, model.City)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.City = value))
                .Add(p => p.ValueExpression, (Expression<Func<string?>>)(() => model.City))
                .Add(p => p.ChildContent, RenderOptions(Options));
            configure?.Invoke(parameters);
        });
    }

    private static RenderFragment RenderOptions(IEnumerable<AutocompleteOption> options) => builder => {
        foreach (var option in options) {
            builder.OpenComponent<NTAutocompleteOption>(0);
            builder.AddAttribute(1, nameof(NTAutocompleteOption.Value), option.Value);
            builder.AddAttribute(2, nameof(NTAutocompleteOption.Label), option.Label);
            builder.AddAttribute(3, nameof(NTAutocompleteOption.SupportingText), option.SupportingText);
            builder.AddAttribute(4, nameof(NTAutocompleteOption.Disabled), option.Disabled);
            if (option.LeadingIcon is not null) {
                builder.AddAttribute(5, nameof(NTAutocompleteOption.LeadingIcon), option.LeadingIcon);
            }

            builder.CloseComponent();
        }
    };

    private static string RenderedOptionMetadata(IRenderedComponent<NTAutocomplete> cut) => string.Concat(cut.FindAll("script[type='application/json'][data-nt-autocomplete-option-definition='true']").Select(script => script.TextContent));

    private static string FormatCustomValueOptionTextForTest(string format, string value) {
        var placeholderIndex = format.IndexOf("{0}", StringComparison.Ordinal);
        return placeholderIndex < 0
            ? format
            : string.Concat(format.AsSpan(0, placeholderIndex), value, format.AsSpan(placeholderIndex + 3));
    }
}




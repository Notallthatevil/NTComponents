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
    public void Dynamically_Added_Option_Matching_The_Bound_Value_Renders_Selected() {
        var model = new TestModel { City = "2971" };
        var options = new[] { new AutocompleteOption("2971", "2971") };

        var cut = RenderAutocomplete(model, parameters => parameters.Add(p => p.AllowCustomValue, true), options);

        var optionMetadata = cut.FindAll("script[type='application/json'][data-nt-autocomplete-option-definition='true']")
            .Select(script => script.TextContent)
            .Single(metadata => metadata.Contains("\"value\":\"2971\"", StringComparison.Ordinal));
        optionMetadata.Should().Contain("\"selected\":true");
        optionMetadata.Should().Contain("nt-combobox-option-selected");
    }

    // Behavior source: Option metadata exposes optional icons, supporting text, fallback labels, disabled state, and group metadata to the enhancement module.
    [Fact]
    public void Option_Metadata_Renders_All_Documented_Optional_States() {
        var icon = new MaterialIcon("location_on") {
            Appearance = IconAppearance.Filled,
            ElementTitle = "Location"
        };
        var options = new[] {
            new AutocompleteOption("Austin", null!, "Texas capital", Disabled: true, LeadingIcon: icon)
        };

        var cut = RenderAutocomplete(options: options);
        var metadata = RenderedOptionMetadata(cut);

        metadata.Should().Contain("\"label\":\"Austin\"");
        metadata.Should().Contain("\"supportingText\":\"Texas capital\"");
        metadata.Should().Contain("\"disabled\":true");
        metadata.Should().Contain("\"leadingIcon\"");
        metadata.Should().Contain("font-variation-settings");
        metadata.Should().Contain("\"title\":\"Location\"");
    }

    // Behavior source: NTAutocompleteOption explicitly requires an NTAutocomplete ancestor.
    [Fact]
    public void Option_Outside_Autocomplete_Throws_Contract_Error() {
        var act = () => Render<NTAutocompleteOption>(parameters => parameters
            .Add(p => p.Value, "Austin")
            .Add(p => p.Label, "Austin"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must be a child of NTAutocomplete*");
    }

    // Behavior source: Blazor parameter rerenders must refresh the inert option metadata consumed by progressive enhancement.
    [Fact]
    public void Option_Metadata_Refreshes_After_Each_Documented_Parameter_Changes() {
        var groupDisabled = false;
        var groupLabel = "Texas";
        var disabled = false;
        var label = "Austin";
        var supportingText = "Capital";
        var value = "AUS";
        TnTIcon? leadingIcon = null;
        var model = new TestModel();
        RenderFragment options = builder => {
            builder.OpenComponent<NTAutocompleteOptionGroup>(0);
            builder.AddAttribute(1, nameof(NTAutocompleteOptionGroup.Disabled), groupDisabled);
            builder.AddAttribute(2, nameof(NTAutocompleteOptionGroup.Label), groupLabel);
            builder.AddAttribute(3, nameof(NTAutocompleteOptionGroup.ChildContent), (RenderFragment)(optionBuilder => {
                optionBuilder.OpenComponent<NTAutocompleteOption>(0);
                optionBuilder.AddAttribute(1, nameof(NTAutocompleteOption.Disabled), disabled);
                optionBuilder.AddAttribute(2, nameof(NTAutocompleteOption.Label), label);
                optionBuilder.AddAttribute(3, nameof(NTAutocompleteOption.SupportingText), supportingText);
                optionBuilder.AddAttribute(4, nameof(NTAutocompleteOption.Value), value);
                if (leadingIcon is not null) {
                    optionBuilder.AddAttribute(5, nameof(NTAutocompleteOption.LeadingIcon), (object)leadingIcon);
                }
                optionBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
        var cut = Render<NTAutocomplete>(parameters => parameters
            .Add(p => p.Value, model.City)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, selected => model.City = selected))
            .Add(p => p.ValueExpression, () => model.City)
            .Add(p => p.ChildContent, options));

        label = "Austin City";
        cut.Render();
        RenderedOptionMetadata(cut).Should().Contain("Austin City");
        supportingText = "State capital";
        cut.Render();
        RenderedOptionMetadata(cut).Should().Contain("State capital");
        leadingIcon = MaterialIcon.Place;
        cut.Render();
        RenderedOptionMetadata(cut).Should().Contain("place");
        value = "ATX";
        cut.Render();
        RenderedOptionMetadata(cut).Should().Contain("ATX");
        disabled = true;
        cut.Render();
        RenderedOptionMetadata(cut).Should().Contain("\"disabled\":true");
        disabled = false;
        cut.Render();
        groupDisabled = true;
        cut.Render();
        RenderedOptionMetadata(cut).Should().Contain("\"disabled\":true");
        groupLabel = "Central Texas";
        cut.Render();
        RenderedOptionMetadata(cut).Should().Contain("Central Texas");
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
    public void Native_Change_Allows_Custom_Value_Without_Rendering_An_Option_Pattern() {
        var model = new TestModel();
        var cut = RenderAutocomplete(model);
        var input = cut.Find("input[role='combobox']");
        const string customValue = "0005 - FARM: NURSERY EMPLOYEES & DRIVERS";

        input.Change(customValue);

        model.City.Should().Be(customValue);
        input.GetAttribute("value").Should().Be(customValue);
        input.HasAttribute("pattern").Should().BeFalse();
    }

    // Behavior source: Disabled and ReadOnly field contracts prevent native input events from changing the bound value.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Native_Change_Does_Not_Update_A_Disabled_Or_ReadOnly_Field(bool disabled, bool readOnly) {
        var model = new TestModel { City = "Austin" };
        var bindAfterCalls = 0;
        var cut = RenderAutocomplete(model, parameters => parameters
            .Add(p => p.Disabled, disabled)
            .Add(p => p.ReadOnly, readOnly)
            .Add(p => p.BindAfter, EventCallback.Factory.Create<string?>(this, _ => bindAfterCalls++)));

        cut.Find("input[role='combobox']").Change("Boston");

        model.City.Should().Be("Austin");
        bindAfterCalls.Should().Be(0);
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
    public void Disallow_Custom_Value_Does_Not_Render_Option_Values_As_A_Native_Pattern() {
        var model = new TestModel();
        var options = new[] {
            new AutocompleteOption("Austin", "Austin"),
            new AutocompleteOption("A/B (North)", "A/B (North)"),
            new AutocompleteOption("Phoenix", "Phoenix", Disabled: true)
        };

        var cut = RenderAutocomplete(model, parameters => parameters
            .Add(p => p.AllowCustomValue, false)
            .AddUnmatched("name", "Input.City"), options);
        cut.Render();

        var input = cut.Find("input[role='combobox']");

        input.GetAttribute("name").Should().Be("Input.City");
        input.HasAttribute("pattern").Should().BeFalse();
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

    // Behavior source: The Value contract is nullable, and the exact-match restriction applies only to non-empty typed values.
    [Fact]
    public void Disallow_Custom_Value_Accepts_Empty_Value() {
        var model = new TestModel { City = "Austin" };
        var cut = RenderAutocomplete(model, parameters => parameters.Add(p => p.AllowCustomValue, false));

        cut.Find("input[role='combobox']").Change(string.Empty);

        model.City.Should().BeEmpty();
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

    // Behavior source: Disabled and ReadOnly field contracts also apply to browser-module value notifications.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task NotifyValueChanged_Does_Not_Update_A_Disabled_Or_ReadOnly_Field(bool disabled, bool readOnly) {
        var model = new TestModel { City = "Austin" };
        var bindAfterCalls = 0;
        var cut = RenderAutocomplete(model, parameters => parameters
            .Add(p => p.Disabled, disabled)
            .Add(p => p.ReadOnly, readOnly)
            .Add(p => p.BindAfter, EventCallback.Factory.Create<string?>(this, _ => bindAfterCalls++)));

        await cut.InvokeAsync(() => cut.Instance.NotifyAutocompleteValueChanged("Boston", closeMenu: false));

        model.City.Should().Be("Austin");
        bindAfterCalls.Should().Be(0);
    }

    // Behavior source: Component disposal must tolerate a disconnected browser circuit so teardown remains safe.
    [Fact]
    public async Task DisposeAsync_Ignores_JsDisconnectedException() {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetException(new JSDisconnectedException("Disconnected"));
        var model = new TestModel();
        var cut = context.Render<NTAutocomplete>(parameters => parameters
            .Add(p => p.Value, model.City)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.City = value))
            .Add(p => p.ValueExpression, () => model.City)
            .Add(p => p.ChildContent, RenderOptions(Options)));

        var act = () => cut.Instance.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync();
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

    private IRenderedComponent<NTAutocomplete> RenderAutocomplete(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTAutocomplete>>? configure = null, IEnumerable<AutocompleteOption>? options = null) {
        model ??= new TestModel();
        return Render<NTAutocomplete>(parameters => {
            parameters
                .Add(p => p.Value, model.City)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.City = value))
                .Add(p => p.ValueExpression, (Expression<Func<string?>>)(() => model.City))
                .Add(p => p.ChildContent, RenderOptions(options ?? Options));
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
            if (option.LeadingIcon is { } leadingIcon) {
                builder.AddAttribute(5, nameof(NTAutocompleteOption.LeadingIcon), (object)leadingIcon);
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




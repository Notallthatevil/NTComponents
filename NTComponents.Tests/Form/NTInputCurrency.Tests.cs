using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace NTComponents.Tests.Form;

public class NTInputCurrency_Tests : BunitContext {

    private sealed class TestModel {
        public decimal? Amount { get; set; }
    }

    [Fact]
    public void Renders_Text_Input_For_Formatted_Currency() {
        var cut = RenderInputCurrency();

        cut.Find("input").GetAttribute("type").Should().Be("text");
    }

    [Fact]
    public void Does_Not_Implement_ITnTComponentBase() {
        var cut = RenderInputCurrency();

        cut.Instance.Should().NotBeAssignableTo<NTComponents.Interfaces.ITnTComponentBase>();
    }

    [Fact]
    public void Adds_Currency_Formatting_Attributes() {
        var cut = RenderInputCurrency(configure: parameters => parameters
            .Add(component => component.CultureCode, "de-DE")
            .Add(component => component.CurrencyCode, "EUR"));
        var input = cut.Find("input");

        input.GetAttribute("cultureCode").Should().Be("de-DE");
        input.GetAttribute("currencyCode").Should().Be("EUR");
        input.GetAttribute("currencyDecimalDigits").Should().Be("2");
        input.GetAttribute("currencyDecimalSeparator").Should().Be(",");
        input.GetAttribute("currencyGroupSeparator").Should().Be(".");
        input.GetAttribute("currencySymbol").Should().Be("€");
        input.GetAttribute("inputmode").Should().Be("decimal");
        input.GetAttribute("oninput").Should().Be("window.NTComponents?.prefixCurrencyInput?.(this)");
        input.HasAttribute("onkeydown").Should().BeFalse();
        input.HasAttribute("onkeyup").Should().BeFalse();
    }

    [Fact]
    public void Formats_Value_As_Currency() {
        var model = new TestModel { Amount = 1234.56m };
        var cut = RenderInputCurrency(model);

        cut.Find("input").GetAttribute("value").Should().Be("$1,234.56");
    }

    [Fact]
    public void Formats_Value_Using_Culture() {
        var model = new TestModel { Amount = 1234.56m };
        var cut = RenderInputCurrency(model, parameters => parameters
            .Add(component => component.CultureCode, "de-DE")
            .Add(component => component.CurrencyCode, "EUR"));

        cut.Find("input").GetAttribute("value").Should().Be(1234.56m.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
    }

    [Fact]
    public void Formats_Value_Using_Currency_Code_Symbol() {
        var model = new TestModel { Amount = 1234.56m };
        var cut = RenderInputCurrency(model, parameters => parameters
            .Add(component => component.CultureCode, "en-US")
            .Add(component => component.CurrencyCode, "EUR"));

        cut.Find("input").GetAttribute("value").Should().Be("€1,234.56");
    }

    [Fact]
    public void Change_Parses_Currency_Value() {
        var model = new TestModel();
        var cut = RenderInputCurrency(model);

        cut.Find("input").Change("$1,234.56");

        model.Amount.Should().Be(1234.56m);
    }

    [Fact]
    public void Input_Preserves_Editing_Text_When_BindOnInput_True() {
        var model = new TestModel();
        var cut = RenderInputCurrency(model, parameters => parameters
            .Add(component => component.BindOnInput, true));
        var input = cut.Find("input");

        input.Input("1");
        cut.Find("input").GetAttribute("value").Should().Be("$1");

        input = cut.Find("input");
        input.Input("$12");
        cut.Find("input").GetAttribute("value").Should().Be("$12");

        input = cut.Find("input");
        input.Input("$12,000");

        model.Amount.Should().Be(12000m);
        cut.Find("input").GetAttribute("value").Should().Be("$12,000");
    }

    [Fact]
    public void Input_Limits_Decimal_Digits_When_BindOnInput_True() {
        var model = new TestModel();
        var cut = RenderInputCurrency(model, parameters => parameters
            .Add(component => component.BindOnInput, true));

        cut.Find("input").Input("$1.234");

        model.Amount.Should().Be(1.23m);
        cut.Find("input").GetAttribute("value").Should().Be("$1.23");
        cut.Find("input[type='hidden']").GetAttribute("value").Should().Be("1.23");
    }

    [Fact]
    public void Blur_Formats_Editing_Text_As_Currency() {
        var model = new TestModel();
        var cut = RenderInputCurrency(model, parameters => parameters
            .Add(component => component.BindOnInput, true));
        var input = cut.Find("input");

        input.Input("$12000333.12");
        cut.Find("input").Blur();

        model.Amount.Should().Be(12000333.12m);
        cut.Find("input").GetAttribute("value").Should().Be("$12,000,333.12");
    }

    [Fact]
    public void Change_Then_Blur_Formats_As_Currency() {
        var model = new TestModel();
        var cut = RenderInputCurrency(model);
        var input = cut.Find("input");

        input.Change("1");
        cut.Find("input").GetAttribute("value").Should().Be("$1");

        cut.Find("input").Blur();

        model.Amount.Should().Be(1m);
        cut.Find("input").GetAttribute("value").Should().Be("$1.00");
    }

    [Fact]
    public void Form_Post_Value_Is_Decimal_String_Not_Formatted_Text() {
        var model = new TestModel { Amount = 1234.56m };
        var cut = RenderInputCurrency(model);
        var visibleInput = cut.Find("input.nt-input-control");
        var formValueInput = cut.Find("input[type='hidden']");

        visibleInput.HasAttribute("name").Should().BeFalse();
        visibleInput.GetAttribute("value").Should().Be("$1,234.56");
        formValueInput.GetAttribute("name").Should().Be("model.Amount");
        formValueInput.GetAttribute("value").Should().Be("1234.56");
    }

    [Fact]
    public void ValueChanged_Emits_Decimal_Value_Not_Formatted_Text() {
        decimal? callbackValue = null;
        var model = new TestModel();
        var cut = Render<NTInputCurrency>(parameters => parameters
            .Add(component => component.Value, model.Amount)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<decimal?>(this, value => {
                callbackValue = value;
                model.Amount = value;
            }))
            .Add(component => component.ValueExpression, () => model.Amount)
            .Add(component => component.BindOnInput, true));

        cut.Find("input").Input("$12,000.34");

        callbackValue.Should().Be(12000.34m);
    }

    [Fact]
    public void BindAfter_Emits_Decimal_Value_Not_Formatted_Text() {
        decimal? bindAfterValue = null;
        var model = new TestModel();
        var cut = RenderInputCurrency(model, parameters => parameters
            .Add(component => component.BindOnInput, true)
            .Add(component => component.BindAfter, EventCallback.Factory.Create<decimal?>(this, value => bindAfterValue = value)));

        cut.Find("input").Input("$12,000.34");

        bindAfterValue.Should().Be(12000.34m);
    }

    [Fact]
    public void Change_Parses_Empty_Value_As_Null() {
        var model = new TestModel { Amount = 1234.56m };
        var cut = RenderInputCurrency(model);

        cut.Find("input").Change(string.Empty);

        model.Amount.Should().BeNull();
    }

    [Fact]
    public void Inherits_FormV2_Input_Chrome() {
        var cut = RenderInputCurrency(configure: parameters => parameters
            .Add(component => component.Label, "Amount")
            .Add(component => component.SupportingText, "Invoice amount"));

        cut.Find(".nt-input").ClassList.Should().Contain("nt-input-outlined");
        cut.Find(".nt-input-label").TextContent.Should().Be("Amount");
        cut.Find(".nt-input-supporting").TextContent.Should().Be("Invoice amount");
    }

    private IRenderedComponent<NTInputCurrency> RenderInputCurrency(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputCurrency>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputCurrency>(parameters => {
            parameters
                .Add(component => component.Value, model.Amount)
                .Add(component => component.ValueChanged, EventCallback.Factory.Create<decimal?>(this, value => model.Amount = value))
                .Add(component => component.ValueExpression, () => model.Amount);

            configure?.Invoke(parameters);
        });
    }
}

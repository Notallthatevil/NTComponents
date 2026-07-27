using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace NTComponents.Tests.Form.TnTInputBase;

public class Constraints_Tests : BunitContext {

    public Constraints_Tests() => SetRendererInfo(new RendererInfo("WebAssembly", true));

    [Fact]
    public void MaxMinAndRequiredAnnotations_RenderInputConstraints() {
        var model = new ConstraintModel();

        var cut = Render<TextConstraintInput>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Code)
            .Add(component => component.Value, model.Code)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Code = value)));

        var input = cut.Find("input");
        input.GetAttribute("maxlength").Should().Be("8");
        input.GetAttribute("minlength").Should().Be("2");
        input.HasAttribute("required").Should().BeTrue();
    }

    [Fact]
    public void StringLengthAnnotation_RendersBothLengthBoundaries() {
        var model = new ConstraintModel();

        var cut = Render<TextConstraintInput>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Description)
            .Add(component => component.Value, model.Description)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Description = value)));

        var input = cut.Find("input");
        input.GetAttribute("maxlength").Should().Be("12");
        input.GetAttribute("minlength").Should().Be("3");
        input.HasAttribute("required").Should().BeFalse();
    }

    [Fact]
    public void RangeAnnotation_RendersNumericBoundaries() {
        var model = new ConstraintModel { Quantity = 5 };

        var cut = Render<NumberConstraintInput>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Quantity)
            .Add(component => component.Value, model.Quantity)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<int>(this, value => model.Quantity = value)));

        var input = cut.Find("input");
        input.GetAttribute("min").Should().Be("1");
        input.GetAttribute("max").Should().Be("10");
    }

    [Fact]
    public void AdditionalAttributes_OverrideAnnotationBoundaries() {
        var model = new ConstraintModel { Quantity = 5 };
        var attributes = new Dictionary<string, object> { ["min"] = -4, ["max"] = 25 };

        var cut = Render<NumberConstraintInput>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Quantity)
            .Add(component => component.Value, model.Quantity)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<int>(this, value => model.Quantity = value))
            .Add(component => component.AdditionalAttributes, attributes));

        var input = cut.Find("input");
        input.GetAttribute("min").Should().Be("-4");
        input.GetAttribute("max").Should().Be("25");
    }

    [Fact]
    public void MalformedLengthAttributes_FallBackToAnnotations() {
        var model = new ConstraintModel();
        var attributes = new Dictionary<string, object> { ["minlength"] = "invalid", ["maxlength"] = "invalid" };

        var cut = Render<TextConstraintInput>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Code)
            .Add(component => component.Value, model.Code)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Code = value))
            .Add(component => component.AdditionalAttributes, attributes));

        var input = cut.Find("input");
        input.GetAttribute("minlength").Should().Be("2");
        input.GetAttribute("maxlength").Should().Be("8");
    }

    private sealed class ConstraintModel {
        [MaxLength(8)]
        [MinLength(2)]
        [Required]
        public string? Code { get; set; }

        [StringLength(12, MinimumLength = 3)]
        public string? Description { get; set; }

        [Range(1, 10)]
        public int Quantity { get; set; }
    }

    private sealed class TextConstraintInput : global::NTComponents.TnTInputBase<string?> {
        public override InputType Type => InputType.Text;

        protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage) {
            result = value;
            validationErrorMessage = string.Empty;
            return true;
        }
    }

    private sealed class NumberConstraintInput : global::NTComponents.TnTInputBase<int> {
        public override InputType Type => InputType.Number;

        protected override bool TryParseValueFromString(string? value, out int result, out string validationErrorMessage) {
            var parsed = int.TryParse(value, out result);
            validationErrorMessage = parsed ? string.Empty : "Not an integer.";
            return parsed;
        }
    }
}

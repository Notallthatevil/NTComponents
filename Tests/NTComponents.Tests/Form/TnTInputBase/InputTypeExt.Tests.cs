namespace NTComponents.Tests.Form.TnTInputBase;

public class InputTypeExt_Tests {

    public static TheoryData<InputType, string> SupportedTypes => new() {
        { InputType.Button, "button" },
        { InputType.Checkbox, "checkbox" },
        { InputType.Color, "color" },
        { InputType.Date, "date" },
        { InputType.DateTime, "datetime-local" },
        { InputType.Email, "email" },
        { InputType.File, "file" },
        { InputType.Hidden, "hidden" },
        { InputType.Image, "image" },
        { InputType.Month, "month" },
        { InputType.Number, "number" },
        { InputType.Password, "password" },
        { InputType.Radio, "radio" },
        { InputType.Range, "range" },
        { InputType.Search, "search" },
        { InputType.Tel, "tel" },
        { InputType.Text, "text" },
        { InputType.Time, "time" },
        { InputType.Url, "url" },
        { InputType.Week, "week" },
        { InputType.Currency, "text" }
    };

    [Theory]
    [MemberData(nameof(SupportedTypes))]
    public void SupportedValue_ReturnsHtmlInputType(InputType inputType, string expected) {
        inputType.ToInputTypeString().Should().Be(expected);
    }

    [Fact]
    public void UndefinedValue_ThrowsExactInvalidValueError() {
        var inputType = (InputType)int.MaxValue;

        var act = () => inputType.ToInputTypeString();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"{int.MaxValue} is not a valid value of InputType");
    }
}

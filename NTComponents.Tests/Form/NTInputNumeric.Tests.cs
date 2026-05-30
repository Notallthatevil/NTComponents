using Microsoft.AspNetCore.Components;
using System.Numerics;

namespace NTComponents.Tests.Form;

public class NTInputNumeric_Tests : BunitContext {

    private sealed class TestModel {
        public int? Count { get; set; }

        public BigInteger? LargeCount { get; set; }
    }

    [Fact]
    public void Renders_Number_Input() {
        var cut = RenderInput();

        cut.Find("input").GetAttribute("type").Should().Be("number");
    }

    [Fact]
    public void Change_Updates_Value() {
        var model = new TestModel();
        var cut = RenderInput(model);

        cut.Find("input").Change("42");

        model.Count.Should().Be(42);
    }

    [Fact]
    public void Change_Updates_BigInteger_Value() {
        var model = new TestModel();
        var cut = RenderBigIntegerInput(model);

        cut.Find("input").Change("123456789012345678901234567890");

        model.LargeCount.Should().Be(BigInteger.Parse("123456789012345678901234567890"));
    }

    [Fact]
    public void Supported_Numeric_Types_Render() {
        RenderSupported<sbyte>();
        RenderSupported<byte>();
        RenderSupported<short>();
        RenderSupported<ushort>();
        RenderSupported<int>();
        RenderSupported<uint>();
        RenderSupported<long>();
        RenderSupported<ulong>();
        RenderSupported<BigInteger>();
        RenderSupported<float>();
        RenderSupported<double>();
        RenderSupported<decimal>();
    }

    [Fact]
    public void Empty_Input_Updates_Nullable_Value_To_Null() {
        var model = new TestModel { Count = 42 };
        var cut = RenderInput(model);

        cut.Find("input").Change(string.Empty);

        model.Count.Should().BeNull();
    }

    private IRenderedComponent<NTInputNumeric<int>> RenderInput(TestModel? model = null) {
        model ??= new TestModel();
        return Render<NTInputNumeric<int>>(parameters => parameters
            .Add(p => p.Value, model.Count)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<int?>(this, value => model.Count = value))
            .Add(p => p.ValueExpression, () => model.Count));
    }

    private IRenderedComponent<NTInputNumeric<BigInteger>> RenderBigIntegerInput(TestModel? model = null) {
        model ??= new TestModel();
        return Render<NTInputNumeric<BigInteger>>(parameters => parameters
            .Add(p => p.Value, model.LargeCount)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<BigInteger?>(this, value => model.LargeCount = value))
            .Add(p => p.ValueExpression, () => model.LargeCount));
    }

    private void RenderSupported<TNumber>() where TNumber : struct, INumber<TNumber> {
        TNumber? value = default(TNumber);
        var cut = Render<NTInputNumeric<TNumber>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        cut.Find("input").GetAttribute("type").Should().Be("number");
    }
}

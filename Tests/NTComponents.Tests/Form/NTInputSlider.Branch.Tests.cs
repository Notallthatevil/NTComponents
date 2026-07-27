using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Form;

public sealed class NTInputSlider_Branch_Tests : BunitContext {
    private sealed class TestModel {
        public int Value { get; set; } = 50;

        public NTSliderRange<int> Range { get; set; } = new(20, 80);
    }

    private sealed class TestableRangeSlider : NTInputRangeSlider<int> {
        public void ParseSingleValue() => TryParseValueFromString("50", out _, out _);
    }

    public static TheoryData<NTSliderSize, string> SliderSizes => new() {
        { NTSliderSize.ExtraSmall, "nt-slider-extra-small" },
        { NTSliderSize.Small, "nt-slider-small" },
        { NTSliderSize.Medium, "nt-slider-medium" },
        { NTSliderSize.Large, "nt-slider-large" },
        { NTSliderSize.ExtraLarge, "nt-slider-extra-large" }
    };

    [Theory]
    [MemberData(nameof(SliderSizes))]
    public void Size_Selects_The_Corresponding_Track_Class_For_Both_Slider_Types(NTSliderSize size, string expectedClass) {
        var slider = RenderSlider(configure: parameters => parameters.Add(p => p.Size, size));
        var range = RenderRangeSlider(configure: parameters => parameters.Add(p => p.Size, size));

        slider.Find(".nt-slider").ClassList.Should().Contain(expectedClass);
        range.Find(".nt-slider").ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void NonNumeric_Input_Does_Not_Change_Slider_Value_Or_Invoke_BindAfter() {
        var model = new TestModel();
        var callbackCount = 0;
        var cut = RenderSlider(model, parameters => parameters.Add(p => p.BindAfter, EventCallback.Factory.Create<int>(this, _ => callbackCount++)));

        cut.Find("input[type=range]").Input("not-a-number");

        model.Value.Should().Be(50);
        callbackCount.Should().Be(0);
        cut.Find("input[type=range]").GetAttribute("value").Should().Be("50");
    }

    [Fact]
    public void NonNumeric_Range_Input_Does_Not_Change_Either_Handle_Or_Invoke_BindAfter() {
        var model = new TestModel();
        var callbackCount = 0;
        var cut = RenderRangeSlider(model, parameters => parameters.Add(p => p.BindAfter, EventCallback.Factory.Create<NTSliderRange<int>>(this, _ => callbackCount++)));

        cut.FindAll("input[type=range]")[0].Input("invalid-start");
        cut.FindAll("input[type=range]")[1].Input("invalid-end");

        model.Range.Start.Should().Be(20);
        model.Range.End.Should().Be(80);
        callbackCount.Should().Be(0);
        cut.FindAll("input[type=range]").Select(input => input.GetAttribute("value")).Should().Equal("20", "80");
    }

    [Fact]
    public void Slider_Input_Clamps_Below_Minimum_And_Invalid_Bounds_To_The_Effective_Minimum() {
        var belowMinimum = new TestModel();
        var belowMinimumCut = RenderSlider(belowMinimum, parameters => parameters
            .Add(p => p.Min, 10)
            .Add(p => p.Max, 90));

        belowMinimumCut.Find("input[type=range]").Input("-5");

        belowMinimum.Value.Should().Be(10);
        belowMinimumCut.Find("input[type=range]").GetAttribute("value").Should().Be("10");

        var invalidBounds = new TestModel { Value = 75 };
        var invalidBoundsCut = RenderSlider(invalidBounds, parameters => parameters
            .Add(p => p.Min, 10)
            .Add(p => p.Max, 5)
            .Add(p => p.ShowValueIndicator, true));

        invalidBoundsCut.Find("input[type=range]").Input("99");

        invalidBounds.Value.Should().Be(10);
        invalidBoundsCut.Find(".nt-slider").GetAttribute("style").Should().Contain("--nt-slider-end-percent:0%;");
        invalidBoundsCut.Find("output").TextContent.Should().Be("10");
    }

    [Fact]
    public void Range_Input_Clamps_To_Bounds_And_Preserves_Handle_Order() {
        var model = new TestModel { Range = new NTSliderRange<int>(30, 70) };
        var callbacks = new List<NTSliderRange<int>>();
        var cut = RenderRangeSlider(model, parameters => parameters
            .Add(p => p.Min, 10)
            .Add(p => p.Max, 90)
            .Add(p => p.BindAfter, EventCallback.Factory.Create<NTSliderRange<int>>(this, value => callbacks.Add(value))));

        cut.FindAll("input[type=range]")[0].Input("-5");
        cut.FindAll("input[type=range]")[1].Input("100");

        model.Range.Start.Should().Be(10);
        model.Range.End.Should().Be(90);
        callbacks.Select(value => value.Start).Should().Equal(10, 10);
        callbacks.Select(value => value.End).Should().Equal(70, 90);
    }

    [Fact]
    public void Invalid_Range_Bounds_Collapse_Both_Handles_To_The_Minimum() {
        var model = new TestModel { Range = new NTSliderRange<int>(0, 100) };
        var cut = RenderRangeSlider(model, parameters => parameters
            .Add(p => p.Min, 10)
            .Add(p => p.Max, 5)
            .Add(p => p.ShowValueIndicator, true));

        cut.FindAll("input[type=range]")[0].Input("50");

        model.Range.Start.Should().Be(10);
        model.Range.End.Should().Be(10);
        cut.Find(".nt-slider").GetAttribute("style").Should().Contain("--nt-slider-start-percent:0%;").And.Contain("--nt-slider-end-percent:0%;");
        cut.FindAll("output").Select(output => output.TextContent).Should().Equal("10", "10");
    }

    [Fact]
    public void Range_BindAfter_Receives_Each_Normalized_Handle_Update() {
        var model = new TestModel();
        var callbacks = new List<NTSliderRange<int>>();
        var cut = RenderRangeSlider(model, parameters => parameters.Add(p => p.BindAfter, EventCallback.Factory.Create<NTSliderRange<int>>(this, value => callbacks.Add(value))));

        cut.FindAll("input[type=range]")[0].Input("30");
        cut.FindAll("input[type=range]")[1].Input("70");

        callbacks.Should().HaveCount(2);
        callbacks.Select(value => value.Start).Should().Equal(30, 30);
        callbacks.Select(value => value.End).Should().Equal(80, 70);
        model.Range.Start.Should().Be(30);
        model.Range.End.Should().Be(70);
    }

    [Fact]
    public void Disabled_Sliders_Ignore_Input_And_Do_Not_Emit_ReadOnly_Form_Values() {
        var model = new TestModel();
        var singleCallbackCount = 0;
        var rangeCallbackCount = 0;
        var slider = RenderSlider(model, parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.BindAfter, EventCallback.Factory.Create<int>(this, _ => singleCallbackCount++)));
        var range = RenderRangeSlider(model, parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.BindAfter, EventCallback.Factory.Create<NTSliderRange<int>>(this, _ => rangeCallbackCount++)));

        slider.Find("input[type=range]").Input("60");
        range.FindAll("input[type=range]")[0].Input("30");
        range.FindAll("input[type=range]")[1].Input("70");

        model.Value.Should().Be(50);
        model.Range.Start.Should().Be(20);
        model.Range.End.Should().Be(80);
        singleCallbackCount.Should().Be(0);
        rangeCallbackCount.Should().Be(0);
        slider.Find(".nt-slider").ClassList.Should().Contain("nt-slider-disabled");
        range.Find(".nt-slider").ClassList.Should().Contain("nt-slider-disabled");
        slider.FindAll("input[type=hidden]").Should().BeEmpty();
        range.FindAll("input[type=hidden]").Should().BeEmpty();
    }

    [Fact]
    public void ReadOnly_Sliders_Ignore_Input_And_Do_Not_Invoke_BindAfter() {
        var model = new TestModel();
        var singleCallbackCount = 0;
        var rangeCallbackCount = 0;
        var slider = RenderSlider(model, parameters => parameters
            .Add(p => p.ReadOnly, true)
            .Add(p => p.BindAfter, EventCallback.Factory.Create<int>(this, _ => singleCallbackCount++)));
        var range = RenderRangeSlider(model, parameters => parameters
            .Add(p => p.ReadOnly, true)
            .Add(p => p.BindAfter, EventCallback.Factory.Create<NTSliderRange<int>>(this, _ => rangeCallbackCount++)));

        slider.Find("input[type=range]").Input("60");
        range.FindAll("input[type=range]")[0].Input("30");
        range.FindAll("input[type=range]")[1].Input("70");

        model.Value.Should().Be(50);
        model.Range.Start.Should().Be(20);
        model.Range.End.Should().Be(80);
        singleCallbackCount.Should().Be(0);
        rangeCallbackCount.Should().Be(0);
    }

    [Fact]
    public void Disabled_ChangeBound_Sliders_Ignore_Change_And_Do_Not_Invoke_BindAfter() {
        var model = new TestModel();
        var singleCallbackCount = 0;
        var rangeCallbackCount = 0;
        var slider = RenderSlider(model, parameters => parameters
            .Add(p => p.BindOnInput, false)
            .Add(p => p.Disabled, true)
            .Add(p => p.BindAfter, EventCallback.Factory.Create<int>(this, _ => singleCallbackCount++)));
        var range = RenderRangeSlider(model, parameters => parameters
            .Add(p => p.BindOnInput, false)
            .Add(p => p.Disabled, true)
            .Add(p => p.BindAfter, EventCallback.Factory.Create<NTSliderRange<int>>(this, _ => rangeCallbackCount++)));

        slider.Find("input[type=range]").Change("60");
        range.FindAll("input[type=range]")[0].Change("30");
        range.FindAll("input[type=range]")[1].Change("70");

        model.Value.Should().Be(50);
        model.Range.Start.Should().Be(20);
        model.Range.End.Should().Be(80);
        singleCallbackCount.Should().Be(0);
        rangeCallbackCount.Should().Be(0);
    }

    [Theory]
    [InlineData("any", 0)]
    [InlineData("invalid", 0)]
    [InlineData("0", 0)]
    [InlineData("25", 5)]
    [InlineData("1", 2)]
    public void Automatic_Stop_Counts_Are_Deterministic_For_Both_Slider_Types(string step, int expectedCount) {
        var slider = RenderSlider(configure: parameters => parameters
            .Add(p => p.ShowStops, true)
            .Add(p => p.Step, step));
        var range = RenderRangeSlider(configure: parameters => parameters
            .Add(p => p.ShowStops, true)
            .Add(p => p.Step, step));

        slider.FindAll(".nt-slider-stop").Should().HaveCount(expectedCount);
        range.FindAll(".nt-slider-stop").Should().HaveCount(expectedCount);
        slider.Find(".nt-slider").ClassList.Should().Contain("nt-slider-with-stops");
        range.Find(".nt-slider").ClassList.Should().Contain("nt-slider-with-stops");
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(60, 50)]
    public void Explicit_Stop_Counts_Respect_Rendering_Boundaries(int requestedCount, int expectedCount) {
        var slider = RenderSlider(configure: parameters => parameters
            .Add(p => p.ShowStops, true)
            .Add(p => p.StopCount, requestedCount));
        var range = RenderRangeSlider(configure: parameters => parameters
            .Add(p => p.ShowStops, true)
            .Add(p => p.StopCount, requestedCount));

        slider.FindAll(".nt-slider-stop").Should().HaveCount(expectedCount);
        range.FindAll(".nt-slider-stop").Should().HaveCount(expectedCount);
    }

    [Fact]
    public void Range_Stops_Identify_Active_And_Inactive_Track_Segments() {
        var cut = RenderRangeSlider(configure: parameters => parameters
            .Add(p => p.ShowStops, true)
            .Add(p => p.StopCount, 5));
        var stops = cut.FindAll(".nt-slider-stop");

        stops.Should().HaveCount(5);
        stops.Count(stop => stop.ClassList.Contains("nt-slider-stop-active")).Should().Be(3);
        stops.Count(stop => stop.ClassList.Contains("nt-slider-stop-inactive")).Should().Be(2);
        stops.Select(stop => stop.GetAttribute("style")).Should().Equal(
            "--nt-slider-stop-percent:0%;",
            "--nt-slider-stop-percent:25%;",
            "--nt-slider-stop-percent:50%;",
            "--nt-slider-stop-percent:75%;",
            "--nt-slider-stop-percent:100%;");
    }

    [Fact]
    public void Empty_Step_Omits_The_Native_Attribute_For_Both_Slider_Types() {
        var slider = RenderSlider(configure: parameters => parameters.Add(p => p.Step, " "));
        var range = RenderRangeSlider(configure: parameters => parameters.Add(p => p.Step, null));

        slider.Find("input[type=range]").HasAttribute("step").Should().BeFalse();
        range.FindAll("input[type=range]").Should().OnlyContain(input => !input.HasAttribute("step"));
    }

    [Fact]
    public void Centered_Value_Above_Midpoint_And_Low_Inset_Value_Emit_Correct_Track_Styles() {
        var centeredModel = new TestModel { Value = 25 };
        var centered = RenderSlider(centeredModel, parameters => parameters
            .Add(p => p.Min, -100)
            .Add(p => p.Max, 100)
            .Add(p => p.Variant, NTSliderVariant.Centered));
        var insetModel = new TestModel { Value = 10 };
        var inset = RenderSlider(insetModel, parameters => parameters
            .Add(p => p.InsetIcon, MaterialIcon.VolumeDown)
            .Add(p => p.Size, NTSliderSize.Medium));

        centered.Find(".nt-slider").GetAttribute("style").Should()
            .Contain("--nt-slider-start-percent:50%;")
            .And.Contain("--nt-slider-end-percent:62.5%;")
            .And.Contain("--nt-slider-start-gap:0px;")
            .And.Contain("--nt-slider-end-gap:8px;");
        inset.Find(".nt-slider-inset-icon").ClassList.Should().Contain("nt-slider-inset-icon-inactive");
        inset.Find(".nt-slider").GetAttribute("style").Should().Contain("--nt-slider-inset-icon-position:calc(var(--nt-slider-end-percent) + 20px);");
    }

    [Fact]
    public void Every_Color_Override_Is_Emitted_For_Both_Slider_Types() {
        Action<ComponentParameterCollectionBuilder<NTInputSlider<int>>> configureSlider = parameters => parameters
            .Add(p => p.ActiveTrackColor, TnTColor.Primary)
            .Add(p => p.DisabledColor, TnTColor.OnSurface)
            .Add(p => p.ErrorColor, TnTColor.Error)
            .Add(p => p.FocusColor, TnTColor.Secondary)
            .Add(p => p.HandleColor, TnTColor.Tertiary)
            .Add(p => p.InactiveTrackColor, TnTColor.SurfaceContainer)
            .Add(p => p.LabelColor, TnTColor.OnSurfaceVariant)
            .Add(p => p.StateLayerColor, TnTColor.PrimaryContainer)
            .Add(p => p.SupportingTextColor, TnTColor.Outline);
        Action<ComponentParameterCollectionBuilder<NTInputRangeSlider<int>>> configureRange = parameters => parameters
            .Add(p => p.ActiveTrackColor, TnTColor.Primary)
            .Add(p => p.DisabledColor, TnTColor.OnSurface)
            .Add(p => p.ErrorColor, TnTColor.Error)
            .Add(p => p.FocusColor, TnTColor.Secondary)
            .Add(p => p.HandleColor, TnTColor.Tertiary)
            .Add(p => p.InactiveTrackColor, TnTColor.SurfaceContainer)
            .Add(p => p.LabelColor, TnTColor.OnSurfaceVariant)
            .Add(p => p.StateLayerColor, TnTColor.PrimaryContainer)
            .Add(p => p.SupportingTextColor, TnTColor.Outline);

        var sliderStyle = RenderSlider(configure: configureSlider).Find(".nt-slider").GetAttribute("style");
        var rangeStyle = RenderRangeSlider(configure: configureRange).Find(".nt-slider").GetAttribute("style");
        var expectedVariables = new[] {
            "--nt-slider-active-track-color:var(--tnt-color-primary);",
            "--nt-slider-disabled-color:var(--tnt-color-on-surface);",
            "--nt-slider-error-color:var(--tnt-color-error);",
            "--nt-slider-focus-color:var(--tnt-color-secondary);",
            "--nt-slider-handle-color:var(--tnt-color-tertiary);",
            "--nt-slider-inactive-track-color:var(--tnt-color-surface-container);",
            "--nt-slider-label-color:var(--tnt-color-on-surface-variant);",
            "--nt-slider-state-layer-color:var(--tnt-color-primary-container);",
            "--nt-slider-supporting-text-color:var(--tnt-color-outline);"
        };

        foreach (var expectedVariable in expectedVariables) {
            sliderStyle.Should().Contain(expectedVariable);
            rangeStyle.Should().Contain(expectedVariable);
        }
    }

    [Fact]
    public void Range_Handle_Labels_Use_Custom_Suffixes_With_And_Without_A_Group_Label() {
        var unlabeled = RenderRangeSlider(configure: parameters => parameters
            .Add(p => p.StartHandleLabel, "Lower bound")
            .Add(p => p.EndHandleLabel, "Upper bound"));
        var labeled = RenderRangeSlider(configure: parameters => parameters
            .Add(p => p.Label, "Price")
            .Add(p => p.StartHandleLabel, "From")
            .Add(p => p.EndHandleLabel, "To"));

        unlabeled.FindAll("input[type=range]").Select(input => input.GetAttribute("aria-label")).Should().Equal("Lower bound", "Upper bound");
        labeled.FindAll("input[type=range]").Select(input => input.GetAttribute("aria-label")).Should().Equal("Price From", "Price To");
    }

    [Fact]
    public void Range_Slider_Rejects_The_Single_String_Parsing_Path() {
        var component = new TestableRangeSlider();

        component.Invoking(static slider => slider.ParseSingleValue())
            .Should().Throw<NotSupportedException>()
            .WithMessage("TestableRangeSlider uses two native range inputs instead of parsing a single string value.");
    }

    private IRenderedComponent<NTInputSlider<int>> RenderSlider(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputSlider<int>>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputSlider<int>>(parameters => {
            parameters.Add(p => p.Value, model.Value);
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, value => model.Value = value));
            parameters.Add(p => p.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTInputRangeSlider<int>> RenderRangeSlider(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputRangeSlider<int>>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputRangeSlider<int>>(parameters => {
            parameters.Add(p => p.Value, model.Range);
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<NTSliderRange<int>>(this, value => model.Range = value));
            parameters.Add(p => p.ValueExpression, () => model.Range);
            configure?.Invoke(parameters);
        });
    }
}

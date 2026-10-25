namespace NTComponents.Analyzers.Tests;

public sealed class NTInputRules_Tests {

    private static async Task<string[]> IdsAsync(string statements) => (await AnalyzerHarness.AnalyzeBuildAsync(statements)).Ids();

    [Theory]
    [InlineData("", "NTC1059")]
    [InlineData("builder.AddComponentParameter(2, \"InputType\", TextInputType.Email);", "NTC1059")]
    [InlineData("builder.AddComponentParameter(2, \"InputType\", TextInputType.Tel);", "")]
    [InlineData("builder.AddComponentParameter(2, \"InputType\", TextInputType.Tel); builder.AddComponentParameter(3, \"PhoneMask\", \"---\");", "NTC1060")]
    public async Task PhoneMask_Rules(string extra, string expected) {
        var hasMask = !extra.Contains("PhoneMask");
        var ids = await IdsAsync($$"""
            builder.OpenComponent<NTInputText>(0);
            builder.AddComponentParameter(1, "Label", "Phone");
            {{(hasMask ? "builder.AddComponentParameter(4, \"PhoneMask\", \"(###)\");" : "")}}
            {{extra}}
            builder.CloseComponent();
            """);

        Assert.Equal(expected.Length == 0 ? [] : [expected], ids);
    }

    [Fact]
    public async Task PhoneMask_Unknown_InputType_Or_Mask_Is_Not_Reported() {
        var ids = await IdsAsync("""
            var type = DateTime.Now.Second > 30 ? TextInputType.Tel : TextInputType.Text;
            var mask = DateTime.Now.ToString();
            builder.OpenComponent<NTInputText>(0);
            builder.AddComponentParameter(1, "Label", "Phone");
            builder.AddComponentParameter(2, "InputType", type);
            builder.AddComponentParameter(3, "PhoneMask", mask);
            builder.CloseComponent();
            builder.OpenComponent<NTInputText>(4);
            builder.AddComponentParameter(5, "Label", "Phone");
            builder.AddComponentParameter(6, "InputType", TextInputType.Tel);
            builder.AddComponentParameter(7, "PhoneMask", mask);
            builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task Reports_Raw_Attributes_Each_Component_Drops() {
        var ids = await IdsAsync("""
            builder.OpenComponent<NTInputText>(0);
            builder.AddComponentParameter(1, "Label", "A");
            builder.AddComponentParameter(2, "id", "x");
            builder.AddComponentParameter(3, "type", "text");
            builder.CloseComponent();
            builder.OpenComponent<NTInputSlider<int>>(4);
            builder.AddComponentParameter(5, "Label", "A");
            builder.AddComponentParameter(6, "aria-invalid", "true");
            builder.CloseComponent();
            builder.OpenComponent<NTCombobox<int>>(7);
            builder.AddComponentParameter(8, "Label", "A");
            builder.AddComponentParameter(9, "role", "combobox");
            builder.AddComponentParameter(10, "aria-expanded", "true");
            builder.CloseComponent();
            builder.OpenComponent<NTInputRadioGroup<int>>(11);
            builder.AddComponentParameter(12, "Label", "A");
            builder.AddComponentParameter(13, "tabindex", "1");
            builder.CloseComponent();
            builder.OpenComponent<NTColorPicker>(14);
            builder.AddComponentParameter(15, "role", "group");
            builder.CloseComponent();
            builder.OpenComponent<NTFormSectionView>(16);
            builder.AddComponentParameter(17, "lang", "en");
            builder.CloseComponent();
            builder.OpenComponent<NTInputCheckbox>(18);
            builder.AddComponentParameter(19, "Label", "A");
            builder.AddComponentParameter(20, "aria-required", "true");
            builder.CloseComponent();
            builder.OpenComponent<NTInputRadio<int>>(21);
            builder.AddComponentParameter(22, "Label", "A");
            builder.AddComponentParameter(23, "name", "n");
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1061", "NTC1061", "NTC1061", "NTC1061", "NTC1061", "NTC1061", "NTC1061", "NTC1061", "NTC1061", "NTC1061"], ids);
    }

    [Fact]
    public async Task DoesNotReport_Attributes_The_Component_Reads_Or_Passes_Through() {
        var ids = await IdsAsync("""
            builder.OpenComponent<NTInputText>(0);
            builder.AddComponentParameter(1, "Label", "A");
            builder.AddComponentParameter(2, "required", true);
            builder.AddComponentParameter(3, "name", "n");
            builder.AddComponentParameter(4, "min", "1");
            builder.AddComponentParameter(5, "maxlength", "5");
            builder.CloseComponent();
            builder.OpenComponent<NTInputCheckbox>(6);
            builder.AddComponentParameter(7, "Label", "A");
            builder.AddComponentParameter(8, "title", "t");
            builder.AddComponentParameter(9, "aria-describedby", "d");
            builder.CloseComponent();
            builder.OpenComponent<NTInputSlider<int>>(10);
            builder.AddComponentParameter(11, "Label", "A");
            builder.AddComponentParameter(12, "aria-describedby", "d");
            builder.AddComponentParameter(13, "Min", 1);
            builder.AddComponentParameter(14, "Max", 5);
            builder.AddComponentParameter(15, "Step", "1");
            builder.CloseComponent();
            builder.OpenComponent<NTFileUpload>(16);
            builder.AddComponentParameter(17, "Label", "A");
            builder.AddComponentParameter(18, "multiple", true);
            builder.CloseComponent();
            builder.OpenComponent<NTInputText>(19);
            builder.AddComponentParameter(20, "Label", "A");
            builder.AddComponentParameter(21, "Value", "v");
            builder.AddComponentParameter(22, "ValueExpression", (System.Linq.Expressions.Expression<Func<string?>>)(() => ""));
            builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("builder.AddComponentParameter(2, \"Value\", \"v\");", true)]
    [InlineData("builder.AddComponentParameter(2, \"ValueChanged\", default(EventCallback<string?>));", true)]
    [InlineData("builder.AddComponentParameter(2, \"Value\", \"v\"); builder.AddMultipleAttributes(3, new System.Collections.Generic.Dictionary<string, object>());", false)]
    [InlineData("", false)]
    public async Task ValueExpression(string parameters, bool reported) {
        var ids = await IdsAsync($$"""
            builder.OpenComponent<NTInputText>(0);
            builder.AddComponentParameter(1, "Label", "A");
            {{parameters}}
            builder.CloseComponent();
            """);

        Assert.Equal(reported ? ["NTC1062"] : [], ids);
    }

    [Fact]
    public async Task Boolean_Inputs_Require_The_Required_Parameter() {
        var ids = await IdsAsync("""
            builder.OpenComponent<NTInputCheckbox>(0);
            builder.AddComponentParameter(1, "Label", "A");
            builder.AddComponentParameter(2, "required", true);
            builder.CloseComponent();
            builder.OpenComponent<NTInputSwitch>(3);
            builder.AddComponentParameter(4, "Label", "A");
            builder.AddComponentParameter(5, "required", true);
            builder.CloseComponent();
            builder.OpenComponent<NTInputSwitch>(6);
            builder.AddComponentParameter(7, "Label", "A");
            builder.AddComponentParameter(8, "Required", true);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1063", "NTC1063"], ids);
    }

    [Theory]
    [InlineData("true", "\"\"", true)]
    [InlineData("true", "\"  \"", true)]
    [InlineData("true", "\"Required\"", false)]
    [InlineData("false", "\"\"", false)]
    public async Task Form_Required_Supporting_Text(string show, string text, bool reported) {
        var ids = await IdsAsync($$"""
            builder.OpenComponent<NTForm>(0);
            builder.AddComponentParameter(1, "ShowRequiredSupportingText", {{show}});
            builder.AddComponentParameter(2, "RequiredSupportingText", {{text}});
            builder.CloseComponent();
            """);

        Assert.Equal(reported ? ["NTC1064"] : [], ids);
    }

    [Fact]
    public async Task Form_Required_Supporting_Text_Unknown_Is_Not_Reported() {
        var ids = await IdsAsync("""
            var text = DateTime.Now.ToString();
            builder.OpenComponent<NTForm>(0);
            builder.AddComponentParameter(1, "ShowRequiredSupportingText", true);
            builder.AddComponentParameter(2, "RequiredSupportingText", text);
            builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("builder.AddComponentParameter(2, \"MinVisibleLines\", 0);", true)]
    [InlineData("builder.AddComponentParameter(2, \"MaxVisibleLines\", (int?)0);", true)]
    [InlineData("builder.AddComponentParameter(2, \"MaxVisibleLines\", (int?)1);", true)]
    [InlineData("builder.AddComponentParameter(2, \"MinVisibleLines\", 8);", true)]
    [InlineData("builder.AddComponentParameter(2, \"MinVisibleLines\", 8); builder.AddComponentParameter(3, \"MaxVisibleLines\", (int?)null);", false)]
    [InlineData("builder.AddComponentParameter(2, \"MinVisibleLines\", 3); builder.AddComponentParameter(3, \"MaxVisibleLines\", (int?)3);", false)]
    [InlineData("", false)]
    public async Task TextArea_Visible_Lines(string parameters, bool reported) {
        var ids = await IdsAsync($$"""
            builder.OpenComponent<NTTextArea>(0);
            builder.AddComponentParameter(1, "Label", "A");
            {{parameters}}
            builder.CloseComponent();
            """);

        Assert.Equal(reported ? ["NTC1104"] : [], ids);
    }

    [Fact]
    public async Task TextArea_Unknown_Lines_Are_Not_Reported() {
        var ids = await IdsAsync("""
            var lines = DateTime.Now.Second;
            builder.OpenComponent<NTTextArea>(0);
            builder.AddComponentParameter(1, "Label", "A");
            builder.AddComponentParameter(2, "MinVisibleLines", lines);
            builder.AddComponentParameter(3, "MaxVisibleLines", (int?)1);
            builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task Select_Multiple_And_Unsupported_Types() {
        var ids = await IdsAsync("""
            builder.OpenComponent<NTSelect<string>>(0);
            builder.AddComponentParameter(1, "Label", "A");
            builder.AddComponentParameter(2, "multiple", true);
            builder.CloseComponent();
            builder.OpenComponent<NTSelect<object>>(3);
            builder.AddComponentParameter(4, "Label", "A");
            builder.CloseComponent();
            builder.OpenComponent<NTSelect<int[]>>(5);
            builder.AddComponentParameter(6, "Label", "A");
            builder.CloseComponent();
            builder.OpenComponent<NTSelect<string>>(7);
            builder.AddComponentParameter(8, "Label", "A");
            builder.CloseComponent();
            builder.OpenComponent<NTSelect<int?>>(9);
            builder.AddComponentParameter(10, "Label", "A");
            builder.CloseComponent();
            builder.OpenComponent<NTSelect<DayOfWeek>>(11);
            builder.AddComponentParameter(12, "Label", "A");
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1105", "NTC1106", "NTC1106"], ids);
    }

    [Fact]
    public async Task DateTime_Type_Arguments() {
        var ids = await IdsAsync("""
            builder.OpenComponent<NTInputDateTime<string>>(0);
            builder.AddComponentParameter(1, "Label", "A");
            builder.CloseComponent();
            builder.OpenComponent<NTInputDateTime<int?>>(2);
            builder.AddComponentParameter(3, "Label", "A");
            builder.CloseComponent();
            builder.OpenComponent<NTInputDateTime<DateTime>>(4);
            builder.AddComponentParameter(5, "Label", "A");
            builder.CloseComponent();
            builder.OpenComponent<NTInputDateTime<DateTimeOffset?>>(6);
            builder.AddComponentParameter(7, "Label", "A");
            builder.CloseComponent();
            builder.OpenComponent<NTInputDateTime<DateOnly>>(8);
            builder.AddComponentParameter(9, "Label", "A");
            builder.CloseComponent();
            builder.OpenComponent<NTInputDateTime<TimeOnly?>>(10);
            builder.AddComponentParameter(11, "Label", "A");
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1106", "NTC1106"], ids);
    }

    [Fact]
    public async Task Generic_Type_Parameters_Are_Not_Reported() {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync("""
            using Microsoft.AspNetCore.Components.Rendering;
            using NTComponents;

            public static class Fixture {
                public static void Build<T>(RenderTreeBuilder builder) {
                    builder.OpenComponent<NTSelect<T>>(0);
                    builder.AddComponentParameter(1, "Label", "A");
                    builder.CloseComponent();
                    builder.OpenComponent<NTInputDateTime<T>>(2);
                    builder.AddComponentParameter(3, "Label", "A");
                    builder.CloseComponent();
                }
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Accessible_Name_Rules() {
        var ids = await IdsAsync("""
            builder.OpenComponent<NTInputText>(0);
            builder.CloseComponent();
            builder.OpenComponent<NTSelect<string>>(1);
            builder.AddComponentParameter(2, "Label", "");
            builder.CloseComponent();
            builder.OpenComponent<NTInputCheckbox>(3);
            builder.AddComponentParameter(4, "aria-label", "");
            builder.CloseComponent();
            builder.OpenComponent<NTInputSlider<int>>(5);
            builder.CloseComponent();
            builder.OpenComponent<NTColorPicker>(6);
            builder.AddComponentParameter(7, "AriaLabel", "");
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1107", "NTC1107", "NTC1107", "NTC1107", "NTC1107"], ids);
    }

    [Fact]
    public async Task Named_Or_Unknown_Fields_Are_Not_Reported() {
        var ids = await IdsAsync("""
            var label = DateTime.Now.ToString();
            builder.OpenComponent<NTInputText>(0);
            builder.AddComponentParameter(1, "Label", label);
            builder.CloseComponent();
            builder.OpenComponent<NTInputText>(2);
            builder.AddComponentParameter(3, "aria-label", "Name");
            builder.CloseComponent();
            builder.OpenComponent<NTInputCheckbox>(4);
            builder.AddComponentParameter(5, "aria-labelledby", "heading");
            builder.CloseComponent();
            builder.OpenComponent<NTInputRangeSlider<int>>(6);
            builder.CloseComponent();
            builder.OpenComponent<NTInputText>(7);
            builder.AddMultipleAttributes(8, new System.Collections.Generic.Dictionary<string, object>());
            builder.CloseComponent();
            builder.OpenComponent<NTColorPicker>(9);
            builder.CloseComponent();
            builder.OpenComponent<NTRichTextEditor>(10);
            builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("true", "\"\"", true)]
    [InlineData("true", "null", true)]
    [InlineData("true", "\"Address\"", false)]
    [InlineData("false", "\"\"", false)]
    public async Task Fieldset_Needs_A_Heading(string useFieldset, string heading, bool reported) {
        var ids = await IdsAsync($$"""
            builder.OpenComponent<NTFormSectionView>(0);
            builder.AddComponentParameter(1, "UseFieldset", {{useFieldset}});
            builder.AddComponentParameter(2, "Heading", (string?){{heading}});
            builder.CloseComponent();
            """);

        Assert.Equal(reported ? ["NTC1108"] : [], ids);
    }

    [Fact]
    public async Task Ignored_And_Adjusted_Parameters() {
        var ids = await IdsAsync("""
            builder.OpenComponent<NTTextArea>(0);
            builder.AddComponentParameter(1, "Label", "A");
            builder.AddComponentParameter(2, "AutoGrow", true);
            builder.AddComponentParameter(3, "SizeByContent", true);
            builder.CloseComponent();
            builder.OpenComponent<NTInputDateTime<DateTime>>(4);
            builder.AddComponentParameter(5, "Label", "A");
            builder.AddComponentParameter(6, "MonthOnly", true);
            builder.CloseComponent();
            builder.OpenComponent<NTInputSlider<int>>(7);
            builder.AddComponentParameter(8, "Label", "A");
            builder.AddComponentParameter(9, "StopCount", (int?)4);
            builder.CloseComponent();
            builder.OpenComponent<NTInputRangeSlider<int>>(10);
            builder.AddComponentParameter(11, "ShowStops", true);
            builder.AddComponentParameter(12, "StopCount", (int?)60);
            builder.CloseComponent();
            builder.OpenComponent<NTInputSlider<int>>(13);
            builder.AddComponentParameter(14, "Label", "A");
            builder.AddComponentParameter(15, "ShowStops", true);
            builder.AddComponentParameter(16, "StopCount", (int?)1);
            builder.CloseComponent();
            builder.OpenComponent<NTFormFieldGridView>(17);
            builder.AddComponentParameter(18, "MaxColumns", 5);
            builder.CloseComponent();
            builder.OpenComponent<NTFormSectionView>(19);
            builder.AddComponentParameter(20, "MaxColumns", 9);
            builder.CloseComponent();
            builder.OpenComponent<NTFormFieldGridView>(21);
            builder.AddComponentParameter(22, "MaxColumns", 0);
            builder.CloseComponent();
            builder.OpenComponent<NTFormFieldLayoutSpan>(23);
            builder.AddComponentParameter(24, "SmallColumns", (int?)0);
            builder.AddComponentParameter(25, "LargeColumns", (int?)13);
            builder.CloseComponent();
            builder.OpenComponent<NTRichTextEditor>(26);
            builder.AddComponentParameter(27, "Label", "A");
            builder.AddComponentParameter(28, "ErrorText", "a");
            builder.AddComponentParameter(29, "ErrorMessage", "b");
            builder.AddComponentParameter(30, "LeadingIcon", MaterialIcon.Home);
            builder.AddComponentParameter(31, "StartIcon", MaterialIcon.Search);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1102", "NTC1102", "NTC1102", "NTC1103", "NTC1103", "NTC1103", "NTC1103", "NTC1103", "NTC1103", "NTC1103", "NTC1102", "NTC1102"], ids);
    }

    [Fact]
    public async Task Valid_Or_Unknown_Adjustments_Are_Not_Reported() {
        var ids = await IdsAsync("""
            var n = DateTime.Now.Second;
            builder.OpenComponent<NTTextArea>(0);
            builder.AddComponentParameter(1, "Label", "A");
            builder.AddComponentParameter(2, "SizeByContent", true);
            builder.CloseComponent();
            builder.OpenComponent<NTInputDateTime<DateOnly>>(3);
            builder.AddComponentParameter(4, "Label", "A");
            builder.AddComponentParameter(5, "MonthOnly", true);
            builder.CloseComponent();
            builder.OpenComponent<NTInputSlider<int>>(6);
            builder.AddComponentParameter(7, "Label", "A");
            builder.AddComponentParameter(8, "ShowStops", true);
            builder.AddComponentParameter(9, "StopCount", (int?)5);
            builder.CloseComponent();
            builder.OpenComponent<NTFormFieldGridView>(10);
            builder.AddComponentParameter(11, "MaxColumns", 4);
            builder.CloseComponent();
            builder.OpenComponent<NTFormFieldGridView>(12);
            builder.AddComponentParameter(13, "MaxColumns", n);
            builder.CloseComponent();
            builder.OpenComponent<NTFormFieldLayoutSpan>(14);
            builder.AddComponentParameter(15, "SmallColumns", (int?)12);
            builder.AddComponentParameter(16, "LargeColumns", (int?)n);
            builder.CloseComponent();
            builder.OpenComponent<NTRichTextEditor>(17);
            builder.AddComponentParameter(18, "Label", "A");
            builder.AddComponentParameter(19, "ErrorText", (string?)null);
            builder.AddComponentParameter(20, "ErrorMessage", "b");
            builder.AddComponentParameter(21, "LeadingIcon", (NTIcon?)null);
            builder.AddComponentParameter(22, "StartIcon", MaterialIcon.Search);
            builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }
}

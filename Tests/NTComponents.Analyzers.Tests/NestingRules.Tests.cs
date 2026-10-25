namespace NTComponents.Analyzers.Tests;

public sealed class NestingRules_Tests {

    /// <summary>Wraps render statements in a routable page, where the whole component tree is visible.</summary>
    private static string Page(string statements, string route = "[Route(\"/page\")]") => $$"""
        using Microsoft.AspNetCore.Components;
        using Microsoft.AspNetCore.Components.Rendering;
        using NTComponents;

        {{route}}
        public sealed class Page : ComponentBase {
            protected override void BuildRenderTree(RenderTreeBuilder builder) {
        {{statements}}
            }
        }

        public sealed class Wrapper : ComponentBase {
            [Parameter] public RenderFragment? ChildContent { get; set; }
        }
        """;

    [Fact]
    public async Task Reports_A_Tab_With_No_TabView_In_A_Page() {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Page("""
            builder.OpenComponent<NTTab>(0);
            builder.AddComponentParameter(1, "Label", "Orphan");
            builder.CloseComponent();
            """));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("NTC1087", diagnostic.Id);
        Assert.Equal("NTTab must be rendered inside NTTabView", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DoesNotReport_A_Tab_Inside_A_TabView() {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Page("""
            builder.OpenComponent<NTTabView>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(tabs => {
                tabs.OpenComponent<NTTab>(2);
                tabs.AddComponentParameter(3, "Label", "Inside");
                tabs.CloseComponent();
            }));
            builder.CloseComponent();
            """));

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Route(\"/page\")]", true)]
    public async Task DoesNotReport_When_A_Parent_Could_Exist_Outside_The_Visible_Tree(string route, bool wrapInUnknownComponent = false) {
        var tab = """
            {0}.OpenComponent<NTTab>(10);
            {0}.AddComponentParameter(11, "Label", "Tab");
            {0}.CloseComponent();
            """;
        var statements = wrapInUnknownComponent
            ? "builder.OpenComponent<Wrapper>(0);\nbuilder.AddAttribute(1, \"ChildContent\", (RenderFragment)(inner => {\n" + string.Format(tab, "inner") + "\n}));\nbuilder.CloseComponent();"
            : string.Format(tab, "builder");

        Assert.Empty(await AnalyzerHarness.AnalyzeAsync(Page(statements, route)));
    }

    [Fact]
    public async Task Generic_Children_Need_A_Parent_With_The_Same_Type_Argument() {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Page("""
            builder.OpenComponent<NTInputRadioGroup<string>>(0);
            builder.AddComponentParameter(1, "Label", "Choice");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(radios => {
                radios.OpenComponent<NTInputRadio<int>>(3);
                radios.AddComponentParameter(4, "Label", "One");
                radios.CloseComponent();
                radios.OpenComponent<NTInputRadio<string>>(5);
                radios.AddComponentParameter(6, "Label", "Two");
                radios.CloseComponent();
            }));
            builder.CloseComponent();
            """));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("NTInputRadio must be rendered inside an NTInputRadioGroup with the same TValue", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Menu_Items_Accept_Every_Menu_Host() {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Page("""
            builder.OpenComponent<NTSplitButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(items => {
                items.OpenComponent<NTMenuButtonItem>(3);
                items.AddComponentParameter(4, "Label", "Save as");
                items.CloseComponent();
            }));
            builder.CloseComponent();
            builder.OpenComponent<NTButton>(5);
            builder.AddComponentParameter(6, "Label", "Not a menu");
            builder.AddAttribute(7, "Tooltip", (RenderFragment)(tooltip => {
                tooltip.OpenComponent<NTMenuDividerItem>(8);
                tooltip.CloseComponent();
            }));
            builder.CloseComponent();
            """));

        var diagnostic = Assert.Single(diagnostics);
        Assert.StartsWith("NTMenuDividerItem must be rendered inside NTMenu", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Derived_Wizard_Steps_Need_A_Wizard() {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Page("""
            builder.OpenComponent<NTWizardStep>(0);
            builder.AddComponentParameter(1, "Title", "Details");
            builder.CloseComponent();
            """));

        Assert.Equal(["NTC1087"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_A_Form_Nested_In_A_Form_Or_Wizard_Form_Step_Anywhere() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var model = new object();
            builder.OpenComponent<NTForm>(0);
            builder.AddComponentParameter(1, "Model", model);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<Microsoft.AspNetCore.Components.Forms.EditContext>)(context => inner => {
                inner.OpenComponent<NTForm>(3);
                inner.AddComponentParameter(4, "Model", model);
                inner.CloseComponent();
            }));
            builder.CloseComponent();
            builder.OpenComponent<NTForm>(5);
            builder.AddComponentParameter(6, "Model", model);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1088"], diagnostics.Ids());
    }
}

namespace NTComponents.Analyzers.Tests;

public sealed class RenderTreeWalker_Tests {
    private const string Usings = """
        using System;
        using System.Collections.Generic;
        using Microsoft.AspNetCore.Components;
        using Microsoft.AspNetCore.Components.Rendering;
        using NTComponents;
        """;

    [Fact]
    public async Task Child_Content_Lambdas_Link_Children_To_Their_Parent() {
        var frames = await AnalyzerHarness.CaptureFramesAsync(Usings + """
            public static class Fixture {
                public static void Build(RenderTreeBuilder builder) {
                    builder.OpenComponent<NTMenu>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(inner => {
                        inner.OpenElement(2, "div");
                        inner.AddAttribute(3, "class", "group");
                        inner.OpenComponent<NTMenuButtonItem>(4);
                        inner.AddComponentParameter(5, "Label", "Open");
                        inner.CloseComponent();
                        inner.CloseElement();
                    }));
                    builder.CloseComponent();
                }
            }
            """);

        var item = Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTMenuButtonItem");
        var menu = Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTMenu");
        Assert.Same(menu, item.Parent);
        Assert.Equal([item], menu.Children);
        Assert.False(item.IsConditional);
        Assert.Equal(["ChildContent"], menu.Attributes.Keys);
        Assert.Equal(["Label"], item.Attributes.Keys);
    }

    [Fact]
    public async Task Loops_And_Conditions_Mark_Children_Conditional() {
        var frames = await AnalyzerHarness.CaptureFramesAsync(Usings + """
            public static class Fixture {
                public static void Build(RenderTreeBuilder builder, string[] labels, bool show) {
                    builder.OpenComponent<NTMenu>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(inner => {
                        foreach (var label in labels) {
                            inner.OpenComponent<NTMenuButtonItem>(2);
                            inner.CloseComponent();
                        }

                        inner.OpenComponent<NTMenuDividerItem>(3);
                        inner.CloseComponent();
                    }));
                    builder.CloseComponent();

                    if (show) {
                        builder.OpenComponent<NTButton>(4);
                        builder.AddAttribute(5, "ChildContent", (RenderFragment)(inner => { }));
                        builder.OpenComponent<NTIconButton>(6);
                        builder.CloseComponent();
                        builder.CloseComponent();
                    }
                }
            }
            """);

        Assert.True(Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTMenuButtonItem").IsConditional);
        Assert.False(Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTMenuDividerItem").IsConditional);
        Assert.True(Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTButton").IsConditional);
        Assert.False(Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTIconButton").IsConditional);
    }

    [Fact]
    public async Task Fragments_The_Walker_Cannot_See_Mark_The_Owner_Opaque() {
        var frames = await AnalyzerHarness.CaptureFramesAsync(Usings + """
            public static class Fixture {
                public static void Build(RenderTreeBuilder builder, RenderFragment items) {
                    builder.OpenComponent<NTMenu>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(inner => inner.AddContent(2, items)));
                    builder.CloseComponent();
                    builder.OpenComponent<NTFabMenu>(3);
                    builder.AddAttribute(4, "ChildContent", items);
                    builder.CloseComponent();
                    builder.OpenComponent<NTSplitButton>(5);
                    builder.AddAttribute(6, "ChildContent", (RenderFragment)(inner => inner.AddContent(7, "text")));
                    builder.CloseComponent();
                }
            }
            """);

        Assert.True(Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTMenu").RendersOpaqueContent);
        Assert.True(Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTFabMenu").RendersOpaqueContent);
        Assert.False(Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTSplitButton").RendersOpaqueContent);
    }

    [Fact]
    public async Task Template_Fragments_Keep_Their_Parent() {
        var frames = await AnalyzerHarness.CaptureFramesAsync(Usings + """
            public static class Fixture {
                public static void Build(RenderTreeBuilder builder) {
                    builder.OpenComponent<NTDataGrid<string>>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(columns => {
                        columns.OpenComponent<NTTemplateColumn<string>>(2);
                        columns.AddAttribute(3, "ChildContent", (RenderFragment<string>)(context => cell => {
                            cell.OpenComponent<NTButton>(4);
                            cell.CloseComponent();
                        }));
                        columns.CloseComponent();
                    }));
                    builder.CloseComponent();
                }
            }
            """);

        var button = Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTButton");
        Assert.Equal(["NTComponents.NTTemplateColumn`1", "NTComponents.NTDataGrid`1"], button.Ancestors().Select(static frame => frame.MetadataName));
    }

    [Fact]
    public async Task Walks_Every_Member_And_Unclaimed_Lambda_As_A_Root() {
        var frames = await AnalyzerHarness.CaptureFramesAsync(Usings + """
            public sealed class Fixture {
                private readonly RenderFragment _field = builder => { builder.OpenComponent<NTButton>(0); builder.CloseComponent(); };

                public Fixture(RenderTreeBuilder builder) {
                    builder.OpenComponent<NTButton>(0);
                    builder.CloseComponent();
                }

                private RenderFragment Property => builder => { builder.OpenComponent<NTButton>(0); builder.CloseComponent(); };

                public static void Build(RenderTreeBuilder builder) {
                    void Local(RenderTreeBuilder local) {
                        local.OpenComponent<NTButton>(0);
                        local.CloseComponent();
                    }

                    Action<RenderTreeBuilder> lambda = inner => { inner.OpenComponent<NTButton>(0); inner.CloseComponent(); };
                    Action<RenderTreeBuilder> anonymous = delegate (RenderTreeBuilder inner) { inner.OpenComponent<NTButton>(0); inner.CloseComponent(); };
                }
            }
            """);

        Assert.Equal(6, frames.Count);
        Assert.All(frames, static frame => Assert.Null(frame.Parent));
    }

    [Fact]
    public async Task Only_Top_Level_Components_Of_A_Routable_Page_Are_Page_Roots() {
        var frames = await AnalyzerHarness.CaptureFramesAsync(Usings + """
            [Route("/page")]
            public sealed class Page : ComponentBase {
                private RenderFragment Template => builder => { builder.OpenComponent<NTIconButton>(0); builder.CloseComponent(); };

                protected override void BuildRenderTree(RenderTreeBuilder builder) {
                    builder.OpenComponent<NTMenu>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(inner => {
                        inner.OpenComponent<NTMenuButtonItem>(2);
                        inner.CloseComponent();
                    }));
                    builder.CloseComponent();
                }
            }
            """);

        Assert.True(Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTMenu").IsPageRoot);
        Assert.False(Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTMenuButtonItem").IsPageRoot);
        Assert.False(Assert.Single(frames, static frame => frame.MetadataName == "NTComponents.NTIconButton").IsPageRoot);
    }

    [Fact]
    public async Task Reads_Constant_Parameter_Values() {
        var frames = await AnalyzerHarness.CaptureFramesAsync(Usings + """
            public static class Fixture {
                public static void Build(RenderTreeBuilder builder, string runtime) {
                    builder.OpenComponent<NTButton>(0);
                    builder.AddComponentParameter(1, "Label", " ");
                    builder.AddComponentParameter(2, "AriaLabel", runtime);
                    builder.AddComponentParameter(3, "Variant", NTButtonVariant.Text);
                    builder.AddComponentParameter(4, "BackgroundColor", (NTColor)999);
                    builder.AddComponentParameter(5, "Elevation", (NTElevation?)null);
                    builder.AddComponentParameter(6, "Disabled", true);
                    builder.AddComponentParameter(7, "ProgressValue", 40);
                    builder.CloseComponent();
                }
            }
            """);

        var button = Assert.Single(frames);
        Assert.Equal(StringState.Empty, button.GetStringState("Label"));
        Assert.Equal(StringState.Unknown, button.GetStringState("AriaLabel"));
        Assert.Equal(StringState.NonEmpty, button.GetStringState("Tooltip", "default"));
        Assert.Equal("Text", button.GetEnumName("Variant", "Filled"));
        Assert.Null(button.GetEnumName("BackgroundColor"));
        Assert.Equal("None", button.GetEnumName("Elevation", "None"));
        Assert.Equal("Small", button.GetEnumName("ButtonSize", "Small"));
        Assert.True(button.GetBool("Disabled"));
        Assert.False(button.GetBool("ShowProgress"));
        Assert.Equal(40, button.GetNumber("ProgressValue"));
        Assert.True(OperationConstants.TryGetEnum(button.Attributes["BackgroundColor"].Value, out var undefined) && undefined.Member is null);
    }
}

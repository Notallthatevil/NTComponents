using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTFabMenuConfigurationAnalyzer_Tests {

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1039, NTC1040, and NTC1044.
    [Fact]
    public async Task Reports_Omitted_Required_Parameters_At_The_Component() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabMenuFactory.cs", source));

        Assert.Collection(
            diagnostics.OrderBy(static diagnostic => diagnostic.Id),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.MissingIconDiagnosticId,
                "NTFabMenu requires a non-null Icon parameter",
                "builder.OpenComponent<global::NTComponents.NTFabMenu>(0)",
                "FabMenuFactory.cs"),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.MissingAriaLabelDiagnosticId,
                "NTFabMenu requires a non-empty AriaLabel that describes the menu opened by the FAB",
                "builder.OpenComponent<global::NTComponents.NTFabMenu>(0)",
                "FabMenuFactory.cs"),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.InvalidMenuItemCountDiagnosticId,
                "NTFabMenu requires 2 to 6 NTFabMenuButtonItem or NTFabMenuAnchorItem children",
                "builder.OpenComponent<global::NTComponents.NTFabMenu>(0)",
                "FabMenuFactory.cs"));
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1039 and NTC1040.
    [Fact]
    public async Task Reports_Null_Icon_And_Whitespace_AriaLabel_At_Attribute_Values() {
        const string source = """
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.AddAttribute(1, "Icon", null);
        builder.AddAttribute(2, "AriaLabel", " ");
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(child => {
            child.OpenComponent<global::NTComponents.NTFabMenuButtonItem>(4);
            child.AddAttribute(5, "Label", "Save");
            child.CloseComponent();
            child.OpenComponent<global::NTComponents.NTFabMenuAnchorItem>(6);
            child.AddAttribute(7, "Label", "Docs");
            child.AddAttribute(8, "Href", "/docs");
            child.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabMenuFactory.cs", source));

        Assert.Collection(
            diagnostics.OrderBy(static diagnostic => diagnostic.Id),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.MissingIconDiagnosticId,
                "NTFabMenu requires a non-null Icon parameter",
                "null",
                "FabMenuFactory.cs"),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.MissingAriaLabelDiagnosticId,
                "NTFabMenu requires a non-empty AriaLabel that describes the menu opened by the FAB",
                "\" \"",
                "FabMenuFactory.cs"));
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1041.
    [Theory]
    [InlineData("BackgroundColor", "None")]
    [InlineData("BackgroundColor", "Transparent")]
    [InlineData("TextColor", "None")]
    [InlineData("TextColor", "Transparent")]
    [InlineData("SelectedFabBackgroundColor", "None")]
    [InlineData("SelectedFabBackgroundColor", "Transparent")]
    [InlineData("SelectedFabTextColor", "None")]
    [InlineData("SelectedFabTextColor", "Transparent")]
    [InlineData("MenuItemBackgroundColor", "None")]
    [InlineData("MenuItemBackgroundColor", "Transparent")]
    [InlineData("MenuItemTextColor", "None")]
    [InlineData("MenuItemTextColor", "Transparent")]
    public async Task Reports_Invisible_Color_Override_At_The_Color_Value(string attributeName, string colorName) {
        var source = $$"""
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder, object icon, string label, object childContent) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.AddAttribute(1, "Icon", icon);
        builder.AddAttribute(2, "AriaLabel", label);
        builder.AddAttribute(3, "ChildContent", childContent);
        builder.AddAttribute(4, "{{attributeName}}", global::NTComponents.TnTColor.{{colorName}});
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("FabMenuFactory.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTFabMenuConfigurationAnalyzer.InvisibleColorDiagnosticId,
            $"NTFabMenu {attributeName} must be a visible color",
            $"global::NTComponents.TnTColor.{colorName}",
            "FabMenuFactory.cs");
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1042.
    [Theory]
    [InlineData("Smallest", "Small")]
    [InlineData("XS", "Small")]
    [InlineData("Largest", "Large")]
    [InlineData("XL", "Large")]
    public async Task Reports_Remapped_ButtonSize_At_The_Size_Value(string sizeName, string renderedSizeName) {
        var source = $$"""
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder, object icon, string label, object childContent) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.AddAttribute(1, "Icon", icon);
        builder.AddAttribute(2, "AriaLabel", label);
        builder.AddAttribute(3, "ChildContent", childContent);
        builder.AddAttribute(4, "ButtonSize", global::NTComponents.Size.{{sizeName}});
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("FabMenuFactory.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTFabMenuConfigurationAnalyzer.UnsupportedSizeDiagnosticId,
            $"NTFabMenu does not support ButtonSize '{sizeName}' and will render with '{renderedSizeName}'",
            $"global::NTComponents.Size.{sizeName}",
            "FabMenuFactory.cs");
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1043.
    [Fact]
    public async Task Reports_Undefined_Placement_At_The_Placement_Value() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder, object icon, string label, object childContent) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.AddAttribute(1, "Icon", icon);
        builder.AddAttribute(2, "AriaLabel", label);
        builder.AddAttribute(3, "ChildContent", childContent);
        builder.AddAttribute(4, "Placement", (global::NTComponents.NTFabButtonPlacement)999);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("FabMenuFactory.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTFabMenuConfigurationAnalyzer.InvalidPlacementDiagnosticId,
            "NTFabMenu Placement must be Inline, LowerRight, LowerLeft, UpperRight, or UpperLeft",
            "(global::NTComponents.NTFabButtonPlacement)999",
            "FabMenuFactory.cs");
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1044.
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public async Task Reports_Static_Actionable_Item_Count_Outside_Two_Through_Six(int itemCount) {
        var items = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, itemCount).Select(index => $$"""
            child.OpenComponent<global::NTComponents.NTFabMenuButtonItem>({{index + 4}});
            child.AddAttribute({{index + 20}}, "Label", "Action {{index}}");
            child.CloseComponent();
"""));
        items += """
            child.OpenComponent<global::NTComponents.OtherComponent>(100);
            child.CloseComponent();
""";
        var source = $$"""
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder, object icon, string label) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.AddAttribute(1, "Icon", icon);
        builder.AddAttribute(2, "AriaLabel", label);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(child => {
{{items}}
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("FabMenuFactory.cs", source)));

        Assert.Equal(NTFabMenuConfigurationAnalyzer.InvalidMenuItemCountDiagnosticId, diagnostic.Id);
        Assert.Equal("NTFabMenu requires 2 to 6 NTFabMenuButtonItem or NTFabMenuAnchorItem children", diagnostic.GetMessage());
        Assert.Equal("FabMenuFactory.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.Contains("(RenderFragment)(child =>", GetSourceText(diagnostic), StringComparison.Ordinal);
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1044 defines the inclusive two-through-six boundary.
    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public async Task Does_Not_Report_Static_Actionable_Item_Count_At_Valid_Boundaries(int itemCount) {
        var items = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, itemCount).Select(index => $$"""
            child.OpenComponent<global::NTComponents.NTFabMenuButtonItem>({{index + 4}});
            child.AddAttribute({{index + 20}}, "Label", "Action {{index}}");
            child.CloseComponent();
"""));
        var source = $$"""
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder, object icon, string label) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.AddAttribute(1, "Icon", icon);
        builder.AddAttribute(2, "AriaLabel", label);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(child => {
{{items}}
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabMenuFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    // Behavior source: NTC1039, NTC1040, and NTC1044 apply to NTFabMenu configuration regardless of the containing executable form.
    [Fact]
    public async Task Reports_Invalid_Configuration_In_Each_Supported_Executable_Form() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class FabMenuFactory {
    public FabMenuFactory(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.CloseComponent();
    }

    public static void Build(RenderTreeBuilder builder) {
        void Local() {
            builder.OpenComponent<global::NTComponents.NTFabMenu>(1);
            builder.CloseComponent();
        }

        Action parenthesized = () => {
            builder.OpenComponent<global::NTComponents.NTFabMenu>(2);
            builder.CloseComponent();
        };

        Action<RenderTreeBuilder> simple = nested => {
            nested.OpenComponent<global::NTComponents.NTFabMenu>(3);
            nested.CloseComponent();
        };

        Action anonymous = delegate {
            builder.OpenComponent<global::NTComponents.NTFabMenu>(4);
            builder.CloseComponent();
        };

        Local();
        parenthesized();
        simple(builder);
        anonymous();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabMenuFactory.cs", source));

        Assert.Equal(15, diagnostics.Length);
        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTFabMenuConfigurationAnalyzer.MissingIconDiagnosticId));
        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTFabMenuConfigurationAnalyzer.MissingAriaLabelDiagnosticId));
        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTFabMenuConfigurationAnalyzer.InvalidMenuItemCountDiagnosticId));
        Assert.All(diagnostics, static diagnostic => Assert.Equal("FabMenuFactory.cs", diagnostic.Location.GetLineSpan().Path));
        Assert.All(diagnostics, static diagnostic => Assert.Contains("OpenComponent<global::NTComponents.NTFabMenu>", GetSourceText(diagnostic), StringComparison.Ordinal));
    }

    // Behavior source: NTC1041 and NTC1042 apply equally to named enum members and their equivalent compile-time enum constants.
    [Fact]
    public async Task Reports_Invalid_Enum_Constants_Expressed_As_Numeric_Casts() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder, object icon, string label, object childContent) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.AddAttribute(1, "Icon", icon);
        builder.AddAttribute(2, "AriaLabel", label);
        builder.AddAttribute(3, "ChildContent", childContent);
        builder.AddAttribute(4, "BackgroundColor", (global::NTComponents.TnTColor)1);
        builder.AddAttribute(5, "ButtonSize", (global::NTComponents.Size)0);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabMenuFactory.cs", source));

        Assert.Collection(
            diagnostics.OrderBy(static diagnostic => diagnostic.Id),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.InvisibleColorDiagnosticId,
                "NTFabMenu BackgroundColor must be a visible color",
                "(global::NTComponents.TnTColor)1",
                "FabMenuFactory.cs"),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.UnsupportedSizeDiagnosticId,
                "NTFabMenu does not support ButtonSize 'Smallest' and will render with 'Small'",
                "(global::NTComponents.Size)0",
                "FabMenuFactory.cs"));
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1045 and NTC1046.
    [Fact]
    public async Task Reports_Empty_Item_Label_And_Anchor_Href_At_Their_Values() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabMenuButtonItem>(0);
        builder.AddAttribute(1, "Label", " ");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTFabMenuAnchorItem>(2);
        builder.AddAttribute(3, "Label", "Docs");
        builder.AddAttribute(4, "Href", "");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabMenuFactory.cs", source));

        Assert.Collection(
            diagnostics.OrderBy(static diagnostic => diagnostic.Id),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.EmptyMenuItemLabelDiagnosticId,
                "NTFabMenuButtonItem requires a non-empty Label",
                "\" \"",
                "FabMenuFactory.cs"),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.EmptyMenuItemHrefDiagnosticId,
                "NTFabMenuAnchorItem requires a non-empty Href",
                "\"\"",
                "FabMenuFactory.cs"));
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1045.
    [Theory]
    [InlineData("NTFabMenuButtonItem", "NTFabMenuButtonItem")]
    [InlineData("NTFabMenuAnchorItem", "NTFabMenuAnchorItem")]
    public async Task Reports_Omitted_Item_Label_At_The_Item_Component(string componentType, string componentName) {
        var source = $$"""
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.{{componentType}}>(0);
        {{(componentType == "NTFabMenuAnchorItem" ? "builder.AddAttribute(1, \"Href\", \"/docs\");" : string.Empty)}}
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("FabMenuFactory.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTFabMenuConfigurationAnalyzer.EmptyMenuItemLabelDiagnosticId,
            $"{componentName} requires a non-empty Label",
            $"builder.OpenComponent<global::NTComponents.{componentType}>(0)",
            "FabMenuFactory.cs");
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1045.
    [Theory]
    [InlineData("NTFabMenuButtonItem", "\"\"")]
    [InlineData("NTFabMenuAnchorItem", "\"   \"")]
    public async Task Reports_Empty_Item_Label_At_The_Label_Value(string componentType, string labelExpression) {
        var source = $$"""
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.{{componentType}}>(0);
        builder.AddAttribute(1, "Label", {{labelExpression}});
        {{(componentType == "NTFabMenuAnchorItem" ? "builder.AddAttribute(2, \"Href\", \"/docs\");" : string.Empty)}}
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("FabMenuFactory.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTFabMenuConfigurationAnalyzer.EmptyMenuItemLabelDiagnosticId,
            $"{componentType} requires a non-empty Label",
            labelExpression,
            "FabMenuFactory.cs");
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1046.
    [Theory]
    [InlineData(null)]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    public async Task Reports_Omitted_Or_Empty_Anchor_Href(string? hrefExpression) {
        var hrefAttribute = hrefExpression is null ? string.Empty : $"builder.AddAttribute(2, \"Href\", {hrefExpression});";
        var source = $$"""
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabMenuAnchorItem>(0);
        builder.AddAttribute(1, "Label", "Docs");
        {{hrefAttribute}}
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("FabMenuFactory.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTFabMenuConfigurationAnalyzer.EmptyMenuItemHrefDiagnosticId,
            "NTFabMenuAnchorItem requires a non-empty Href",
            hrefExpression ?? "builder.OpenComponent<global::NTComponents.NTFabMenuAnchorItem>(0)",
            "FabMenuFactory.cs");
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1039-NTC1046 and the corresponding runtime contracts in NTFabMenu.Tests.cs.
    [Fact]
    public async Task Does_Not_Report_Valid_Static_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.AddAttribute(1, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(2, "AriaLabel", "Create item");
        builder.AddAttribute(3, "BackgroundColor", global::NTComponents.TnTColor.PrimaryContainer);
        builder.AddAttribute(4, "TextColor", global::NTComponents.TnTColor.OnPrimaryContainer);
        builder.AddAttribute(5, "ButtonSize", global::NTComponents.Size.Medium);
        builder.AddAttribute(6, "Placement", global::NTComponents.NTFabButtonPlacement.Inline);
        builder.AddAttribute(7, "ChildContent", (RenderFragment)(child => {
            child.OpenComponent<global::NTComponents.NTFabMenuButtonItem>(8);
            child.AddAttribute(9, "Label", "Save");
            child.CloseComponent();
            child.OpenComponent<global::NTComponents.NTFabMenuAnchorItem>(10);
            child.AddAttribute(11, "Label", "Docs");
            child.AddAttribute(12, "Href", "/docs");
            child.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabMenuFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1042 and NTC1043 define the supported static values.
    [Theory]
    [InlineData("Small", "Inline")]
    [InlineData("Medium", "LowerRight")]
    [InlineData("Large", "LowerLeft")]
    [InlineData("Medium", "UpperRight")]
    [InlineData("Medium", "UpperLeft")]
    public async Task Does_Not_Report_Supported_Size_And_Placement(string sizeName, string placementName) {
        var source = $$"""
using Microsoft.AspNetCore.Components.Rendering;

public static class FabMenuFactory {
    public static void Build(RenderTreeBuilder builder, object icon, string label, object childContent) {
        builder.OpenComponent<global::NTComponents.NTFabMenu>(0);
        builder.AddAttribute(1, "Icon", icon);
        builder.AddAttribute(2, "AriaLabel", label);
        builder.AddAttribute(3, "ChildContent", childContent);
        builder.AddAttribute(4, "ButtonSize", global::NTComponents.Size.{{sizeName}});
        builder.AddAttribute(5, "Placement", global::NTComponents.NTFabButtonPlacement.{{placementName}});
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabMenuFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    // Behavior source: generated Razor parity established by NTCarouselConfigurationAnalyzer_Tests.Supports_Razor_TypeCheck_And_AddComponentParameter.
    [Fact]
    public async Task Supports_Razor_TypeCheck_AddComponentParameter_And_NonGeneric_OpenComponent() {
        const string source = """
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;

public static class GeneratedFabMenu {
    public static void Build(RenderTreeBuilder builder, object childContent) {
        builder.OpenComponent(0, typeof(global::NTComponents.NTFabMenu));
        builder.AddComponentParameter(1, "Icon", RuntimeHelpers.TypeCheck<global::NTComponents.TnTIcon?>(null));
        builder.AddComponentParameter(2, "AriaLabel", RuntimeHelpers.TypeCheck<string>("Menu"));
        builder.AddComponentParameter(3, "ButtonSize", RuntimeHelpers.TypeCheck<global::NTComponents.Size>(global::NTComponents.Size.XL));
        builder.AddComponentParameter(4, "ChildContent", childContent);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("GeneratedFabMenu.razor.g.cs", source));

        Assert.Collection(
            diagnostics.OrderBy(static diagnostic => diagnostic.Id),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.MissingIconDiagnosticId,
                "NTFabMenu requires a non-null Icon parameter",
                "RuntimeHelpers.TypeCheck<global::NTComponents.TnTIcon?>(null)",
                "GeneratedFabMenu.razor.g.cs"),
            diagnostic => AssertDiagnostic(
                diagnostic,
                NTFabMenuConfigurationAnalyzer.UnsupportedSizeDiagnosticId,
                "NTFabMenu does not support ButtonSize 'XL' and will render with 'Large'",
                "RuntimeHelpers.TypeCheck<global::NTComponents.Size>(global::NTComponents.Size.XL)",
                "GeneratedFabMenu.razor.g.cs"));
    }

    private static void AssertDiagnostic(Diagnostic diagnostic, string id, string message, string sourceText, string path) {
        Assert.Equal(id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(message, diagnostic.GetMessage());
        Assert.Equal(path, diagnostic.Location.GetLineSpan().Path);
        Assert.Equal(sourceText, GetSourceText(diagnostic));
    }

    private static string GetSourceText(Diagnostic diagnostic) {
        Assert.NotNull(diagnostic.Location.SourceTree);
        return diagnostic.Location.SourceTree.GetText().ToString(diagnostic.Location.SourceSpan);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(params (string Path, string Source)[] sources) {
        var syntaxTrees = sources
            .Select(source => CSharpSyntaxTree.ParseText(source.Source, new CSharpParseOptions(LanguageVersion.Latest), source.Path))
            .ToImmutableArray();
        var references = new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location)
        };
        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await compilation
            .WithAnalyzers([new NTFabMenuConfigurationAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }

    private const string SupportTypes = """

namespace Microsoft.AspNetCore.Components {
    public delegate void RenderFragment(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder);
}

namespace Microsoft.AspNetCore.Components.CompilerServices {
    public static class RuntimeHelpers {
        public static T TypeCheck<T>(T value) => value;
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void OpenComponent(int sequence, global::System.Type componentType) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTFabMenu { }
    public class NTFabMenuButtonItem { }
    public class NTFabMenuAnchorItem { }
    public class OtherComponent { }
    public class TnTIcon { }
    public enum TnTColor { None, Transparent, PrimaryContainer, OnPrimaryContainer }
    public enum Size { Smallest, XS, Small, Medium, Large, Largest, XL }
    public enum NTFabButtonPlacement { Inline, LowerRight, LowerLeft, UpperRight, UpperLeft }
}
""";
}

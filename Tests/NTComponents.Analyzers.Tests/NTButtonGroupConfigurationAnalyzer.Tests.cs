using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTButtonGroupConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_Invalid_ButtonGroup_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonGroupFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Text);
        builder.AddAttribute(2, "BackgroundColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(3, "TextColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(4, "SelectedTextColor", global::NTComponents.TnTColor.None);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(5);
        builder.AddAttribute(6, "Variant", global::NTComponents.NTButtonVariant.Outlined);
        builder.AddAttribute(7, "BackgroundColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(8, "SelectedBackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(9);
        builder.AddAttribute(10, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(11, "SelectedBackgroundColor", global::NTComponents.TnTColor.None);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

        Assert.Equal(
            [
                NTButtonGroupConfigurationAnalyzer.TextSelectableDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.OpaqueBackgroundDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.OpaqueBackgroundDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.TransparentBackgroundDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.InvisibleTextColorDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.TransparentSelectedBackgroundDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.TransparentSelectedBackgroundDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.InvisibleSelectedTextColorDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_SelectedColors_When_Group_Is_Not_Selectable() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonGroupFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Text);
        builder.AddAttribute(2, "SelectionMode", global::NTComponents.NTButtonGroupSelectionMode.None);
        builder.AddAttribute(3, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(4, "SelectedBackgroundColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(5, "SelectedTextColor", global::NTComponents.TnTColor.Transparent);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_IconOnly_Item_Without_AriaLabel() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonGroupFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(0);
        builder.AddAttribute(1, "Icon", new global::NTComponents.TnTIcon());
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(2);
        builder.AddAttribute(3, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(4, "Label", " ");
        builder.AddAttribute(5, "AriaLabel", " ");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

        Assert.Equal(
            [
                NTButtonGroupConfigurationAnalyzer.MissingIconOnlyAriaLabelDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.MissingIconOnlyAriaLabelDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_For_Valid_Static_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonGroupFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Tonal);
        builder.AddAttribute(2, "BackgroundColor", global::NTComponents.TnTColor.SecondaryContainer);
        builder.AddAttribute(3, "TextColor", global::NTComponents.TnTColor.OnSecondaryContainer);
        builder.AddAttribute(4, "SelectedBackgroundColor", global::NTComponents.TnTColor.Secondary);
        builder.AddAttribute(5, "SelectedTextColor", global::NTComponents.TnTColor.OnSecondary);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(6);
        builder.AddAttribute(7, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(8, "AriaLabel", "Home");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(9);
        builder.AddAttribute(10, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(11, "Label", "Home");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_Explicit_Text_Selection_And_Field_Backed_Icon_Without_Label() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonGroupFactory {
    private static readonly global::NTComponents.TnTIcon HomeIcon = new();

    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Text);
        builder.AddAttribute(2, "SelectionMode", global::NTComponents.NTButtonGroupSelectionMode.Multiple);
        builder.AddAttribute(3, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(4);
        builder.AddAttribute(5, "Icon", HomeIcon);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

        Assert.Equal(
            [
                NTButtonGroupConfigurationAnalyzer.TextSelectableDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.MissingIconOnlyAriaLabelDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
        Assert.All(diagnostics, static diagnostic => Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity));
    }

    [Fact]
    public async Task DoesNotReport_For_Dynamic_Values_Unrelated_Components_And_Wrapped_Valid_Constants() {
        const string source = """
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonGroupFactory {
    public static void Build(
        RenderTreeBuilder builder,
        global::NTComponents.NTButtonVariant runtimeVariant,
        global::NTComponents.NTButtonGroupSelectionMode runtimeSelection,
        global::NTComponents.TnTColor runtimeColor,
        string runtimeLabel,
        string runtimeName) {
        builder.OpenComponent<global::NTComponents.OtherComponent>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Text);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(2);
        builder.AddAttribute(3, "Variant", runtimeVariant);
        builder.AddAttribute(4, "SelectionMode", runtimeSelection);
        builder.AddAttribute(5, "BackgroundColor", runtimeColor);
        builder.AddAttribute(6, "TextColor", runtimeColor);
        builder.AddAttribute(7, "SelectedBackgroundColor", runtimeColor);
        builder.AddAttribute(8, "SelectedTextColor", runtimeColor);
        builder.AddAttribute(9, runtimeName, runtimeColor);
        builder.CloseComponent();

        builder.OpenComponent(10, typeof(global::NTComponents.NTButtonGroup<string>));
        builder.AddComponentParameter(11, "Variant", RuntimeHelpers.TypeCheck((global::NTComponents.NTButtonVariant)2));
        builder.AddComponentParameter(12, "BackgroundColor", (global::NTComponents.TnTColor)6);
        builder.AddComponentParameter(13, "TextColor", (global::NTComponents.TnTColor)7);
        builder.AddComponentParameter(14, "SelectedBackgroundColor", (global::NTComponents.TnTColor)4);
        builder.AddComponentParameter(15, "SelectedTextColor", (global::NTComponents.TnTColor)5);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(16);
        builder.AddAttribute(17, "Variant", (global::NTComponents.NTButtonVariant)99);
        builder.AddAttribute(18, "SelectionMode", (global::NTComponents.NTButtonGroupSelectionMode)99);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(19);
        builder.AddAttribute(20, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(21, "Label", runtimeLabel);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(22);
        builder.AddAttribute(23, "Icon", (object?)null);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(24);
        builder.AddAttribute(25, "Icon", "symbol");
        builder.AddAttribute(26, "Label", "Home");
        builder.CloseComponent();
    }
}
""" + ExtendedSupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Analyzes_All_Block_Executable_Forms_Without_Duplicating_Nested_Diagnostics() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class ButtonGroupFactory {
    public ButtonGroupFactory(RenderTreeBuilder builder) {
        AddIconOnly(builder);

        void AddIconOnly(RenderTreeBuilder nestedBuilder) {
            nestedBuilder.OpenComponent(0, typeof(global::NTComponents.NTButtonGroupItem<string>));
            nestedBuilder.AddAttribute(1, "Icon", new global::NTComponents.TnTIcon());
            nestedBuilder.CloseComponent();
        }

        Action<int> parenthesized = (value) => {
            builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(2);
            builder.AddAttribute(3, "Icon", new global::NTComponents.TnTIcon());
            builder.CloseComponent();
        };
        Action<int> simple = value => {
            builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(4);
            builder.AddAttribute(5, "Icon", new global::NTComponents.TnTIcon());
            builder.CloseComponent();
        };
        Action anonymous = delegate {
            builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(6);
            builder.AddAttribute(7, "Icon", new global::NTComponents.TnTIcon());
            builder.CloseComponent();
        };
    }

    public int ExpressionBodied() => 42;
}
""" + ExtendedSupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

        Assert.Equal(4, diagnostics.Count(static diagnostic => diagnostic.Id == NTButtonGroupConfigurationAnalyzer.MissingIconOnlyAriaLabelDiagnosticId));
    }

    [Fact]
    public async Task Does_Not_Report_When_Required_Analyzer_Types_Are_Missing() {
        const string noTypes = "public static class Factory { public static void Build() { } }";
        const string noItem = """
namespace NTComponents {
    public class NTButtonGroup<T> { }
    public enum NTButtonVariant { Tonal }
    public enum NTButtonGroupSelectionMode { Single }
    public enum TnTColor { Primary }
}
""";
        const string noVariant = """
namespace NTComponents {
    public class NTButtonGroup<T> { }
    public class NTButtonGroupItem<T> { }
    public enum NTButtonGroupSelectionMode { Single }
    public enum TnTColor { Primary }
}
""";
        const string noSelection = """
namespace NTComponents {
    public class NTButtonGroup<T> { }
    public class NTButtonGroupItem<T> { }
    public enum NTButtonVariant { Tonal }
    public enum TnTColor { Primary }
}
""";
        const string noColor = """
namespace NTComponents {
    public class NTButtonGroup<T> { }
    public class NTButtonGroupItem<T> { }
    public enum NTButtonVariant { Tonal }
    public enum NTButtonGroupSelectionMode { Single }
}
""";

        Assert.Empty(await GetDiagnosticsAsync(("NoTypes.cs", noTypes)));
        Assert.Empty(await GetDiagnosticsAsync(("NoItem.cs", noItem)));
        Assert.Empty(await GetDiagnosticsAsync(("NoVariant.cs", noVariant)));
        Assert.Empty(await GetDiagnosticsAsync(("NoSelection.cs", noSelection)));
        Assert.Empty(await GetDiagnosticsAsync(("NoColor.cs", noColor)));
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(params (string Path, string Source)[] sources) {
        var syntaxTrees = sources
            .Select(source => CSharpSyntaxTree.ParseText(
                source.Source,
                new CSharpParseOptions(LanguageVersion.Latest),
                source.Path))
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

        var analyzer = new NTButtonGroupConfigurationAnalyzer();
        return await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }

    private const string SupportTypes = """

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void OpenComponent(int sequence, System.Type componentType) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTButtonGroup<TObjectType> { }
    public class NTButtonGroupItem<TObjectType> { }
    public class TnTIcon { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum NTButtonGroupSelectionMode { Single, Multiple, None }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, Secondary, OnSecondary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, SurfaceContainer, OnSurfaceVariant, InverseSurface, InverseOnSurface }
}
""";

    private const string ExtendedSupportTypes = SupportTypes + """

namespace Microsoft.AspNetCore.Components.CompilerServices {
    public static class RuntimeHelpers {
        public static T TypeCheck<T>(T value) => value;
    }
}

namespace NTComponents {
    public class OtherComponent { }
}
""";
}

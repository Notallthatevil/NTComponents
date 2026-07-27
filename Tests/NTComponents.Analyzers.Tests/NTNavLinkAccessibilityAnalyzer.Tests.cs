using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTNavLinkAccessibilityAnalyzer_Tests {

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1056.
    [Fact]
    public async Task Reports_Omitted_Label_At_The_Component() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class NavLinkFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTNavLink>(0);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("NavLinkFactory.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTNavLinkAccessibilityAnalyzer.EmptyLabelDiagnosticId,
            "NTNavLink requires a non-empty Label so the anchor has an accessible name",
            "builder.OpenComponent<global::NTComponents.NTNavLink>(0)",
            "NavLinkFactory.cs");
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1056.
    [Theory]
    [InlineData("null")]
    [InlineData("\"   \"")]
    public async Task Reports_Null_Or_Whitespace_Label_At_The_Label_Value(string labelExpression) {
        var source = $$"""
using Microsoft.AspNetCore.Components.Rendering;

public static class NavLinkFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTNavLink>(0);
        builder.AddAttribute(1, "Label", {{labelExpression}});
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("NavLinkFactory.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTNavLinkAccessibilityAnalyzer.EmptyLabelDiagnosticId,
            "NTNavLink requires a non-empty Label so the anchor has an accessible name",
            labelExpression,
            "NavLinkFactory.cs");
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1057.
    [Theory]
    [InlineData("aria-current")]
    [InlineData("aria-disabled")]
    public async Task Reports_Component_Owned_Aria_State_At_The_Attribute_Value(string attributeName) {
        var source = $$"""
using Microsoft.AspNetCore.Components.Rendering;

public static class NavLinkFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTNavLink>(0);
        builder.AddAttribute(1, "Label", "Home");
        builder.AddAttribute(2, "{{attributeName}}", "true");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("NavLinkFactory.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTNavLinkAccessibilityAnalyzer.ComponentOwnedAriaStateDiagnosticId,
            $"Do not set '{attributeName}' on NTNavLink. Use NTNavLink parameters and route matching so the component renders this ARIA state correctly.",
            "\"true\"",
            "NavLinkFactory.cs");
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1058.
    [Fact]
    public async Task Reports_TabIndex_When_The_Link_Is_Statically_Disabled() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class NavLinkFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTNavLink>(0);
        builder.AddAttribute(1, "Label", "Home");
        builder.AddAttribute(2, "Disabled", true);
        builder.AddAttribute(3, "tabindex", 0);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("NavLinkFactory.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTNavLinkAccessibilityAnalyzer.DisabledTabIndexDiagnosticId,
            "Disabled NTNavLink renders tabindex='-1'. Remove the explicit tabindex or do not disable the link.",
            "0",
            "NavLinkFactory.cs");
    }

    // Behavior source: AnalyzerReleases.Unshipped.md NTC1056-NTC1058 and NTNavLink.Tests.cs valid rendering contracts.
    [Fact]
    public async Task Does_Not_Report_Valid_Or_Unrelated_Components() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class NavLinkFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTNavLink>(0);
        builder.AddAttribute(1, "Label", "Home");
        builder.AddAttribute(2, "Disabled", false);
        builder.AddAttribute(3, "tabindex", 0);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTNavLink>(4);
        builder.AddAttribute(5, "Label", "Disabled link");
        builder.AddAttribute(6, "Disabled", true);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTNavLink>(7);
        builder.AddAttribute(8, "Label", "Described link");
        builder.AddAttribute(9, "aria-label", "Navigation home");
        builder.AddAttribute(10, "aria-describedby", "nav-description");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.OtherComponent>(11);
        builder.AddAttribute(12, "aria-current", "page");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("NavLinkFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    // Behavior source: generated Razor parity established by NTCarouselConfigurationAnalyzer_Tests.Supports_Razor_TypeCheck_And_AddComponentParameter.
    [Fact]
    public async Task Supports_Razor_TypeCheck_AddComponentParameter_And_NonGeneric_OpenComponent() {
        const string source = """
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;

public static class GeneratedNavLink {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent(0, typeof(global::NTComponents.NTNavLink));
        builder.AddComponentParameter(1, "Label", RuntimeHelpers.TypeCheck<string>("Home"));
        builder.AddComponentParameter(2, "Disabled", RuntimeHelpers.TypeCheck<bool>(true));
        builder.AddAttribute(3, "tabindex", RuntimeHelpers.TypeCheck<int>(0));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("GeneratedNavLink.razor.g.cs", source)));

        AssertDiagnostic(
            diagnostic,
            NTNavLinkAccessibilityAnalyzer.DisabledTabIndexDiagnosticId,
            "Disabled NTNavLink renders tabindex='-1'. Remove the explicit tabindex or do not disable the link.",
            "RuntimeHelpers.TypeCheck<int>(0)",
            "GeneratedNavLink.razor.g.cs");
    }

    // Behavior source: NTC1056 applies to NTNavLink configuration regardless of the containing executable form.
    [Fact]
    public async Task Reports_Missing_Label_In_Each_Supported_Executable_Form() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class NavLinkFactory {
    public NavLinkFactory(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTNavLink>(0);
        builder.CloseComponent();
    }

    public static void Build(RenderTreeBuilder builder) {
        void Local() {
            builder.OpenComponent<global::NTComponents.NTNavLink>(1);
            builder.CloseComponent();
        }

        Action parenthesized = () => {
            builder.OpenComponent<global::NTComponents.NTNavLink>(2);
            builder.CloseComponent();
        };

        Action<RenderTreeBuilder> simple = nested => {
            nested.OpenComponent<global::NTComponents.NTNavLink>(3);
            nested.CloseComponent();
        };

        Action anonymous = delegate {
            builder.OpenComponent<global::NTComponents.NTNavLink>(4);
            builder.CloseComponent();
        };

        Local();
        parenthesized();
        simple(builder);
        anonymous();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("NavLinkFactory.cs", source));

        Assert.Equal(5, diagnostics.Length);
        Assert.All(diagnostics, static diagnostic => Assert.Equal(NTNavLinkAccessibilityAnalyzer.EmptyLabelDiagnosticId, diagnostic.Id));
        Assert.All(diagnostics, static diagnostic => Assert.Equal("NTNavLink requires a non-empty Label so the anchor has an accessible name", diagnostic.GetMessage()));
        Assert.All(diagnostics, static diagnostic => Assert.Equal("NavLinkFactory.cs", diagnostic.Location.GetLineSpan().Path));
        Assert.All(diagnostics, static diagnostic => Assert.EndsWith("OpenComponent<global::NTComponents.NTNavLink>", GetSourceText(diagnostic).Split('(')[0], StringComparison.Ordinal));
    }

    // Behavior source: analyzer soundness invariant; target-specific diagnostics require a statically identified NTNavLink and attribute name.
    [Fact]
    public async Task Does_Not_Report_When_Target_Or_Attribute_Name_Is_Not_Statically_Identified() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public abstract class AbstractFactory {
    public abstract void Build(RenderTreeBuilder builder);
}

public static class NavLinkFactory {
    public static void Build(RenderTreeBuilder builder, Type componentType, string attributeName) {
        Action parenthesized = () => NoOp();
        Action<int> simple = value => NoOp();

        builder.OpenComponent(0, componentType);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTNavLink>(1);
        builder.AddAttribute(2, "Label", ("Home"));
        builder.AddAttribute(3, "Disabled", (false));
        builder.AddAttribute(4, "tabindex", 0);
        builder.AddAttribute(5, attributeName, "page");
        builder.CloseComponent();

        parenthesized();
        simple(0);
    }

    private static void NoOp() { }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("NavLinkFactory.cs", source));

        Assert.Empty(diagnostics);
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
            .WithAnalyzers([new NTNavLinkAccessibilityAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }

    private const string SupportTypes = """

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
    public class NTNavLink { }
    public class OtherComponent { }
}
""";
}

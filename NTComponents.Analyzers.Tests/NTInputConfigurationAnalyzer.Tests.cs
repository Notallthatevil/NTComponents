using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTInputConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_ValueBinding_Without_ValueExpression_For_ValidationInputs() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class InputFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTInputText>(0);
        builder.AddAttribute(1, "Value", "hello");
        builder.AddAttribute(2, "ValueChanged", new object());
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSelect<int>>(3);
        builder.AddAttribute(4, "ValueChanged", new object());
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public abstract class NTFormControlBaseCore<TValue> { }
    public abstract class NTInputBase<TValue> : NTFormControlBaseCore<TValue> { }
    public abstract class NTBooleanInputBase : NTFormControlBaseCore<bool> { }
    public class NTInputText : NTInputBase<string?> { }
    public class NTSelect<TValue> : NTFormControlBaseCore<TValue> { }
    public class NTForm { }
    public enum TextInputType { Text, Tel }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("InputFactory.cs", source));

        Assert.Equal(
            [
                NTInputConfigurationAnalyzer.MissingValueExpressionDiagnosticId,
                NTInputConfigurationAnalyzer.MissingValueExpressionDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_ValueBinding_When_ValueExpression_Is_Present() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class InputFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTInputText>(0);
        builder.AddAttribute(1, "Value", "hello");
        builder.AddAttribute(2, "ValueChanged", new object());
        builder.AddAttribute(3, "ValueExpression", new object());
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public abstract class NTFormControlBaseCore<TValue> { }
    public abstract class NTInputBase<TValue> : NTFormControlBaseCore<TValue> { }
    public abstract class NTBooleanInputBase : NTFormControlBaseCore<bool> { }
    public class NTInputText : NTInputBase<string?> { }
    public class NTForm { }
    public enum TextInputType { Text, Tel }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("InputFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_Raw_Required_Attribute_On_BooleanInputs() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class InputFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTInputCheckbox>(0);
        builder.AddAttribute(1, "required", true);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputSwitch>(2);
        builder.AddAttribute(3, "required", true);
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public abstract class NTFormControlBaseCore<TValue> { }
    public abstract class NTInputBase<TValue> : NTFormControlBaseCore<TValue> { }
    public abstract class NTBooleanInputBase : NTFormControlBaseCore<bool> { }
    public class NTInputCheckbox : NTBooleanInputBase { }
    public class NTInputSwitch : NTBooleanInputBase { }
    public class NTForm { }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("InputFactory.cs", source));

        Assert.Equal(
            [
                NTInputConfigurationAnalyzer.BooleanInputRequiredAttributeDiagnosticId,
                NTInputConfigurationAnalyzer.BooleanInputRequiredAttributeDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_Required_Parameter_On_BooleanInputs() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class InputFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTInputCheckbox>(0);
        builder.AddAttribute(1, "Required", true);
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public abstract class NTFormControlBaseCore<TValue> { }
    public abstract class NTInputBase<TValue> : NTFormControlBaseCore<TValue> { }
    public abstract class NTBooleanInputBase : NTFormControlBaseCore<bool> { }
    public class NTInputCheckbox : NTBooleanInputBase { }
    public class NTForm { }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("InputFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_Empty_RequiredSupportingText_When_Form_Shows_Required_Text() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FormFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTForm>(0);
        builder.AddAttribute(1, "ShowRequiredSupportingText", true);
        builder.AddAttribute(2, "RequiredSupportingText", " ");
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public abstract class NTFormControlBaseCore<TValue> { }
    public abstract class NTInputBase<TValue> : NTFormControlBaseCore<TValue> { }
    public abstract class NTBooleanInputBase : NTFormControlBaseCore<bool> { }
    public class NTForm { }
}
""";

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("FormFactory.cs", source)));

        Assert.Equal(NTInputConfigurationAnalyzer.EmptyRequiredSupportingTextDiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task Reports_ComponentOwned_ValidationAttributes_On_FormControlBase_Components() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class InputFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTInputSlider<int>>(0);
        builder.AddAttribute(1, "aria-invalid", true);
        builder.AddAttribute(2, "aria-errormessage", "error");
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public abstract class NTFormControlBaseCore<TValue> { }
    public abstract class NTInputBase<TValue> : NTFormControlBaseCore<TValue> { }
    public abstract class NTBooleanInputBase : NTFormControlBaseCore<bool> { }
    public class NTInputSlider<TNumber> : NTFormControlBaseCore<TNumber> { }
    public class NTForm { }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("InputFactory.cs", source));

        Assert.Equal(
            [
                NTInputConfigurationAnalyzer.ComponentOwnedInputAttributeDiagnosticId,
                NTInputConfigurationAnalyzer.ComponentOwnedInputAttributeDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
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

        var analyzer = new NTInputConfigurationAnalyzer();
        return await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }
}

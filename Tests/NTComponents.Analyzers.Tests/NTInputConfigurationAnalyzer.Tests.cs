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

    [Fact]
    public async Task Reports_Only_Statically_Invalid_Phone_Masks() {
        const string source = """
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;

public static class InputFactory {
    public static void Build(RenderTreeBuilder builder, global::NTComponents.TextInputType runtimeType, string runtimeMask) {
        builder.OpenComponent<global::NTComponents.OtherComponent>(20);
        builder.AddAttribute(21, "PhoneMask", "ignored");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputText>(0);
        builder.AddAttribute(1, "PhoneMask", "(###) ###-####");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputText>(2);
        builder.AddAttribute(3, "InputType", global::NTComponents.TextInputType.Text);
        builder.AddAttribute(4, "PhoneMask", "###");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputText>(5);
        builder.AddAttribute(6, "InputType", global::NTComponents.TextInputType.Tel);
        builder.AddAttribute(7, "PhoneMask", (string?)null);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputText>(8);
        builder.AddAttribute(9, "InputType", global::NTComponents.TextInputType.Tel);
        builder.AddAttribute(10, "PhoneMask", "letters only");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputText>(11);
        builder.AddAttribute(12, "InputType", (global::NTComponents.TextInputType)1);
        builder.AddAttribute(13, "PhoneMask", RuntimeHelpers.TypeCheck<string>("###"));
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputText>(14);
        builder.AddAttribute(15, "InputType", (global::NTComponents.TextInputType)99);
        builder.AddAttribute(16, "PhoneMask", "letters only");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputText>(17);
        builder.AddAttribute(18, "InputType", runtimeType);
        builder.AddAttribute(19, "PhoneMask", runtimeMask);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputText>(22);
        builder.AddAttribute(23, "InputType", global::NTComponents.TextInputType.Tel);
        builder.AddAttribute(24, "PhoneMask", " ");
        builder.CloseComponent();
    }
}
""" + ExtendedSupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("InputFactory.cs", source));

        Assert.Equal(
            [
                NTInputConfigurationAnalyzer.PhoneMaskRequiresTelDiagnosticId,
                NTInputConfigurationAnalyzer.PhoneMaskRequiresTelDiagnosticId,
                NTInputConfigurationAnalyzer.InvalidPhoneMaskDiagnosticId,
                NTInputConfigurationAnalyzer.InvalidPhoneMaskDiagnosticId,
                NTInputConfigurationAnalyzer.InvalidPhoneMaskDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Reports_Only_Statically_Empty_Required_Text_And_Component_Owned_Attributes() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class InputFactory {
    public static void Build(RenderTreeBuilder builder, bool runtimeFlag, string runtimeText, string runtimeName) {
        builder.OpenComponent<global::NTComponents.NTForm>(0);
        builder.AddAttribute(1, "ShowRequiredSupportingText", false);
        builder.AddAttribute(2, "RequiredSupportingText", " ");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTForm>(3);
        builder.AddAttribute(4, "ShowRequiredSupportingText", true);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTForm>(5);
        builder.AddAttribute(6, "ShowRequiredSupportingText", true);
        builder.AddAttribute(7, "RequiredSupportingText", (string?)null);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTForm>(8);
        builder.AddAttribute(9, "ShowRequiredSupportingText", runtimeFlag);
        builder.AddAttribute(10, "RequiredSupportingText", runtimeText);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputText>(11);
        builder.AddComponentParameter(12, "Value", "hello");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTInputText>(13);
        builder.AddAttribute(14, "aria-describedby", "hint");
        builder.AddAttribute(15, "autocomplete", "name");
        builder.AddAttribute(16, "autofocus", true);
        builder.AddAttribute(17, "disabled", false);
        builder.AddAttribute(18, "id", "name");
        builder.AddAttribute(19, "oninput", new object());
        builder.AddAttribute(20, "placeholder", "Name");
        builder.AddAttribute(21, "readonly", false);
        builder.AddAttribute(22, "title", "Name");
        builder.AddAttribute(23, "type", "text");
        builder.AddAttribute(24, "value", "hello");
        builder.AddAttribute(25, runtimeName, "ignored");
        builder.CloseComponent();
    }
}
""" + ExtendedSupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("InputFactory.cs", source));

        Assert.Equal(1, diagnostics.Count(static diagnostic => diagnostic.Id == NTInputConfigurationAnalyzer.EmptyRequiredSupportingTextDiagnosticId));
        Assert.Equal(1, diagnostics.Count(static diagnostic => diagnostic.Id == NTInputConfigurationAnalyzer.MissingValueExpressionDiagnosticId));
        Assert.Equal(11, diagnostics.Count(static diagnostic => diagnostic.Id == NTInputConfigurationAnalyzer.ComponentOwnedInputAttributeDiagnosticId));
        Assert.Equal(13, diagnostics.Length);
    }

    [Fact]
    public async Task Analyzes_All_Block_Executable_Forms_Without_Duplicating_Nested_Diagnostics() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class InputFactory {
    public InputFactory(RenderTreeBuilder builder) {
        AddInvalidRequired(builder);

        void AddInvalidRequired(RenderTreeBuilder nestedBuilder) {
            nestedBuilder.OpenComponent(0, typeof(global::NTComponents.NTInputCheckbox));
            nestedBuilder.AddAttribute(1, "required", true);
            nestedBuilder.CloseComponent();
        }

        Action<int> parenthesized = (value) => {
            builder.OpenComponent<global::NTComponents.NTInputCheckbox>(2);
            builder.AddAttribute(3, "required", true);
            builder.CloseComponent();
        };
        Action<int> simple = value => {
            builder.OpenComponent<global::NTComponents.NTInputCheckbox>(4);
            builder.AddAttribute(5, "required", true);
            builder.CloseComponent();
        };
        Action anonymous = delegate {
            builder.OpenComponent<global::NTComponents.NTInputCheckbox>(6);
            builder.AddAttribute(7, "required", true);
            builder.CloseComponent();
        };
    }

    public int ExpressionBodied() => 42;
}
""" + ExtendedSupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("InputFactory.cs", source));

        Assert.Equal(4, diagnostics.Count(static diagnostic => diagnostic.Id == NTInputConfigurationAnalyzer.BooleanInputRequiredAttributeDiagnosticId));
    }

    [Fact]
    public async Task Does_Not_Report_When_Analyzer_Type_Surface_Is_Absent_Or_Partial() {
        const string noComponents = """
public static class Factory {
    public static void Build() { }
}
""";
        const string onlyFormControl = """
namespace NTComponents {
    public abstract class NTFormControlBaseCore<TValue> { }
}
""";
        const string onlyForm = """
namespace NTComponents {
    public class NTForm { }
}
""";

        Assert.Empty(await GetDiagnosticsAsync(("NoComponents.cs", noComponents)));
        Assert.Empty(await GetDiagnosticsAsync(("OnlyFormControl.cs", onlyFormControl)));
        Assert.Empty(await GetDiagnosticsAsync(("OnlyForm.cs", onlyForm)));
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

    private const string ExtendedSupportTypes = """

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void OpenComponent(int sequence, System.Type componentType) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace Microsoft.AspNetCore.Components.CompilerServices {
    public static class RuntimeHelpers {
        public static T TypeCheck<T>(T value) => value;
    }
}

namespace NTComponents {
    public abstract class NTFormControlBaseCore<TValue> { }
    public abstract class NTInputBase<TValue> : NTFormControlBaseCore<TValue> { }
    public abstract class NTBooleanInputBase : NTFormControlBaseCore<bool> { }
    public class NTInputText : NTInputBase<string?> { }
    public class NTInputCheckbox : NTBooleanInputBase { }
    public class NTForm { }
    public class OtherComponent { }
    public enum TextInputType { Text, Tel }
}
""";
}

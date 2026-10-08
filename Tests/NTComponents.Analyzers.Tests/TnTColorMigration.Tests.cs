using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NTComponents.Analyzers.CodeFixes;

namespace NTComponents.Analyzers.Tests;

public sealed class TnTColorMigration_Tests {
    private const string ColorTypes = """
namespace NTComponents {
    public enum TnTColor { Primary = 1, OnPrimary = 3, None = 66, Black = 67, White = 68 }
    public enum NTColor { Primary = 1, OnPrimary = 3 }
    public static class TnTColorEnumExt {
        public static string ToCssTnTColorVariable(this TnTColor color) => "legacy";
    }
    public static class NTColorEnumExt {
        public static string ToCssNTColorVariable(this NTColor color) => "modern";
    }
}
""";

    [Theory]
    [InlineData("using NTComponents; class Consumer { TnTColor Color = TnTColor.Primary; }")]
    [InlineData("class Consumer { NTComponents.TnTColor Color = global::NTComponents.TnTColor.Primary; }")]
    [InlineData("using Color = NTComponents.TnTColor; class Consumer { Color Value = Color.Primary; }")]
    [InlineData("using static NTComponents.TnTColor; class Consumer { object Value = Primary; }")]
    [InlineData("using NTComponents; class NTColor { } class Consumer { TnTColor Value = TnTColor.Primary; }")]
    [InlineData("using NTComponents; class Consumer { TnTColor? Color = TnTColor.Primary; TnTColor[] Values = [TnTColor.OnPrimary]; }")]
    public async Task LegacyReferences_FixAllProducesCompilableNTColorSource(string source) {
        using var workspace = CreateWorkspace(source);
        var project = workspace.CurrentSolution.Projects.Single();
        var before = await GetDiagnosticsAsync(project);
        Assert.Contains(before, diagnostic => diagnostic.Id == TnTColorMigrationAnalyzer.DiagnosticId);

        var fixedSolution = await FixAllAsync(project, TnTColorMigrationAnalyzer.DiagnosticId);
        var fixedProject = fixedSolution.GetProject(project.Id)!;
        var text = (await fixedProject.Documents.Last().GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        Assert.DoesNotContain("TnTColor", text);
        Assert.Empty(await GetDiagnosticsAsync(fixedProject));
        var compilation = await fixedProject.GetCompilationAsync(TestContext.Current.CancellationToken);
        Assert.Empty(compilation!.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public async Task UnrelatedEnumsStringsAndComments_DoNotReport() {
        const string source = """
namespace Other { enum TnTColor { Primary } }
class Consumer {
    Other.TnTColor Color = Other.TnTColor.Primary;
    string Name = "TnTColor.Primary";
    // TnTColor.Primary
}
""";
        using var workspace = CreateWorkspace(source);
        Assert.Empty(await GetDiagnosticsAsync(workspace.CurrentSolution.Projects.Single()));
    }

    [Theory]
    [InlineData("None")]
    [InlineData("Black")]
    [InlineData("White")]
    public async Task RemovedMembers_ReportManualMigrationWithoutAnUnsafeFix(string member) {
        using var workspace = CreateWorkspace($"class Consumer {{ object Color = NTComponents.TnTColor.{member}; }}");
        var project = workspace.CurrentSolution.Projects.Single();
        var diagnostic = Assert.Single(await GetDiagnosticsAsync(project));
        Assert.Equal(TnTColorMigrationAnalyzer.RemovedColorDiagnosticId, diagnostic.Id);
        var actions = new List<CodeAction>();
        var provider = new TnTColorMigrationCodeFixProvider();
        await provider.RegisterCodeFixesAsync(new CodeFixContext(project.Documents.Last(), diagnostic, (action, _) => actions.Add(action), default));
        Assert.Empty(actions);
    }

    [Theory]
    [InlineData("class Consumer { string Css = NTComponents.NTColor.Primary.ToCssTnTColorVariable(); }")]
    [InlineData("class Consumer { string Css = NTComponents.TnTColorEnumExt.ToCssTnTColorVariable(NTComponents.NTColor.Primary); }")]
    [InlineData("using Helper = NTComponents.TnTColorEnumExt; class Consumer { string Css = Helper.ToCssTnTColorVariable(NTComponents.NTColor.Primary); }")]
    public async Task OldCssHelperAfterTypeMigration_FixAllProducesCompilableSource(string source) {
        using var workspace = CreateWorkspace("using NTComponents; " + source);
        var project = workspace.CurrentSolution.Projects.Single();
        Assert.Contains(await GetDiagnosticsAsync(project), diagnostic => diagnostic.Id == TnTColorMigrationAnalyzer.CssHelperDiagnosticId);
        var solution = await FixAllAsync(project, TnTColorMigrationAnalyzer.CssHelperDiagnosticId);
        var compilation = await solution.GetProject(project.Id)!.GetCompilationAsync(TestContext.Current.CancellationToken);
        Assert.Empty(compilation!.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public async Task GeneratedRazorDiagnostic_MapsToTheOriginalRazorFile() {
        const string source = """
class Consumer {
    object Render() =>
#line 12 "Pages/Example.razor"
        NTComponents.TnTColor.Primary;
#line default
}
""";
        using var workspace = CreateWorkspace(source);
        var diagnostic = Assert.Single(await GetDiagnosticsAsync(workspace.CurrentSolution.Projects.Single()));
        Assert.Equal("Pages/Example.razor", diagnostic.Location.GetMappedLineSpan().Path);
        Assert.Equal(11, diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line);
    }

    private static AdhocWorkspace CreateWorkspace(string source) {
        var workspace = new AdhocWorkspace();
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var project = workspace.AddProject("Consumer", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview))
            .AddMetadataReferences(references);
        project = project.AddDocument("ColorTypes.cs", SourceText.From(ColorTypes)).Project;
        project = project.AddDocument("Consumer.cs", SourceText.From(source)).Project;
        Assert.True(workspace.TryApplyChanges(project.Solution));
        return workspace;
    }

    private static async Task<Diagnostic[]> GetDiagnosticsAsync(Project project) {
        var compilation = await project.GetCompilationAsync(TestContext.Current.CancellationToken);
        return (await compilation!.WithAnalyzers([new TnTColorMigrationAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken)).Where(diagnostic => diagnostic.Location.SourceTree?.FilePath != "ColorTypes.cs").ToArray();
    }

    private static async Task<Solution> FixAllAsync(Project project, string diagnosticId) {
        var provider = new TnTColorMigrationCodeFixProvider();
        var context = new FixAllContext(project.Documents.Last(), provider, FixAllScope.Project, diagnosticId, [diagnosticId], new ColorMigrationDiagnosticProvider(), default);
        var action = await provider.GetFixAllProvider().GetFixAsync(context);
        Assert.NotNull(action);
        var operations = await action.GetOperationsAsync(default);
        return Assert.Single(operations.OfType<ApplyChangesOperation>()).ChangedSolution;
    }
}

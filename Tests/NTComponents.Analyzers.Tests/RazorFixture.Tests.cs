using System.Text.RegularExpressions;

namespace NTComponents.Analyzers.Tests;

/// <summary>
///     Analyzes the code the real Razor compiler generated for each file in RazorFixtures and checks the diagnostics against the
///     markers in the markup. A marker such as <c>@* NTC1008 *@</c> expects those IDs on its own line, which also proves the
///     diagnostic maps back to the .razor file; <c>@* unmapped NTC1008 *@</c> expects one with no markup location.
/// </summary>
public sealed class RazorFixture_Tests {
    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "RazorFixtures");
    private static readonly Regex Marker = new(@"@\*\s*(?<unmapped>unmapped\s+)?(?<ids>NTC\d{4}(?:\s+NTC\d{4})*)\s*\*@", RegexOptions.Compiled);

    public static TheoryData<string> GeneratedFiles() => [.. Directory.GetFiles(FixtureDirectory, "*_razor.g.cs").Select(static path => Path.GetFileName(path))];

    [Theory]
    [MemberData(nameof(GeneratedFiles))]
    public async Task Diagnostics_Match_The_Markers(string generatedFile) {
        var source = await File.ReadAllTextAsync(Path.Combine(FixtureDirectory, generatedFile), TestContext.Current.CancellationToken);
        // Razor emits @code members into a separate declaration file.
        var declarationPath = Path.Combine(FixtureDirectory, generatedFile.Replace("_razor.g.cs", "_razor.decl.g.cs"));
        string[] sources = File.Exists(declarationPath) ? [source, await File.ReadAllTextAsync(declarationPath, TestContext.Current.CancellationToken)] : [source];
        var razorPath = Regex.Match(source, "#pragma checksum \"(?<path>[^\"]+)\"").Groups["path"].Value;
        var razorLines = await File.ReadAllLinesAsync(razorPath, TestContext.Current.CancellationToken);

        var expected = razorLines
            .SelectMany(static (line, index) => Marker.Matches(line).SelectMany(match => match.Groups["ids"].Value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => match.Groups["unmapped"].Success ? $"unmapped {id}" : $"line {index + 1} {id}")))
            .Order(StringComparer.Ordinal);

        var actual = (await AnalyzerHarness.AnalyzeAsync(sources))
            .Select(static diagnostic => diagnostic.Location.GetMappedLineSpan() is { HasMappedPath: true } span
                ? $"line {span.StartLinePosition.Line + 1} {diagnostic.Id}"
                : $"unmapped {diagnostic.Id}")
            .Order(StringComparer.Ordinal);

        Assert.Equal(expected, actual);
    }
}

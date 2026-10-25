namespace NTComponents.Analyzers.Tests;

public sealed class RuleTypeNames_Tests {

    /// <summary>A misspelled or renamed type silently disables its rules, so every name a rule matches on must exist.</summary>
    [Fact]
    public void Every_Type_A_Rule_Names_Exists_In_NTComponents() {
        var library = typeof(NTButton).Assembly;

        var missing = NTComponentUsageAnalyzer.DefaultRegistry.TypeNames.Where(name => library.GetType(name) is null).Order(StringComparer.Ordinal);

        Assert.Empty(missing);
    }
}

namespace NTComponents.Analyzers.Tests;

/// <summary>Builds and analyzes a single component written as <c>Name=expression | Name=expression</c>; <c>@</c> stands for an unreadable <c>@attributes</c> splat.</summary>
internal static class ComponentSnippet {
    public const string Fragment = "(RenderFragment)(b => { })";

    /// <summary>Returns the IDs reported for the component, space separated. <paramref name="only" /> keeps just IDs starting with it, for components other rule sets also check.</summary>
    public static async Task<string> IdsAsync(string component, string parameters, string only = "") {
        var statements = parameters.Split('|')
            .Select(static item => item.Trim())
            .Where(static item => item.Length > 0)
            .Select(static (item, index) => {
                if (item == "@") {
                    return "builder.AddMultipleAttributes(1, new System.Collections.Generic.Dictionary<string, object>());";
                }

                var split = item.IndexOf('=');
                return $"builder.AddComponentParameter({index + 1}, \"{item[..split]}\", {item[(split + 1)..]});";
            });

        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($"builder.OpenComponent<{component}>(0);\n{string.Join("\n", statements)}\nbuilder.CloseComponent();");
        return string.Join(' ', diagnostics.Ids().Where(id => id.StartsWith(only, StringComparison.Ordinal)));
    }
}

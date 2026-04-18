namespace NTComponents.Site.Theming;

public sealed record ThemeConversionResult(
    IReadOnlyList<ThemeCssOutput> Outputs,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings) {

    public bool IsSuccess => Errors.Count == 0;

    public ThemeCssOutput? GetOutput(string fileName) =>
        Outputs.FirstOrDefault(output => string.Equals(output.FileName, fileName, StringComparison.OrdinalIgnoreCase));
}

public sealed record ThemeCssOutput(
    string FileName,
    ThemeVariant Variant,
    ThemeContrast Contrast,
    string Css);

public enum ThemeVariant {
    Light,
    Dark
}

public enum ThemeContrast {
    Default,
    Medium,
    High
}

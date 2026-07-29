namespace NTComponents;

/// <summary>
///     Font families that <see cref="NTHeadDependencies" /> loads from Google Fonts when no custom font stylesheet is configured.
/// </summary>
[Flags]
public enum NTFontFamily {
    /// <summary>
    ///     Do not load any default font families.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Load Roboto in the weights used by NTComponents.
    /// </summary>
    Roboto = 1 << 0,

    /// <summary>
    ///     Load the outlined Material Symbols family used by default, filled, and outlined icons.
    /// </summary>
    MaterialSymbolsOutlined = 1 << 1,

    /// <summary>
    ///     Load the rounded Material Symbols family.
    /// </summary>
    MaterialSymbolsRounded = 1 << 2,

    /// <summary>
    ///     Load the sharp Material Symbols family.
    /// </summary>
    MaterialSymbolsSharp = 1 << 3,

    /// <summary>
    ///     Load every font family supported by NTComponents.
    /// </summary>
    All = Roboto | MaterialSymbolsOutlined | MaterialSymbolsRounded | MaterialSymbolsSharp
}

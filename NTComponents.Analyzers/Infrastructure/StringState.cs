namespace NTComponents.Analyzers;

/// <summary>What the analyzer can prove about a string parameter.</summary>
internal enum StringState {
    /// <summary>The value is not a compile-time constant.</summary>
    Unknown,

    /// <summary>The value is null, empty, or whitespace.</summary>
    Empty,

    /// <summary>The value is a non-blank constant.</summary>
    NonEmpty
}

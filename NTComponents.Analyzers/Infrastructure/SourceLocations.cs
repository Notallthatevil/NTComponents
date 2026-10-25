using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>
///     Chooses diagnostic locations that map back to the user's markup. Razor hides most generated code behind
///     <c>#line hidden</c>; only C# expressions and attribute names map to the <c>.razor</c> file.
/// </summary>
internal static class SourceLocations {

    /// <summary>Plain C# has no hidden regions, so every location there counts as mapped.</summary>
    public static bool IsMapped(Location location) => location.SourceTree?.HasHiddenRegions() != true || location.GetMappedLineSpan().HasMappedPath;

    /// <summary>Returns the first mapped candidate in Razor-generated code, otherwise <paramref name="fallback" />.</summary>
    public static Location Prefer(Location fallback, IEnumerable<Location?> candidates) {
        if (fallback.SourceTree?.HasHiddenRegions() != true) {
            return fallback;
        }

        return candidates.FirstOrDefault(static candidate => candidate is not null && candidate.GetMappedLineSpan().HasMappedPath) ?? fallback;
    }
}

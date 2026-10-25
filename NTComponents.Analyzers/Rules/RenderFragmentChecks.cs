using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>Tells whether a RenderFragment parameter is certainly set, certainly unset, or cannot be known.</summary>
internal static class RenderFragmentChecks {

    /// <summary>The attribute's location, or the component's when Razor gives the attribute (a raw HTML attribute or a fragment) no markup location.</summary>
    public static Location MappedLocationOf(ComponentFrame frame, string name) => SourceLocations.IsMapped(frame.LocationOf(name)) ? frame.LocationOf(name) : frame.Location;

    /// <summary>
    ///     Returns true for a fragment written inline (a lambda, as Razor emits for child content), false when the parameter is omitted
    ///     or null, and null when it is a variable that may be null or when <c>@attributes</c> may supply it.
    /// </summary>
    public static bool? IsSet(ComponentFrame frame, string name) {
        if (!frame.Attributes.TryGetValue(name, out var attribute)) {
            return frame.HasSplattedAttributes ? null : false;
        }

        if (OperationConstants.IsNull(attribute.Value)) {
            return false;
        }

        var value = OperationConstants.Unwrap(attribute.Value);
        if (value is IDelegateCreationOperation creation) {
            value = creation.Target;
        }

        return value is IAnonymousFunctionOperation ? true : null;
    }
}

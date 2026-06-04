using Microsoft.AspNetCore.Components;
using NTComponents.Interfaces;

namespace NTComponents.Core;

/// <summary>
///     Base component for all NTComponents.
/// </summary>
/// <seealso cref="ComponentBase" />
/// <seealso cref="INTComponentBase" />
/// <seealso cref="IAsyncDisposable" />
public abstract class NTComponentBase : ComponentBase, INTComponentBase {

    /// <inheritdoc />
    [Parameter(CaptureUnmatchedValues = true)]
    public virtual IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool? AutoFocus { get; set; }

    /// <summary>
    ///     Unique identifier for the component.
    /// </summary>
    public string ComponentIdentifier { get; } = TnTComponentIdentifier.NewId();

    /// <inheritdoc />
    public ElementReference Element { get; protected set; }

    /// <inheritdoc />
    public abstract string? ElementClass { get; }

    /// <inheritdoc />
    [Parameter]
    public virtual string? ElementId { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? ElementLang { get; set; }

    /// <inheritdoc />
    public abstract string? ElementStyle { get; }

    /// <inheritdoc />
    [Parameter]
    public string? ElementTitle { get; set; }

    /// <summary>
    ///     Custom identifier attribute for the component.
    /// </summary>
    internal const string _tnTCustomIdentifierAttribute = "tntid";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        // Only create a new dictionary if needed, and avoid unnecessary allocations
        Dictionary<string, object> dict;
        if (AdditionalAttributes is null) {
            dict = [];
        }
        else if (!AdditionalAttributes.ContainsKey(_tnTCustomIdentifierAttribute) || !Equals(AdditionalAttributes[_tnTCustomIdentifierAttribute], ComponentIdentifier)) {
            dict = new Dictionary<string, object>(AdditionalAttributes);
        }
        else {
            // Already set and correct, no need to update
            return;
        }
        dict[_tnTCustomIdentifierAttribute] = ComponentIdentifier;
        AdditionalAttributes = dict;
    }
}

/// <summary>
///     Obsolete compatibility alias for <see cref="NTComponentBase" />.
/// </summary>
[Obsolete("TnTComponentBase is obsolete. Use NTComponentBase instead.")]
public abstract class TnTComponentBase : NTComponentBase, ITnTComponentBase;

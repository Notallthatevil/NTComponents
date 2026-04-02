using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;
using NTComponents.Layout;

namespace NTComponents;

/// <summary>
/// Represents the body of a TnT layout component, typically used to contain the main content area.
/// </summary>
[Obsolete("Use NTBody instead. TnTBody will be removed in a future release.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class TnTBody {
    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create(base.ElementClass!)
        .AddClass("tnt-body")
        .Build();

}

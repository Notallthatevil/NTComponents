using Microsoft.AspNetCore.Components;
using NTComponents.Layout;

namespace NTComponents;

/// <summary>
///     Material 3 app layout shell for composing headers, body regions, footers, navigation rails, and nested layouts.
/// </summary>
/// <remarks>
///     Place <see cref="NTNavigationRail" /> as a direct child beside either an <see cref="NTBody" /> or another nested <see cref="NTLayout" /> to create a rail-and-content application shell.
/// </remarks>
public partial class NTLayout {

    [CascadingParameter]
    private NTLayout? ParentLayout { get; set; }

    /// <inheritdoc />
    protected override string ComponentClass => "nt-layout";

    /// <inheritdoc />
    protected override string? ComponentStateClass => ParentLayout is null ? null : "nt-layout-nested";

}

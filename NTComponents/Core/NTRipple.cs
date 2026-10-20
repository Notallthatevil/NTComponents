using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents.Core;

/// <summary>
/// Provides ripple host rendering for interactive NT components.
/// </summary>
public static class NTRipple {
    /// <summary>
    /// Renders only the ripple host. Use this when a component has its own script registration path.
    /// </summary>
    /// <param name="hostClass">The CSS class applied to the ripple host element.</param>
    /// <returns>A render fragment for the ripple host.</returns>
    public static RenderFragment RenderHost(string hostClass = _defaultHostClass) => hostClass == _defaultHostClass ? _defaultHost : CreateHost(hostClass);

    /// <summary>
    /// Renders a ripple host and a lightweight initializer script that registers ripple handlers on the host parent.
    /// </summary>
    /// <param name="hostClass">The CSS class applied to the ripple host element.</param>
    /// <returns>A render fragment for the ripple host and initializer.</returns>
    public static RenderFragment Render(string hostClass = _defaultHostClass) => hostClass == _defaultHostClass ? _defaultHostWithScript : CreateHostWithScript(hostClass);

    private const string _defaultHostClass = "nt-button-ripple-host";
    private static readonly RenderFragment _defaultHost = CreateHost(_defaultHostClass);
    private static readonly RenderFragment _defaultHostWithScript = CreateHostWithScript(_defaultHostClass);

    private static RenderFragment CreateHost(string hostClass) => builder => {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", hostClass);
        builder.AddAttribute(2, "aria-hidden", "true");
        builder.CloseElement();
    };

    private static RenderFragment CreateHostWithScript(string hostClass) {
        var host = CreateHost(hostClass);
        return builder => {
            builder.AddContent(0, host);

            builder.OpenElement(3, "script");
            builder.AddMarkupContent(4, """
            (function (script) {
                window.NTComponents?.startRippleHost?.(script);
            })(document.currentScript);
            """);
            builder.CloseElement();
        };
    }
}

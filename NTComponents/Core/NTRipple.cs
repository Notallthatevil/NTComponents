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
    public static RenderFragment RenderHost(string hostClass = "nt-button-ripple-host") {
        return builder => {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", hostClass);
            builder.AddAttribute(2, "aria-hidden", "true");
            builder.CloseElement();
        };
    }

    /// <summary>
    /// Renders a ripple host and a lightweight initializer script that registers ripple handlers on the host parent.
    /// </summary>
    /// <param name="hostClass">The CSS class applied to the ripple host element.</param>
    /// <returns>A render fragment for the ripple host and initializer.</returns>
    public static RenderFragment Render(string hostClass = "nt-button-ripple-host") {
        return builder => {
            builder.AddContent(0, RenderHost(hostClass));

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

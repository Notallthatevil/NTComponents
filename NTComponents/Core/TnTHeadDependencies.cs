using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     Meant to be placed in the head section of App.razor to include necessary dependencies for NTComponents.
/// </summary>
[ExcludeFromCodeCoverage]
public class TnTHeadDependencies : IComponent {
    private RenderHandle _renderHandle;

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle) => _renderHandle = renderHandle;

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        _renderHandle.Render(Render);
        return Task.CompletedTask;
    }

    private void Render(RenderTreeBuilder builder) {
        // <style data-tnt-theme-critical="true">html, body, #app { background-color: Canvas; color: CanvasText; }</style>
        builder.OpenElement(0, "style");
        builder.AddAttribute(1, "data-tnt-theme-critical", "true");
        builder.AddContent(2, "html, body, #app { background-color: Canvas; color: CanvasText; }");
        builder.CloseElement();

        // <script src="_content/NTComponents/theme-bootstrap.js"></script>
        builder.OpenElement(3, "script");
        builder.AddAttribute(4, "src", "_content/NTComponents/theme-bootstrap.js");
        builder.CloseElement();

        // <link rel="preconnect" href="https://fonts.googleapis.com">
        builder.OpenElement(5, "link");
        builder.AddAttribute(6, "rel", "preconnect");
        builder.AddAttribute(7, "href", "https://fonts.googleapis.com");
        builder.CloseElement();

        // <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
        builder.OpenElement(8, "link");
        builder.AddAttribute(9, "rel", "preconnect");
        builder.AddAttribute(10, "href", "https://fonts.gstatic.com");
        builder.AddAttribute(11, "crossorigin", string.Empty);
        builder.CloseElement();

        // <link href="https://fonts.googleapis.com/css2?family=Roboto:ital,wght@0,100..900;1,100..900&display=swap" rel="stylesheet">
        builder.OpenElement(12, "link");
        builder.AddAttribute(13, "href", "https://fonts.googleapis.com/css2?family=Roboto:ital,wght@0,100..900;1,100..900&display=swap");
        builder.AddAttribute(14, "rel", "stylesheet");
        builder.CloseElement();

        // <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Sharp" />
        builder.OpenElement(15, "link");
        builder.AddAttribute(16, "rel", "stylesheet");
        builder.AddAttribute(17, "href", "https://fonts.googleapis.com/css2?family=Material+Symbols+Sharp");
        builder.CloseElement();

        // <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded" />
        builder.OpenElement(18, "link");
        builder.AddAttribute(19, "rel", "stylesheet");
        builder.AddAttribute(20, "href", "https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded");
        builder.CloseElement();

        // <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined" />
        builder.OpenElement(21, "link");
        builder.AddAttribute(22, "rel", "stylesheet");
        builder.AddAttribute(23, "href", "https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined");
        builder.CloseElement();
    }
}

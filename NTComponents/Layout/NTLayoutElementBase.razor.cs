using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
/// Shared base for the NT layout shell elements.
/// </summary>
public abstract partial class NTLayoutElementBase : TnTComponentBase {
    /// <summary>
    /// Gets the child content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the root tag name override.
    /// </summary>
    [Parameter]
    public string? TagName { get; set; }

    /// <summary>
    /// Gets the service provider for resolving configured component defaults.
    /// </summary>
    [Inject]
    protected IServiceProvider? Services { get; set; }

    /// <summary>
    /// Gets the effective default options instance.
    /// </summary>
    protected NTComponentsDefaultOptions EffectiveDefaultOptions =>
        Services?.GetService<NTComponentsDefaultOptions>() ?? NTComponentsDefaultOptions.Default;

    /// <summary>
    /// Gets the resolved root tag name for the layout element.
    /// </summary>
    protected abstract string ResolvedTagName { get; }

    /// <summary>
    /// Normalizes a semantic tag name to the supported set for NT layout elements.
    /// </summary>
    protected static string NormalizeTagName(string? tagName, string defaultTagName, string fallbackTagName) {
        var resolvedTagName = !string.IsNullOrWhiteSpace(tagName)
            ? tagName
            : defaultTagName;

        var normalized = string.IsNullOrWhiteSpace(resolvedTagName)
            ? fallbackTagName
            : resolvedTagName.Trim().ToLowerInvariant();

        return normalized switch {
            "article" or "div" or "footer" or "header" or "main" or "section" => normalized,
            _ => fallbackTagName
        };
    }

    /// <summary>
    /// Resolves a nullable value parameter against a configured default.
    /// </summary>
    protected static T ResolveValue<T>(T? parameterValue, T defaultValue)
        where T : struct =>
        parameterValue ?? defaultValue;

    /// <summary>
    /// Gets additional root attributes emitted by the base Razor markup after the common attributes.
    /// </summary>
    protected virtual IReadOnlyDictionary<string, object>? RootStateAttributes => null;
}

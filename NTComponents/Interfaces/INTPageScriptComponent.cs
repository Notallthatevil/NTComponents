using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents.Interfaces;

/// <summary>
///     Represents a component that has an isolated JavaScript module
/// </summary>
public interface INTPageScriptComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TComponent> : IAsyncDisposable, IDisposable, INTComponentBase where TComponent : ComponentBase {

    /// <summary>
    ///     Gets the reference to the DotNet object associated with the component.
    /// </summary>
    DotNetObjectReference<TComponent>? DotNetObjectRef { get; }

    /// <summary>
    ///     Gets the reference to the isolated JavaScript module.
    /// </summary>
    IJSObjectReference? IsolatedJsModule { get; }

    /// <summary>
    ///     Gets the path of the JavaScript module.
    /// </summary>
    string? JsModulePath { get; }
}

/// <summary>
///     Obsolete compatibility alias for <see cref="INTPageScriptComponent{TComponent}" />.
/// </summary>
[Obsolete("ITnTPageScriptComponent is obsolete. Use INTPageScriptComponent instead.")]
public interface ITnTPageScriptComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TComponent> : INTPageScriptComponent<TComponent>, ITnTComponentBase where TComponent : ComponentBase;

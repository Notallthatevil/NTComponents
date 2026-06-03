using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using NTComponents.Ext;

namespace NTComponents;

/// <summary>
///     A component that lazily loads its child content.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="TnTLazyLoad" /> class.</remarks>
[Obsolete("TnTLazyLoad is obsolete.")]
[method: DynamicDependency(nameof(BeginLoad))]
public partial class TnTLazyLoad() {

    /// <summary>
    ///     Gets or sets the content to be lazily loaded.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    public override string? ElementClass => throw new NotSupportedException();

    /// <inheritdoc />
    public override string? ElementStyle => throw new NotSupportedException();

    /// <inheritdoc />
    public override string? JsModulePath => _jsModulePath;

    /// <summary>
    ///     Gets or sets the content to be displayed while loading.
    /// </summary>
    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    private const string _jsModulePath = "./_content/NTComponents/LazyLoader/TnTLazyLoad.razor.js";
    private bool _beginLoad;

    /// <summary>
    ///     Begins loading the child content.
    /// </summary>
    [JSInvokable]
    public void BeginLoad() {
        _beginLoad = true;
        StateHasChanged();
    }
}

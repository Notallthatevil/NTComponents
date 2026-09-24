using Microsoft.AspNetCore.Components;

namespace NTComponents;

// The renderer initializes preceding columns before this component receives its parameters.
internal sealed class NTDataGridColumnBoundary<TItem> : ComponentBase where TItem : class {
    [CascadingParameter]
    internal NTDataGrid<TItem> Owner { get; set; } = default!;

    protected override Task OnParametersSetAsync() => Owner.CompleteInitialColumnConfigurationAsync();
}

using Microsoft.AspNetCore.Components;
using NTComponents;

namespace LiveTest.Client.Pages;

/// <summary>
///     Demo page for the popover window system.
/// </summary>
public partial class Popover(INTPopoverService popoverService) {
    private readonly INTPopoverService _popoverService = popoverService;

    private async Task OpenInspectorPopoverAsync() {
        await _popoverService.OpenAsync(CreateInspectorContent(), new NTPopoverOptions {
            Description = "A secondary floating tool window.",
            InitialLeft = 360,
            InitialTop = 128,
            InstanceKey = "livetest-inspector",
            Title = "Inspector",
            Width = "320px"
        });
    }

    private async Task OpenNotesPopoverAsync() {
        await _popoverService.OpenAsync(CreateNotesContent(), new NTPopoverOptions {
            Description = "A floating notes window that can be hidden and restored.",
            InitialLeft = 40,
            InitialTop = 96,
            InstanceKey = "livetest-notes",
            MaxHeight = "448px",
            Title = "Notes",
            Width = "384px"
        });
    }

    private async Task OpenPreviewPopoverAsync() {
        await _popoverService.OpenAsync(CreatePreviewContent(), new NTPopoverOptions {
            Description = "A compact read-only preview surface.",
            InitialLeft = 660,
            InitialTop = 112,
            InstanceKey = "livetest-preview",
            MaxHeight = "384px",
            Title = "Preview",
            Width = "288px"
        });
    }

    private async Task OpenWorkspaceAsync() {
        await OpenNotesPopoverAsync();
        await OpenInspectorPopoverAsync();
        await OpenPreviewPopoverAsync();
    }

    private static RenderFragment CreateInspectorContent() {
        return builder => {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "d-flex flex-column gap-2");
            builder.OpenElement(2, "strong");
            builder.AddContent(3, "Selection");
            builder.CloseElement();
            builder.OpenElement(4, "p");
            builder.AddContent(5, "Nothing selected");
            builder.CloseElement();
            builder.OpenElement(6, "p");
            builder.AddContent(7, "Use this window for contextual tools.");
            builder.CloseElement();
            builder.CloseElement();
        };
    }

    private static RenderFragment CreateNotesContent() {
        return builder => {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "d-flex flex-column gap-2");
            builder.OpenElement(2, "p");
            builder.AddContent(3, "Drag this window, hide it, and reopen it from the launcher strip.");
            builder.CloseElement();
            builder.OpenElement(4, "ul");
            builder.OpenElement(5, "li");
            builder.AddContent(6, "Supports modeless interaction");
            builder.CloseElement();
            builder.OpenElement(7, "li");
            builder.AddContent(8, "Preserves position when restored");
            builder.CloseElement();
            builder.OpenElement(9, "li");
            builder.AddContent(10, "Works with keyboard and pointer input");
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        };
    }

    private static RenderFragment CreatePreviewContent() {
        return builder => {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "d-flex flex-column gap-2");
            builder.OpenElement(2, "strong");
            builder.AddContent(3, "Customer Summary");
            builder.CloseElement();
            builder.OpenElement(4, "p");
            builder.AddContent(5, "Status: Active");
            builder.CloseElement();
            builder.OpenElement(6, "p");
            builder.AddContent(7, "Last updated: just now");
            builder.CloseElement();
            builder.OpenElement(8, "p");
            builder.AddContent(9, "This compact popover is useful for previews and side-by-side inspection.");
            builder.CloseElement();
            builder.CloseElement();
        };
    }
}

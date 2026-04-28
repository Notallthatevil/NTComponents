using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using NTComponents;

namespace LiveTest.Client.Pages;

/// <summary>
///     Demonstrates the <see cref="NTButtonGroup" /> component within the LiveTest client app.
/// </summary>
public partial class NTButtonGroupDemo : ComponentBase {
    private static readonly IReadOnlyList<NTButtonGroupDemoItem> BaseButtonItems = new[] {
        new NTButtonGroupDemoItem {
            Key = "mail",
            Label = "Mail",
            StartIcon = MaterialIcon.Mail
        },
        new NTButtonGroupDemoItem {
            Key = "calendar",
            Label = "Calendar",
        },
        new NTButtonGroupDemoItem {
            Key = "spaces",
            Label = "Spaces",
        },
        new NTButtonGroupDemoItem {
            Key = "image1",
            StartIcon = MaterialIcon.QrCode,
            AriaLabel = "Show QR code"
        },
        new NTButtonGroupDemoItem {
            Key = "image2",
            StartIcon = MaterialIcon.Radar,
            AriaLabel = "Show radar"
        },
    };

    private IReadOnlyList<NTButtonGroupDemoItem> DisplayItems => UseImages
        ? BaseButtonItems
        : BaseButtonItems.Where(item => item.Label is not null).ToArray();

    private bool UseImages { get; set; }

    private NTButtonGroupDisplayType DisplayType { get; set; } = NTButtonGroupDisplayType.Disconnected;


    private Size ButtonSize { get; set; } = Size.Medium;

    private NTButtonVariant Variant { get; set; } = NTButtonVariant.Tonal;

    private ButtonShape Shape { get; set; } = ButtonShape.Round;

    private bool DisableRipple { get; set; }

    private bool Disabled { get; set; }

    private bool StopPropagation { get; set; } = true;

    private bool SelectionRequired { get; set; } = true;

    private string SelectedKey { get; set; } = BaseButtonItems[0].Key;

    private string SelectionMessage { get; set; } = $"Selected {BaseButtonItems[0].Label}";


    private string? SelectedKeyValue => string.IsNullOrWhiteSpace(SelectedKey) ? null : SelectedKey;

    private Task HandleSelectionChanged(NTButtonGroupItem<string> item) {
        SelectionMessage = $"Selection changed to {item.Label ?? item.Key}";
        return Task.CompletedTask;
    }

    private Task HandleSelectedKeyChanged(string? newKey) {
        SelectedKey = newKey ?? string.Empty;
        SelectionMessage = string.IsNullOrWhiteSpace(newKey)
            ? "Selection cleared"
            : $"Explicitly selected {newKey}";
        return Task.CompletedTask;
    }

    private sealed record NTButtonGroupDemoItem {
        public required string Key { get; init; }
        public string? Label { get; init; }
        public string? AriaLabel { get; init; }
        public TnTIcon? StartIcon { get; init; }
        public bool Disabled { get; init; }
        public bool IsDefaultSelected { get; init; }
    }
}

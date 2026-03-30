using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace LiveTest.Client.Components;

/// <summary>
///     Demonstrates restoring selected files when navigating between wizard steps.
/// </summary>
public partial class InputFileWizardDemo {
    private readonly Step1Model _step1 = new();
    private IReadOnlyList<IBrowserFile>? _selectedFiles;

    private void HandleSelectionChanged(IReadOnlyList<IBrowserFile> files) {
        _selectedFiles = files;
    }

    private static string FormatFileSize(long bytes) {
        if (bytes < 1024) {
            return $"{bytes} B";
        }

        if (bytes < 1_048_576) {
            return $"{bytes / 1024.0:F1} KB";
        }

        return $"{bytes / 1_048_576.0:F1} MB";
    }

    private sealed class Step1Model {
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}

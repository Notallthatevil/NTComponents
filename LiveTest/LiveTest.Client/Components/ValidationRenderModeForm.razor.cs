using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace LiveTest.Client.Components;

public partial class ValidationRenderModeForm : ComponentBase {
    private const string SeverityHigh = "High";
    private const string SeverityLow = "Low";
    private const string SeverityMedium = "Medium";

    [Parameter]
    public string FormName { get; set; } = "validation-render-mode-form";

    [Parameter]
    public string ModeName { get; set; } = "Validation";

    private ValidationRenderModeModel Model { get; set; } = new();

    private string BlurStatus { get; set; } = "No blur yet.";

    private string ModeKey => ModeName.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

    private string SubmitStatus { get; set; } = "Not submitted.";

    private Task HandleBlurAsync(FocusEventArgs args) {
        BlurStatus = "Claim title blurred.";
        return Task.CompletedTask;
    }

    private Task HandleInvalidSubmit(EditContext editContext) {
        SubmitStatus = $"Invalid: {editContext.GetValidationMessages().Count()} message(s).";
        return Task.CompletedTask;
    }

    private Task HandleSeverityChanged(string? severity) {
        Model.Severity = severity;
        return Task.CompletedTask;
    }

    private Task HandleValidSubmit(EditContext editContext) {
        SubmitStatus = "Valid submit.";
        return Task.CompletedTask;
    }

    private Task ResetAsync(MouseEventArgs args) {
        Model = new ValidationRenderModeModel();
        SubmitStatus = "Reset.";
        BlurStatus = "No blur yet.";
        return Task.CompletedTask;
    }

    private sealed class ValidationRenderModeModel {
        [Required(ErrorMessage = "Enter a claim title.")]
        public string? ClaimTitle { get; set; }

        public bool Confirmed { get; set; }

        [Required(ErrorMessage = "Choose a severity.")]
        public string? Severity { get; set; }
    }
}
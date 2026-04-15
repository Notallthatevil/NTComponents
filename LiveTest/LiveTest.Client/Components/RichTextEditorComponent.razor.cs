using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace LiveTest.Client.Components;

/// <summary>
///     Demonstrates the rich text editor with form validation.
/// </summary>
public partial class RichTextEditorComponent : ComponentBase {
    private RichTextEditorDemoModel Model { get; } = new() {
        Value = """
            # Meeting Notes

            Start with **bold**, *italic*, <u>underline</u>, <span style="color:#2563eb;">accent color</span>, or a [reference link](https://example.com).

            <div align="center">
            ## Centered Callout
            </div>

            - Agenda
              - Introductions
              - Demo walkthrough
            - Follow-up items

            > Keep the example grounded in real content.

            ```text
            npm test -- NTRichTextEditor.test.js
            ```

            | Milestone | Owner | Status |
            | :--- | :---: | ---: |
            | Editor shell | Nate | 100% |
            | Table support | Team | 75% |

            ![Architecture](https://placehold.co/640x240/png)

            <iframe src="https://example.com/embed" title="Embedded example" width="100%" height="315" loading="lazy"></iframe>
            """
    };

    private string? BlurMessage { get; set; }

    private string? SubmitMessage { get; set; }

    private Task HandleBlurAsync(FocusEventArgs args) {
        BlurMessage = "Editor blurred.";
        return Task.CompletedTask;
    }

    private Task HandleInvalidSubmit(EditContext editContext) {
        SubmitMessage = "Form is invalid.";
        return Task.CompletedTask;
    }

    private Task HandleValidSubmit(EditContext editContext) {
        SubmitMessage = "Form is valid.";
        return Task.CompletedTask;
    }

    private sealed class RichTextEditorDemoModel {
        [Required]
        public string? Value { get; set; }
    }
}

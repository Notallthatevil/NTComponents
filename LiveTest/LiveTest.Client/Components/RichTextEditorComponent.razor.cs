using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace LiveTest.Client.Components;

/// <summary>
///     Demonstrates the rich text editor with form validation.
/// </summary>
public partial class RichTextEditorComponent : ComponentBase {
    [Parameter]
    public string ModeName { get; set; } = "Editor";

    private string EditorId => $"rich-text-editor-{ModeKey}";

    private string ModeKey => ModeName.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

    private RichTextEditorDemoModel Model { get; } = new() {
        Value = InitialHtmlValue
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

    private Task Reset(MouseEventArgs args) {
        Model.Value = string.Empty;
        SubmitMessage = "Not submitted.";
        BlurMessage = null;
        return Task.CompletedTask;
    }

    private sealed class RichTextEditorDemoModel {
        [Required]
        public string? Value { get; set; }
    }

    private const string InitialHtmlValue = """
        <h1>Meeting Notes</h1>
        <p>Start with <strong>bold</strong>, <em>italic</em>, <u>underline</u>, <span style="color:#2563eb;">accent color</span>, or a <a href="https://example.com">reference link</a>.</p>
        <div style="text-align:center;"><h2>Centered Callout</h2></div>
        <ul>
            <li>Agenda<ul><li>Introductions</li><li>Demo walkthrough</li></ul></li>
            <li>Follow-up items</li>
        </ul>
        <blockquote><p>Keep the example grounded in real content.</p></blockquote>
        <pre data-language="text"><code data-language="text">npm test -- NTRichTextEditor.test.js</code></pre>
        <table data-border-color="#94a3b8" style="--nt-rich-text-table-border-color:#94a3b8;"><caption>Delivery plan</caption><thead><tr><th>Milestone</th><th>Owner</th><th>Status</th></tr></thead><tbody><tr><td>Editor shell</td><td>Nate</td><td>100%</td></tr><tr><td>Table support</td><td>Team</td><td>75%</td></tr></tbody></table>
        <img src="https://placehold.co/640x240/png" alt="Architecture" />
        <iframe src="https://example.com/embed" title="Embedded example" width="100%" height="315" loading="lazy"></iframe>
        """;
}

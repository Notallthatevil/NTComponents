using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTFileUpload_Tests : BunitContext {

    private sealed class TestModel {
        public IReadOnlyList<IBrowserFile>? Files { get; set; }
    }

    [Fact]
    public void Static_Renderer_Renders_Native_File_Input_For_Form_Post() {
        SetRendererInfo(new RendererInfo("Static", false));
        var model = new TestModel();

        var cut = RenderFileUpload(model, parameters => parameters
            .Add(p => p.ElementId, "resume")
            .Add(p => p.Label, "Resume")
            .Add(p => p.SupportingText, "Attach a PDF")
            .Add(p => p.Accept, ".pdf")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
                ["name"] = "resumeFile",
                ["required"] = true
            }));

        var input = cut.Find("input[type=file]");
        input.GetAttribute("id").Should().Be("resume");
        input.GetAttribute("name").Should().Be("resumeFile");
        input.GetAttribute("accept").Should().Be(".pdf");
        input.HasAttribute("required").Should().BeTrue();
        cut.Find(".nt-input-container").TagName.Should().Be("DIV");
        input.GetAttribute("aria-labelledby").Should().Be("resume-label");
        input.GetAttribute("aria-describedby").Should().Contain("resume-supporting");
        cut.Find("#resume-supporting").TextContent.Should().Be("Attach a PDF");
    }

    [Fact]
    public void Interactive_Renderer_Renders_Upload_Action_Only_When_Upload_Handler_Exists() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        using var withoutHandler = RenderFileUpload(configure: parameters => parameters.Add(p => p.ShowUploadButton, true));
        withoutHandler.FindAll(".nt-file-upload-action").Should().BeEmpty();

        using var withHandler = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.CompletedTask))
            .Add(p => p.ShowUploadButton, true));

        var action = withHandler.Find(".nt-file-upload-action");
        action.TextContent.Should().Be("Upload");
        action.HasAttribute("disabled").Should().BeTrue();
        action.ParentElement!.ClassList.Should().Contain("nt-input-field");
        action.ParentElement!.TagName.Should().NotBe("LABEL");
    }

    [Fact]
    public void Prefix_And_Suffix_Are_Rendered_And_Described_By_File_Input() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.PrefixText, "File")
            .Add(p => p.SuffixText, "PDF"));

        var describedBy = cut.Find("input[type=file]").GetAttribute("aria-describedby")!;
        describedBy.Should().Contain(cut.Find(".nt-input-prefix").GetAttribute("id"));
        describedBy.Should().Contain(cut.Find(".nt-input-suffix").GetAttribute("id"));
        cut.Find(".nt-input-prefix").TextContent.Should().Be("File");
        cut.Find(".nt-input-suffix").TextContent.Should().Be("PDF");
    }

    [Fact]
    public void Error_State_Renders_Error_Adornment_Instead_Of_Trailing_Icon() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.MaximumFileSize, 4)
            .Add(p => p.TrailingIcon, MaterialIcon.Info)
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.CompletedTask)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("large", "large.txt"));

        cut.Find(".nt-input-error-icon").TextContent.Should().Contain(MaterialIcon.Error.Icon);
        cut.FindAll(".nt-input-trailing").Should().BeEmpty();
    }

    [Fact]
    public void Multiple_And_Accept_Are_Applied_To_Interactive_File_Input() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.Multiple, true)
            .Add(p => p.Accept, "image/*"));

        var input = cut.Find("input[type=file]");
        input.HasAttribute("multiple").Should().BeTrue();
        input.GetAttribute("accept").Should().Be("image/*");
    }

    [Fact]
    public async Task ClearAsync_Clears_Bound_Value_And_Selection_Text() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var model = new TestModel {
            Files = [new FakeBrowserFile("a.txt", 12)]
        };

        var cut = RenderFileUpload(model);

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-input-has-value");
        cut.Find(".nt-file-upload-value").TextContent.Should().Be("a.txt");

        await cut.Instance.ClearAsync();
        cut.Render();

        model.Files.Should().BeNull();
        cut.Find(".nt-file-upload-value").TextContent.Should().Be("No file selected");
        cut.FindAll(".nt-file-upload-item").Should().BeEmpty();
    }

    [Fact]
    public void ReadOnly_Disables_File_Selection_Instead_Of_Rendering_Readonly() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters.Add(p => p.ReadOnly, true));

        var input = cut.Find("input[type=file]");
        input.HasAttribute("disabled").Should().BeTrue();
        input.HasAttribute("readonly").Should().BeFalse();
    }

    [Fact]
    public void Oversized_File_Selection_Shows_Immediate_Error_State() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var model = new TestModel();
        NTFileUploadEventArgs? errorArgs = null;

        var cut = RenderFileUpload(model, parameters => parameters
            .Add(p => p.MaximumFileSize, 4)
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.CompletedTask))
            .Add(p => p.OnFileError, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, args => errorArgs = args)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("large", "large.txt"));

        model.Files.Should().BeNull();
        cut.Find(".nt-input").ClassList.Should().NotContain("nt-invalid");
        cut.Find(".nt-input").ClassList.Should().NotContain("nt-modified");
        cut.Find("input[type=file]").GetAttribute("aria-invalid").Should().Be("true");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("large.txt is too large. Maximum file size is 4 bytes.");
        cut.Find(".nt-file-upload-value").TextContent.Should().Be("large.txt");
        cut.Find(".nt-file-upload-status").TextContent.Should().Be("Too large");
        cut.Find(".nt-file-upload-action").HasAttribute("disabled").Should().BeTrue();

        var progress = cut.Find(".nt-file-upload-progress .nt-progress");
        progress.GetAttribute("style")!.Should().Contain("--nt-progress-indicator-color:var(--tnt-color-error)");
        progress.GetAttribute("style")!.Should().Contain("--nt-progress-track-color:var(--tnt-color-error-container)");
        progress.GetAttribute("aria-valuenow").Should().Be("100");

        errorArgs.Should().NotBeNull();
        errorArgs!.Name.Should().Be("large.txt");
        errorArgs.ErrorMessage.Should().Be("large.txt is too large. Maximum file size is 4 bytes.");
    }

    [Fact]
    public void Selected_File_Displays_Status_Percentage() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.CompletedTask)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("small", "small.txt"));

        cut.Find(".nt-file-upload-status span:first-child").TextContent.Should().Be("Ready");
        cut.Find(".nt-file-upload-percent").TextContent.Should().Be("0%");
        cut.Find(".nt-file-upload-list").HasAttribute("aria-live").Should().BeFalse();
    }

    [Fact]
    public void Completed_Upload_Hides_Status_Percentage() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.CompletedTask))
            .Add(p => p.ShowUploadButton, true));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("small", "small.txt"));
        cut.Find(".nt-file-upload-action").Click();

        cut.WaitForAssertion(() => {
            cut.Find(".nt-file-upload-status").TextContent.Should().Be("Complete");
            cut.FindAll(".nt-file-upload-percent").Should().BeEmpty();
        });
    }

    private IRenderedComponent<NTFileUpload> RenderFileUpload(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTFileUpload>>? configure = null) {
        model ??= new TestModel();
        return Render<NTFileUpload>(parameters => {
            parameters
                .Add(p => p.Value, model.Files)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>?>(this, value => model.Files = value))
                .Add(p => p.ValueExpression, (Expression<Func<IReadOnlyList<IBrowserFile>?>>)(() => model.Files));
            configure?.Invoke(parameters);
        });
    }

    private sealed class FakeBrowserFile(string name, long size) : IBrowserFile {
        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public string Name { get; } = name;

        public long Size { get; } = size;

        public string ContentType { get; } = "text/plain";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) {
            if (Size > maxAllowedSize) {
                throw new IOException($"Supplied file with size {Size} bytes exceeds the maximum of {maxAllowedSize} bytes.");
            }

            return new MemoryStream(new byte[(int)Size]);
        }
    }
}

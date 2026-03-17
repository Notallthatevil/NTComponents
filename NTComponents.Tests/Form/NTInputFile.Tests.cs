using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace NTComponents.Tests.Form;

public class NTInputFile_Tests : BunitContext {

    public NTInputFile_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public async Task ProgressTemplate_Renders_Custom_Item_Content_And_Keeps_Progress_Bar() {
        // Arrange
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ProgressTemplate, details => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "custom-progress-item");
                builder.AddContent(2, $"{details.Name}|{details.Status}|{details.Size}");
                builder.CloseElement();
            }));

        await SeedProgressAsync(cut, new TestBrowserFile("Quarterly Report.pdf", 2048), "Ready to upload");

        // Act
        var template = cut.Find(".custom-progress-item");

        // Assert
        template.TextContent.Should().Be("Quarterly Report.pdf|Ready to upload|2048");
        cut.FindComponent<TnTProgressIndicator>();
        cut.Markup.Should().NotContain("nt-input-file-progress-title");
    }

    [Fact]
    public async Task Without_ProgressTemplate_Renders_Default_File_Details() {
        // Arrange
        var cut = Render<NTInputFile>();

        await SeedProgressAsync(cut, new TestBrowserFile("status.txt", 128), "Processing");

        // Assert
        cut.Find(".nt-input-file-progress-title").TextContent.Should().Be("status.txt");
        cut.Find(".nt-input-file-progress-subtitle").TextContent.Should().Be("Processing");
        cut.FindComponent<TnTProgressIndicator>();
    }

    [Fact]
    public async Task ShowProgressBar_False_Hides_Progress_Bar_And_Keeps_Item_Content() {
        // Arrange
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ShowProgressBar, false)
            .Add(component => component.ProgressTemplate, details => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "custom-progress-item");
                builder.AddContent(2, $"{details.Name}|{details.Status}");
                builder.CloseElement();
            }));

        await SeedProgressAsync(cut, new TestBrowserFile("notes.txt", 512), "Ready to upload");

        // Assert
        cut.Find(".custom-progress-item").TextContent.Should().Be("notes.txt|Ready to upload");
        cut.FindComponents<TnTProgressIndicator>().Should().BeEmpty();
    }

    [Fact]
    public async Task ShowRemoveButton_True_Removes_The_Selected_File_Row() {
        // Arrange
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ShowRemoveButton, true));

        await SeedProgressAsync(cut, [
            new TestBrowserFile("first.txt", 100),
            new TestBrowserFile("second.txt", 200)
        ], "Ready to upload");

        // Act
        cut.FindAll(".nt-input-file-progress-remove .tnt-image-button").Should().HaveCount(2);
        cut.FindAll(".nt-input-file-progress-remove .tnt-image-button")[0].Click();

        // Assert
        cut.FindAll(".nt-input-file-progress-item").Should().HaveCount(1);
        cut.Find(".nt-input-file-progress-title").TextContent.Should().Be("second.txt");
    }

    private static Task SeedProgressAsync(IRenderedComponent<NTInputFile> cut, IBrowserFile file, string status)
        => SeedProgressAsync(cut, [file], status);

    private static async Task SeedProgressAsync(IRenderedComponent<NTInputFile> cut, IReadOnlyList<IBrowserFile> files, string status) {
        var initializeMethod = typeof(NTInputFile).GetMethod("InitializeFileProgressStatesAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        initializeMethod.Should().NotBeNull();

        await cut.InvokeAsync(async () => {
            var task = (Task?)initializeMethod!.Invoke(cut.Instance, [files, status]);
            task.Should().NotBeNull();
            await task!;
        });
    }

    private sealed class TestBrowserFile : IBrowserFile {

        public TestBrowserFile(string name, long size) {
            Name = name;
            Size = size;
        }

        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public string Name { get; }

        public long Size { get; }

        public string ContentType { get; } = "application/octet-stream";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) {
            if (Size > maxAllowedSize) {
                throw new IOException("File exceeds the allowed size.");
            }

            return new MemoryStream(new byte[checked((int)Size)]);
        }
    }
}

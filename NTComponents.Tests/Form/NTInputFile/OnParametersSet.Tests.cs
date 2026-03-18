using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace NTComponents.Tests.Form;

public class OnParametersSet_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/NTInputFile.razor.js";

    public OnParametersSet_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("removeSelectedFile", _ => true).SetVoidResult();
        module.SetupVoid("restoreFileNames", _ => true).SetVoidResult();
    }

    [Fact]
    public void WithSelectedFiles_RendersAProgressRowPerFile() {
        // Arrange
        IBrowserFile[] files = [
            new TestBrowserFile("report.pdf", 1024),
            new TestBrowserFile("image.png", 2048)
        ];

        // Act
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(c => c.SelectedFiles, files)
            .Add(c => c.Multiple, true)
            .Add(c => c.MaximumFileCount, 5));

        // Assert
        cut.FindAll(".nt-input-file-progress-item").Should().HaveCount(2);
    }

    [Fact]
    public void WithSelectedFiles_RendersCorrectFileNamesInOrder() {
        // Arrange
        IBrowserFile[] files = [
            new TestBrowserFile("report.pdf", 1024),
            new TestBrowserFile("image.png", 2048)
        ];

        // Act
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(c => c.SelectedFiles, files)
            .Add(c => c.Multiple, true)
            .Add(c => c.MaximumFileCount, 5));

        // Assert
        cut.FindAll(".nt-input-file-progress-title")
            .Select(e => e.TextContent)
            .Should().Equal("report.pdf", "image.png");
    }

    [Fact]
    public void WithSelectedFiles_ShowsReadyToUploadStatus() {
        // Arrange
        IBrowserFile[] files = [new TestBrowserFile("data.csv", 512)];

        // Act
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(c => c.SelectedFiles, files));

        // Assert
        cut.Find(".nt-input-file-progress-subtitle").TextContent.Should().Be("Ready to upload");
    }

    [Fact]
    public void WithNullSelectedFiles_RendersNoProgressRows() {
        // Act
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(c => c.SelectedFiles, (IReadOnlyList<IBrowserFile>?)null));

        // Assert
        cut.FindAll(".nt-input-file-progress-item").Should().BeEmpty();
    }

    [Fact]
    public void WithEmptySelectedFiles_RendersNoProgressRows() {
        // Act
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(c => c.SelectedFiles, Array.Empty<IBrowserFile>()));

        // Assert
        cut.FindAll(".nt-input-file-progress-item").Should().BeEmpty();
    }

    [Fact]
    public void WithSelectedFiles_WhenPendingFilesAlreadyPresent_DoesNotOverwrite() {
        // Arrange – render with an initial set via SelectedFiles, which populates _pendingFiles
        IBrowserFile[] initialFiles = [new TestBrowserFile("original.txt", 100)];
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(c => c.SelectedFiles, initialFiles));

        cut.FindAll(".nt-input-file-progress-item").Should().HaveCount(1);

        // Act – supply a different SelectedFiles; _pendingFiles is non-empty so no restore should occur
        IBrowserFile[] differentFiles = [
            new TestBrowserFile("should-not-appear-1.txt", 300),
            new TestBrowserFile("should-not-appear-2.txt", 400)
        ];
        cut.Render(parameters => parameters
            .Add(c => c.SelectedFiles, differentFiles));

        // Assert – still the original file, not overwritten
        cut.FindAll(".nt-input-file-progress-item").Should().HaveCount(1);
        cut.Find(".nt-input-file-progress-title").TextContent.Should().Be("original.txt");
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

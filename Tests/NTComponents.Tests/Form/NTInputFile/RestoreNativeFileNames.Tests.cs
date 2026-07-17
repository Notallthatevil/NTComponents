using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace NTComponents.Tests.Form;

public class RestoreNativeFileNames_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/NTInputFile.razor.js";

    public RestoreNativeFileNames_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("removeSelectedFile", _ => true).SetVoidResult();
        module.SetupVoid("restoreFileNames", _ => true).SetVoidResult();
    }

    [Fact]
    public void WithSelectedFiles_InvokesRestoreFileNamesJs() {
        // Arrange
        IBrowserFile[] files = [new TestBrowserFile("file.pdf", 1024)];

        // Act
        Render<NTInputFile>(parameters => parameters
            .Add(c => c.SelectedFiles, files));

        // Assert
        JSInterop.Invocations.Should().Contain(i => i.Identifier == "restoreFileNames");
    }

    [Fact]
    public void WithSelectedFiles_PassesTwoArgumentsToJs() {
        // Arrange
        IBrowserFile[] files = [new TestBrowserFile("file.pdf", 1024)];

        // Act
        Render<NTInputFile>(parameters => parameters
            .Add(c => c.SelectedFiles, files));

        // Assert
        var invocation = JSInterop.Invocations.First(i => i.Identifier == "restoreFileNames");
        invocation.Arguments.Should().HaveCount(2);
    }

    [Fact]
    public void WithMultipleSelectedFiles_InvokesRestoreFileNamesOnce() {
        // Arrange
        IBrowserFile[] files = [
            new TestBrowserFile("first.pdf", 512),
            new TestBrowserFile("second.pdf", 1024)
        ];

        // Act
        Render<NTInputFile>(parameters => parameters
            .Add(c => c.SelectedFiles, files)
            .Add(c => c.Multiple, true)
            .Add(c => c.MaximumFileCount, 5));

        // Assert
        JSInterop.Invocations.Count(i => i.Identifier == "restoreFileNames").Should().Be(1);
    }

    [Fact]
    public void WithoutSelectedFiles_DoesNotInvokeRestoreFileNamesJs() {
        // Act
        Render<NTInputFile>();

        // Assert
        JSInterop.Invocations.Should().NotContain(i => i.Identifier == "restoreFileNames");
    }

    [Fact]
    public void WithEmptySelectedFiles_DoesNotInvokeRestoreFileNamesJs() {
        // Act
        Render<NTInputFile>(parameters => parameters
            .Add(c => c.SelectedFiles, Array.Empty<IBrowserFile>()));

        // Assert
        JSInterop.Invocations.Should().NotContain(i => i.Identifier == "restoreFileNames");
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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Text;

namespace NTComponents.Tests.Form.TnTInputFile;

public class Upload_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/TnTInputFile.razor.js";

    public Upload_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupModule("initializeFileDropZone", _ => true).SetupVoid("dispose", _ => true).SetVoidResult();
    }

    [Fact]
    public async Task BufferMode_ReadsEveryChunkAndReportsMonotonicProgress() {
        var copiedBytes = new List<byte>();
        var progress = new List<int>();
        var changedProgress = new List<int>();
        global::NTComponents.TnTInputFileEventArgs? uploaded = null;
        IReadOnlyList<global::NTComponents.TnTInputFileEventArgs>? completed = null;
        var cut = Render<global::NTComponents.TnTInputFile>(parameters => parameters
            .Add(component => component.Mode, InputFileMode.Buffer)
            .Add(component => component.BufferSize, 3u)
            .Add(component => component.OnProgressChange, EventCallback.Factory.Create<global::NTComponents.TnTInputFileEventArgs>(this, args => {
                copiedBytes.AddRange(args.Buffer.Data.AsSpan(0, args.Buffer.BytesRead).ToArray());
                progress.Add(args.ProgressPercent);
            }))
            .Add(component => component.ProgressPercentChanged, EventCallback.Factory.Create<int>(this, changedProgress.Add))
            .Add(component => component.OnFileUploaded, EventCallback.Factory.Create<global::NTComponents.TnTInputFileEventArgs>(this, args => uploaded = args))
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IEnumerable<global::NTComponents.TnTInputFileEventArgs>>(this, files => completed = files.ToArray())));

        await UploadFilesAsync(cut, new TestBrowserFile("payload.txt", "abcdefgh"));

        Encoding.UTF8.GetString(copiedBytes.ToArray()).Should().Be("abcdefgh");
        progress.Should().HaveCount(3).And.BeInAscendingOrder();
        progress[0].Should().BeGreaterThan(0);
        progress[^1].Should().Be(100);
        changedProgress.Should().Equal(progress);
        uploaded.Should().NotBeNull();
        uploaded!.Name.Should().Be("payload.txt");
        completed.Should().ContainSingle(file => file.Name == "payload.txt" && file.ErrorMessage == null);
        cut.Instance.ProgressTitle.Should().Be(global::NTComponents.TnTInputFile.ResourceLoadingCompleted);
    }

    [Fact]
    public async Task BufferMode_WhenProgressCallbackCancels_StopsBeforeNextFileAndReportsCancellation() {
        var progressNames = new List<string>();
        var uploadedNames = new List<string>();
        IReadOnlyList<global::NTComponents.TnTInputFileEventArgs>? completed = null;
        var cut = Render<global::NTComponents.TnTInputFile>(parameters => parameters
            .Add(component => component.Mode, InputFileMode.Buffer)
            .Add(component => component.BufferSize, 3u)
            .Add(component => component.MaximumFileCount, 2)
            .Add(component => component.OnProgressChange, EventCallback.Factory.Create<global::NTComponents.TnTInputFileEventArgs>(this, args => {
                progressNames.Add(args.Name);
                args.IsCancelled = true;
            }))
            .Add(component => component.OnFileUploaded, EventCallback.Factory.Create<global::NTComponents.TnTInputFileEventArgs>(this, args => uploadedNames.Add(args.Name)))
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IEnumerable<global::NTComponents.TnTInputFileEventArgs>>(this, files => completed = files.ToArray())));

        await UploadFilesAsync(cut, new TestBrowserFile("first.txt", "abcdef"), new TestBrowserFile("second.txt", "ghijkl"));

        progressNames.Should().Equal("first.txt");
        uploadedNames.Should().Equal("first.txt");
        completed.Should().ContainSingle(file => file.Name == "first.txt" && file.IsCancelled);
        completed![0].AllFiles.Select(file => file.Name).Should().Equal("first.txt", "second.txt");
        cut.Instance.ProgressTitle.Should().Be(global::NTComponents.TnTInputFile.ResourceLoadingCanceled);
    }

    [Fact]
    public async Task OversizedFile_CompletesWithExactErrorWithoutOpeningAStream() {
        var progressCalled = false;
        var uploadedCalled = false;
        IReadOnlyList<global::NTComponents.TnTInputFileEventArgs>? completed = null;
        var cut = Render<global::NTComponents.TnTInputFile>(parameters => parameters
            .Add(component => component.MaximumFileSize, 4)
            .Add(component => component.OnProgressChange, EventCallback.Factory.Create<global::NTComponents.TnTInputFileEventArgs>(this, _ => progressCalled = true))
            .Add(component => component.OnFileUploaded, EventCallback.Factory.Create<global::NTComponents.TnTInputFileEventArgs>(this, _ => uploadedCalled = true))
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IEnumerable<global::NTComponents.TnTInputFileEventArgs>>(this, files => completed = files.ToArray())));

        await UploadFilesAsync(cut, new TestBrowserFile("large.txt", "large"));

        progressCalled.Should().BeFalse();
        uploadedCalled.Should().BeFalse();
        completed.Should().ContainSingle();
        completed![0].ErrorMessage.Should().Be("The maximum size allowed is reached");
        completed[0].Stream.Should().BeNull();
        completed[0].LocalFile.Should().BeNull();
        cut.Instance.ProgressTitle.Should().Be(global::NTComponents.TnTInputFile.ResourceLoadingCompleted);
    }

    [Fact]
    public async Task TooManyFiles_ReportsCountAndDoesNotStartUpload() {
        int? exceededCount = null;
        var completedCalled = false;
        var cut = Render<global::NTComponents.TnTInputFile>(parameters => parameters
            .Add(component => component.MaximumFileCount, 1)
            .Add(component => component.OnFileCountExceeded, EventCallback.Factory.Create<int>(this, count => exceededCount = count))
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IEnumerable<global::NTComponents.TnTInputFileEventArgs>>(this, _ => completedCalled = true)));

        await UploadFilesAsync(cut, new TestBrowserFile("first.txt", "first"), new TestBrowserFile("second.txt", "second"));

        exceededCount.Should().Be(2);
        completedCalled.Should().BeFalse();
        cut.Instance.ProgressTitle.Should().BeEmpty();
        cut.Instance.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public async Task StreamMode_ExposesReadableContentAndCompletes() {
        byte[]? uploadedBytes = null;
        IReadOnlyList<global::NTComponents.TnTInputFileEventArgs>? completed = null;
        var cut = Render<global::NTComponents.TnTInputFile>(parameters => parameters
            .Add(component => component.Mode, InputFileMode.Stream)
            .Add(component => component.OnFileUploaded, EventCallback.Factory.Create<global::NTComponents.TnTInputFileEventArgs>(this, async args => {
                await using var content = new MemoryStream();
                await args.Stream!.CopyToAsync(content);
                uploadedBytes = content.ToArray();
                await args.Stream.DisposeAsync();
            }))
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IEnumerable<global::NTComponents.TnTInputFileEventArgs>>(this, files => completed = files.ToArray())));

        await UploadFilesAsync(cut, new TestBrowserFile("stream.txt", "streamed"));

        Encoding.UTF8.GetString(uploadedBytes!).Should().Be("streamed");
        completed.Should().ContainSingle(file => file.Name == "stream.txt" && file.Stream != null);
        cut.Instance.ProgressPercent.Should().Be(100);
        cut.Instance.ProgressTitle.Should().Be(global::NTComponents.TnTInputFile.ResourceLoadingCompleted);
    }

    [Fact]
    public async Task TemporaryFolderMode_WritesExactContent() {
        FileInfo? localFile = null;
        var cut = Render<global::NTComponents.TnTInputFile>(parameters => parameters
            .Add(component => component.Mode, InputFileMode.SaveToTemporaryFolder)
            .Add(component => component.BufferSize, 2u)
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IEnumerable<global::NTComponents.TnTInputFileEventArgs>>(this, files => localFile = files.Single().LocalFile)));

        try {
            await UploadFilesAsync(cut, new TestBrowserFile("saved.txt", "persisted"));

            localFile.Should().NotBeNull();
            localFile!.Exists.Should().BeTrue();
            File.ReadAllText(localFile.FullName).Should().Be("persisted");
        }
        finally {
            if (localFile?.Exists == true) {
                localFile.Delete();
            }
        }
    }

    [Fact]
    public async Task InvalidMode_ThrowsExactArgumentErrorAndDoesNotComplete() {
        var completedCalled = false;
        var cut = Render<global::NTComponents.TnTInputFile>(parameters => parameters
            .Add(component => component.Mode, (InputFileMode)int.MaxValue)
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IEnumerable<global::NTComponents.TnTInputFileEventArgs>>(this, _ => completedCalled = true)));

        Func<Task> act = () => UploadFilesAsync(cut, new TestBrowserFile("file.txt", "payload"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid Mode value.");
        completedCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ReadFailure_PropagatesExactDependencyErrorAndDoesNotComplete() {
        var completedCalled = false;
        var cut = Render<global::NTComponents.TnTInputFile>(parameters => parameters
            .Add(component => component.Mode, InputFileMode.Buffer)
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IEnumerable<global::NTComponents.TnTInputFileEventArgs>>(this, _ => completedCalled = true)));
        var change = new InputFileChangeEventArgs([new ThrowingBrowserFile()]);

        Func<Task> act = () => cut.InvokeAsync(() => cut.Instance.OnChange.InvokeAsync(change));

        await act.Should().ThrowAsync<IOException>().WithMessage("Simulated read failure.");
        completedCalled.Should().BeFalse();
        cut.Instance.ProgressTitle.Should().Be(global::NTComponents.TnTInputFile.ResourceLoadingBefore);
    }

    [Fact]
    public async Task CustomInputFileChangeHandler_RunsAfterSuccessfulDefaultUploadProcessing() {
        var callbackNames = Array.Empty<string>();
        var completed = false;
        var cut = Render<global::NTComponents.TnTInputFile>(parameters => parameters
            .Add(component => component.Mode, InputFileMode.Buffer)
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IEnumerable<global::NTComponents.TnTInputFileEventArgs>>(this, _ => completed = true))
            .Add(component => component.OnInputFileChange, EventCallback.Factory.Create<InputFileChangeEventArgs>(this, args => callbackNames = args.GetMultipleFiles().Select(file => file.Name).ToArray())));

        await UploadFilesAsync(cut, new TestBrowserFile("payload.txt", "payload"));

        completed.Should().BeTrue();
        callbackNames.Should().Equal("payload.txt");
        cut.Instance.ProgressTitle.Should().Be(global::NTComponents.TnTInputFile.ResourceLoadingCompleted);
    }

    private sealed class ThrowingBrowserFile : IBrowserFile {
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public string Name => "broken.bin";
        public long Size => 8;
        public string ContentType => "application/octet-stream";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => throw new IOException("Simulated read failure.");
    }

    private static Task UploadFilesAsync(IRenderedComponent<global::NTComponents.TnTInputFile> cut, params IBrowserFile[] files) =>
        cut.InvokeAsync(() => cut.Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs(files)));

    private sealed class TestBrowserFile(string name, string content) : IBrowserFile {
        private readonly byte[] _content = Encoding.UTF8.GetBytes(content);

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public string Name => name;
        public long Size => _content.LongLength;
        public string ContentType => "text/plain";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) {
            if (Size > maxAllowedSize) {
                throw new IOException("File exceeds the allowed size.");
            }

            return new MemoryStream(_content, writable: false);
        }
    }
}

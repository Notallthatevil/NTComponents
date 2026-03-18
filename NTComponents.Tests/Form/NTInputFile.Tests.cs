using Bunit;
using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents.Tests.Form;

public class NTInputFile_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/NTInputFile.razor.js";

    public NTInputFile_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("removeSelectedFile", _ => true).SetVoidResult();
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
    public async Task ShowRemoveButton_True_Close_Button_Removes_The_Selected_File_Row() {
        // Arrange
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ShowRemoveButton, true));

        await SeedProgressAsync(cut, [
            new TestBrowserFile("first.txt", 100),
            new TestBrowserFile("second.txt", 200)
        ], "Ready to upload");

        // Act
        var closeButtons = cut.FindAll("button[title='Remove file']");
        closeButtons.Should().HaveCount(2);
        closeButtons[0].Click();

        // Assert
        cut.FindAll(".nt-input-file-progress-item").Should().HaveCount(1);
        cut.Find(".nt-input-file-progress-title").TextContent.Should().Be("second.txt");
        cut.FindAll("button[title='Remove file']").Should().HaveCount(1);
    }

    [Fact]
    public async Task ShowRemoveButton_True_With_Multiple_Files_Removes_The_Clicked_File() {
        // Arrange
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ShowRemoveButton, true)
            .Add(component => component.Multiple, true)
            .Add(component => component.MaximumFileCount, 3));

        await SeedProgressAsync(cut, [
            new TestBrowserFile("first.txt", 100),
            new TestBrowserFile("second.txt", 200),
            new TestBrowserFile("third.txt", 300)
        ], "Ready to upload");

        // Act
        var closeButtons = cut.FindAll("button[title='Remove file']");
        closeButtons.Should().HaveCount(3);
        closeButtons[1].Click();

        // Assert
        var remainingTitles = cut.FindAll(".nt-input-file-progress-title")
            .Select(element => element.TextContent)
            .ToArray();

        remainingTitles.Should().Equal("first.txt", "third.txt");
        cut.FindAll("button[title='Remove file']").Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveFileAsync_Synchronizes_The_Native_File_Input_With_The_Removed_Index() {
        // Arrange
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ShowRemoveButton, true)
            .Add(component => component.Multiple, true)
            .Add(component => component.MaximumFileCount, 3));

        await SeedSelectionAsync(cut, [
            new TestBrowserFile("first.txt", 100),
            new TestBrowserFile("second.txt", 200),
            new TestBrowserFile("third.txt", 300)
        ], "Ready to upload");

        // Act
        await cut.InvokeAsync(() => cut.Instance.RemoveFileAsync(1));

        // Assert
        var invocation = JSInterop.Invocations.LastOrDefault(i => i.Identifier == "removeSelectedFile");
        invocation.Should().NotBeNull();
        invocation!.Arguments.Should().HaveCount(2);
        invocation.Arguments[1].Should().Be(1);
    }

    [Fact]
    public async Task RemoveButton_Still_Works_When_OnSelectionChanged_Updates_Parent_State() {
        // Arrange
        var host = Render<SelectionChangedHost>();
        var inputFile = host.FindComponent<NTInputFile>();

        await SeedSelectionAsync(inputFile, [
            new TestBrowserFile("first.txt", 100),
            new TestBrowserFile("second.txt", 200),
            new TestBrowserFile("third.txt", 300)
        ], "Ready to upload");

        // Act
        host.FindAll("button[title='Remove file']").Should().HaveCount(3);
        host.FindAll("button[title='Remove file']")[1].Click();

        // Assert
        var remainingTitles = host.FindAll(".nt-input-file-progress-title")
            .Select(element => element.TextContent)
            .ToArray();

        remainingTitles.Should().Equal("first.txt", "third.txt");
        host.Find("#selected-count").TextContent.Should().Be("2");
        host.Find("#selected-names").TextContent.Should().Be("first.txt|third.txt");
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

    private static async Task SeedSelectionAsync(IRenderedComponent<NTInputFile> cut, IReadOnlyList<IBrowserFile> files, string status) {
        var pendingFilesField = typeof(NTInputFile).GetField("_pendingFiles", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        pendingFilesField.Should().NotBeNull();

        var pendingFiles = pendingFilesField!.GetValue(cut.Instance).Should().BeAssignableTo<List<IBrowserFile>>().Subject;
        pendingFiles.Clear();
        pendingFiles.AddRange(files);

        await SeedProgressAsync(cut, files, status);
    }

    private sealed class SelectionChangedHost : ComponentBase {
        private IReadOnlyList<IBrowserFile> _selectedFiles = Array.Empty<IBrowserFile>();

        private Task HandleSelectionChangedAsync(IReadOnlyList<IBrowserFile> files) {
            _selectedFiles = files.ToArray();
            StateHasChanged();
            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTInputFile>(0);
            builder.AddAttribute(1, nameof(NTInputFile.ShowRemoveButton), true);
            builder.AddAttribute(2, nameof(NTInputFile.Multiple), true);
            builder.AddAttribute(3, nameof(NTInputFile.MaximumFileCount), 3);
            builder.AddAttribute(4, nameof(NTInputFile.OnSelectionChanged), EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, HandleSelectionChangedAsync));
            builder.CloseComponent();

            builder.OpenElement(10, "div");
            builder.AddAttribute(11, "id", "selected-count");
            builder.AddContent(12, _selectedFiles.Count);
            builder.CloseElement();

            builder.OpenElement(20, "div");
            builder.AddAttribute(21, "id", "selected-names");
            builder.AddContent(22, string.Join("|", _selectedFiles.Select(file => file.Name)));
            builder.CloseElement();
        }
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

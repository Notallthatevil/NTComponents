using Bunit;
using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Core;

namespace NTComponents.Tests.Form;

public class NTInputFile_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/NTInputFile.razor.js";

    public NTInputFile_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        NTComponents.Tests.TestingUtility.TestingUtility.SetupRippleEffectModule(this);

        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("removeSelectedFile", _ => true).SetVoidResult();

        var tooltipModule = JSInterop.SetupModule("./_content/NTComponents/Tooltip/TnTTooltip.razor.js");
        tooltipModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        tooltipModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        tooltipModule.SetupVoid("onDispose", _ => true).SetVoidResult();
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
    public async Task ShowRemoveButton_True_With_Duplicate_File_Names_Removes_The_Clicked_File() {
        // Arrange
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ShowRemoveButton, true)
            .Add(component => component.Multiple, true)
            .Add(component => component.MaximumFileCount, 3)
            .Add(component => component.ProgressTemplate, details => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "file-row");
                builder.AddContent(2, $"{details.Name}|{details.Size}");
                builder.CloseElement();
            }));

        await SeedProgressAsync(cut, [
            new TestBrowserFile("duplicate.txt", 100),
            new TestBrowserFile("duplicate.txt", 200),
            new TestBrowserFile("third.txt", 300)
        ], "Ready to upload");

        // Act
        var closeButtons = cut.FindAll("button[title='Remove file']");
        closeButtons.Should().HaveCount(3);
        closeButtons[1].Click();

        // Assert
        var remainingRows = cut.FindAll(".file-row")
            .Select(element => element.TextContent)
            .ToArray();

        remainingRows.Should().Equal("duplicate.txt|100", "third.txt|300");
        cut.FindAll("button[title='Remove file']").Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveFileAsync_Synchronizes_The_Native_File_Input_With_The_Removed_Index() {
        // Arrange
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ShowRemoveButton, true)
            .Add(component => component.Multiple, true)
            .Add(component => component.MaximumFileCount, 3));

        var files = new[] {
            new TestBrowserFile("first.txt", 100),
            new TestBrowserFile("second.txt", 200),
            new TestBrowserFile("third.txt", 300)
        };

        await SeedSelectionAsync(cut, files, "Ready to upload");

        // Act
        await cut.InvokeAsync(() => cut.Instance.RemoveFileAsync(files[1]));

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

    [Fact]
    public void Selecting_More_Than_MaximumFileCount_Reports_Count_And_Keeps_Selection_Empty() {
        int? exceededCount = null;
        IReadOnlyList<IBrowserFile>? selectedFiles = null;
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.MaximumFileCount, 2)
            .Add(component => component.OnFileCountExceeded, EventCallback.Factory.Create<int>(this, count => exceededCount = count))
            .Add(component => component.OnSelectionChanged, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, files => selectedFiles = files)));

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("first", "first.txt"),
            InputFileContent.CreateFromText("second", "second.txt"),
            InputFileContent.CreateFromText("third", "third.txt"));

        exceededCount.Should().Be(3);
        selectedFiles.Should().BeNull();
        cut.FindAll(".nt-input-file-progress-item").Should().BeEmpty();
    }

    [Fact]
    public void Upload_Without_PerFile_Handler_Reports_No_Handler_And_Preserves_Selection_When_Configured() {
        IReadOnlyList<IBrowserFile>? uploadSelection = null;
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ClearSelectionAfterUpload, false)
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, files => uploadSelection = files)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("notes", "notes.txt"));
        cut.Find(".nt-input-file-upload-button button").Click();

        uploadSelection.Should().ContainSingle(file => file.Name == "notes.txt");
        cut.Find(".nt-input-file-progress-title").TextContent.Should().Be("notes.txt");
        cut.Find(".nt-input-file-progress-subtitle").TextContent.Should().Be("No upload handler");
    }

    [Fact]
    public void Oversized_Upload_Reports_File_Error_And_Does_Not_Invoke_PerFile_Handler() {
        NTInputFileEventArgs? fileError = null;
        var uploadHandlerCalled = false;
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ClearSelectionAfterUpload, false)
            .Add(component => component.MaximumFileSize, 4)
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask))
            .Add(component => component.OnUploadFile, EventCallback.Factory.Create<NTInputFileEventArgs>(this, _ => uploadHandlerCalled = true))
            .Add(component => component.OnFileError, EventCallback.Factory.Create<NTInputFileEventArgs>(this, args => fileError = args)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("large", "large.txt"));
        cut.Find(".nt-input-file-upload-button button").Click();

        uploadHandlerCalled.Should().BeFalse();
        fileError.Should().NotBeNull();
        fileError!.Name.Should().Be("large.txt");
        fileError.ErrorMessage.Should().Be("The maximum size allowed is reached");
        cut.Find(".nt-input-file-progress-subtitle").TextContent.Should().Be("The maximum size allowed is reached");
    }

    [Fact]
    public void Successful_Upload_Exposes_Readable_Stream_And_Completes_With_File_Details() {
        byte[]? uploadedBytes = null;
        IReadOnlyList<NTInputFileEventArgs>? completedFiles = null;
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ClearSelectionAfterUpload, false)
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask))
            .Add(component => component.OnUploadFile, EventCallback.Factory.Create<NTInputFileEventArgs>(this, async args => {
                using var content = new MemoryStream();
                await args.Stream!.CopyToAsync(content);
                uploadedBytes = content.ToArray();
            }))
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IReadOnlyList<NTInputFileEventArgs>>(this, files => completedFiles = files)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("payload", "payload.txt"));
        cut.Find(".nt-input-file-upload-button button").Click();

        System.Text.Encoding.UTF8.GetString(uploadedBytes!).Should().Be("payload");
        completedFiles.Should().ContainSingle(file => file.Name == "payload.txt" && file.ErrorMessage == null);
        cut.Find(".nt-input-file-progress-subtitle").TextContent.Should().Be("Completed");
    }

    [Fact]
    public void Successful_Upload_Clears_Selection_By_Default() {
        var uploadCount = 0;
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask))
            .Add(component => component.OnUploadFile, EventCallback.Factory.Create<NTInputFileEventArgs>(this, async args => {
                uploadCount++;
                await args.Stream!.CopyToAsync(Stream.Null);
            })));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("payload", "payload.txt"));
        cut.Find(".nt-input-file-upload-button button").Click();

        uploadCount.Should().Be(1);
        cut.FindAll(".nt-input-file-progress-item").Should().BeEmpty();
        cut.Find(".nt-input-file-upload-button button").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Cancelled_Upload_Stops_Before_Subsequent_Files_And_Reports_Cancelled_Item() {
        var handledNames = new List<string>();
        IReadOnlyList<NTInputFileEventArgs>? completedFiles = null;
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ClearSelectionAfterUpload, false)
            .Add(component => component.Multiple, true)
            .Add(component => component.MaximumFileCount, 2)
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask))
            .Add(component => component.OnUploadFile, EventCallback.Factory.Create<NTInputFileEventArgs>(this, args => {
                handledNames.Add(args.Name);
                args.IsCancelled = true;
            }))
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IReadOnlyList<NTInputFileEventArgs>>(this, files => completedFiles = files)));

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("first", "first.txt"),
            InputFileContent.CreateFromText("second", "second.txt"));
        cut.Find(".nt-input-file-upload-button button").Click();

        handledNames.Should().Equal("first.txt");
        completedFiles.Should().ContainSingle(file => file.Name == "first.txt" && file.IsCancelled);
        cut.FindAll(".nt-input-file-progress-subtitle").Select(element => element.TextContent).Should().Equal("Canceled", "Ready to upload");
    }

    [Fact]
    public void Interactive_Input_Applies_Explicit_File_Selection_Attributes() {
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.Accept, ".pdf")
            .Add(component => component.AutoFocus, true)
            .Add(component => component.Disabled, true)
            .Add(component => component.ElementName, "documents")
            .Add(component => component.MaximumFileCount, 2)
            .Add(component => component.ReadOnly, true)
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object> {
                ["data-upload"] = "documents",
                ["name"] = "ignored-name"
            }));

        var input = cut.Find("input[type=file]");
        input.GetAttribute("accept").Should().Be(".pdf");
        input.GetAttribute("name").Should().Be("documents");
        input.GetAttribute("data-upload").Should().Be("documents");
        input.HasAttribute("multiple").Should().BeTrue();
        input.HasAttribute("autofocus").Should().BeTrue();
        input.HasAttribute("disabled").Should().BeTrue();
        input.HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void Static_Input_Uses_The_Form_Name_Fallback_Without_Overwriting_An_Explicit_Name() {
        SetRendererInfo(new RendererInfo("Static", false));
        using var fallback = Render<NTInputFile>();
        using var explicitName = Render<NTInputFile>(parameters => parameters
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object> { ["name"] = "attachments" }));

        fallback.Find("input[type=file]").GetAttribute("name").Should().Be("file");
        explicitName.Find("input[type=file]").GetAttribute("name").Should().Be("attachments");
    }

    [Theory]
    [InlineData(Size.Smallest, "32px", "12px", "32px / 100%")]
    [InlineData(Size.Small, "40px", "16px", "40px / 100%")]
    [InlineData(Size.Medium, "56px", "24px", "56px / 100%")]
    [InlineData(Size.Large, "96px", "48px", "96px / 100%")]
    [InlineData(Size.Largest, "136px", "64px", "136px / 100%")]
    [InlineData((Size)999, "40px", "16px", "40px / 100%")]
    public void Input_Button_Size_Controls_The_Public_Selector_Dimensions(Size size, string height, string padding, string radius) {
        var cut = Render<NTInputFile>(parameters => parameters.Add(component => component.InputButtonSize, size));

        var style = cut.Find(".nt-input-file-container").GetAttribute("style");
        style.Should().Contain($"--nt-input-file-selector-height:{height}");
        style.Should().Contain($"--nt-input-file-selector-padding-x:{padding}");
        style.Should().Contain($"--nt-input-file-selector-radius:{radius}");
    }

    [Theory]
    [InlineData(FormAppearance.Outlined, "tnt-form-outlined", null, "tnt-size-s")]
    [InlineData(FormAppearance.Filled, "tnt-form-filled", null, "tnt-size-s")]
    [InlineData(FormAppearance.OutlinedCompact, "tnt-form-outlined", "tnt-form-compact", "tnt-size-xs")]
    [InlineData(FormAppearance.FilledCompact, "tnt-form-filled", "tnt-form-compact", "tnt-size-xs")]
    [InlineData(FormAppearance.OutlinedXS, "tnt-form-outlined", "tnt-form-xs", "tnt-size-xs")]
    [InlineData(FormAppearance.FilledXS, "tnt-form-filled", "tnt-form-xs", "tnt-size-xs")]
    public void Appearance_Controls_The_Field_And_Default_Upload_Button_Size(FormAppearance appearance, string appearanceClass, string? densityClass, string buttonSizeClass) {
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.Appearance, appearance)
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask)));

        var field = cut.Find("label");
        field.ClassList.Should().Contain(appearanceClass);
        if (densityClass is null) {
            field.ClassList.Should().NotContain("tnt-form-compact");
            field.ClassList.Should().NotContain("tnt-form-xs");
        }
        else {
            field.ClassList.Should().Contain(densityClass);
        }
        cut.Find(".nt-input-file-upload-button button").ClassList.Should().Contain(buttonSizeClass);
    }

    [Fact]
    public void Explicit_Upload_Button_Size_Overrides_The_Appearance_Default() {
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.Appearance, FormAppearance.OutlinedCompact)
            .Add(component => component.UploadButtonSize, Size.Large)
            .Add(component => component.UploadButtonBackgroundColor, TnTColor.Secondary)
            .Add(component => component.UploadButtonTextColor, TnTColor.Error)
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask)));

        cut.Find(".nt-input-file-upload-button button").ClassList.Should().Contain("tnt-size-l");
        var style = cut.Find(".nt-input-file-container").GetAttribute("style");
        style.Should().Contain("--nt-input-file-action-bg-color:var(--tnt-color-secondary)");
        style.Should().Contain("--nt-input-file-action-fg-color:var(--tnt-color-error)");
    }

    [Theory]
    [InlineData(true, true, false, false, true, true)]
    [InlineData(false, false, true, true, true, true)]
    [InlineData(false, false, false, false, false, false)]
    public void Parent_And_Local_States_Combine_Into_The_File_Input_Contract(bool parentDisabled, bool parentReadOnly, bool localDisabled, bool localReadOnly, bool expectedDisabled, bool expectedReadOnly) {
        var form = new TestForm(parentDisabled, parentReadOnly);
        var cut = Render<CascadingValue<ITnTForm>>(parameters => parameters
            .Add(component => component.Value, form)
            .Add(component => component.IsFixed, true)
            .Add(component => component.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<NTInputFile>(0);
                builder.AddAttribute(1, nameof(NTInputFile.Disabled), localDisabled);
                builder.AddAttribute(2, nameof(NTInputFile.ReadOnly), localReadOnly);
                builder.CloseComponent();
            })));

        var input = cut.Find("input[type=file]");
        input.HasAttribute("disabled").Should().Be(expectedDisabled);
        input.HasAttribute("readonly").Should().Be(expectedReadOnly);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Disabled_Or_ReadOnly_Selection_Cannot_Be_Removed(bool disabled, bool readOnly) {
        IReadOnlyList<IBrowserFile>? selected = null;
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.MaximumFileCount, 2)
            .Add(component => component.Multiple, true)
            .Add(component => component.ShowRemoveButton, true)
            .Add(component => component.OnSelectionChanged, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, files => selected = files)));
        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("first", "first.txt"),
            InputFileContent.CreateFromText("second", "second.txt"));
        var removeInvocationCount = JSInterop.Invocations.Count(invocation => invocation.Identifier == "removeSelectedFile");

        cut.Render(parameters => parameters
            .Add(component => component.Disabled, disabled)
            .Add(component => component.MaximumFileCount, 2)
            .Add(component => component.Multiple, true)
            .Add(component => component.ReadOnly, readOnly)
            .Add(component => component.ShowRemoveButton, true));
        await cut.InvokeAsync(() => cut.Instance.RemoveFileAsync(selected![0]));

        cut.FindAll(".nt-input-file-progress-item").Should().HaveCount(2);
        cut.FindAll("button[title='Remove file']").Should().OnlyContain(button => button.HasAttribute("disabled"));
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "removeSelectedFile").Should().Be(removeInvocationCount);
    }

    [Fact]
    public async Task Removing_A_File_Outside_The_Current_Selection_Is_A_No_Op() {
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.MaximumFileCount, 2)
            .Add(component => component.Multiple, true)
            .Add(component => component.ShowRemoveButton, true));
        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("first", "first.txt"),
            InputFileContent.CreateFromText("second", "second.txt"));
        var removeInvocationCount = JSInterop.Invocations.Count(invocation => invocation.Identifier == "removeSelectedFile");

        await cut.InvokeAsync(() => cut.Instance.RemoveFileAsync(new TestBrowserFile("other.txt", 1)));

        cut.FindAll(".nt-input-file-progress-title").Select(element => element.TextContent).Should().Equal("first.txt", "second.txt");
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "removeSelectedFile").Should().Be(removeInvocationCount);
    }

    [Fact]
    public async Task ClearAsync_Removes_The_Selection_And_Disables_The_Upload_Action() {
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask)));
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("payload", "clear.txt"));

        await cut.InvokeAsync(cut.Instance.ClearAsync);

        cut.FindAll(".nt-input-file-progress-item").Should().BeEmpty();
        cut.Find(".nt-input-file-upload-button button").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Synchronous_Stream_Reads_Emit_Progress_And_End_In_Completed_State() {
        var progress = new List<(int Percent, string Title)>();
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ClearSelectionAfterUpload, false)
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask))
            .Add(component => component.OnUploadFile, EventCallback.Factory.Create<NTInputFileEventArgs>(this, args => {
                var buffer = new byte[4];
                while (args.Stream!.Read(buffer, 0, buffer.Length) > 0) { }
                args.Stream.ReadByte().Should().Be(-1);
            }))
            .Add(component => component.OnProgressChanged, EventCallback.Factory.Create<NTInputFileEventArgs>(this, args => progress.Add((args.ProgressPercent, args.ProgressTitle)))));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("0123456789", "sync.txt"));
        cut.Find(".nt-input-file-upload-button button").Click();

        progress.Should().Contain(entry => entry.Percent == 40 && entry.Title.Contains("Uploading"));
        progress.Count(entry => entry.Percent == 100 && entry.Title.Contains("Processing")).Should().Be(1);
        cut.Find(".nt-input-file-progress-subtitle").TextContent.Should().Be("Completed");
    }

    [Fact]
    public void Empty_File_Stream_Completes_Without_Dividing_By_Zero() {
        IReadOnlyList<NTInputFileEventArgs>? completed = null;
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ClearSelectionAfterUpload, false)
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask))
            .Add(component => component.OnUploadFile, EventCallback.Factory.Create<NTInputFileEventArgs>(this, args => {
                args.Stream!.ReadByte().Should().Be(-1);
            }))
            .Add(component => component.OnCompleted, EventCallback.Factory.Create<IReadOnlyList<NTInputFileEventArgs>>(this, files => completed = files)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(string.Empty, "empty.txt"));
        cut.Find(".nt-input-file-upload-button button").Click();

        completed.Should().ContainSingle(file => file.Name == "empty.txt" && file.ErrorMessage == null);
        cut.Find(".nt-input-file-progress-subtitle").TextContent.Should().Be("Completed");
    }

    [Fact]
    public void Upload_Stream_Is_Disposed_When_The_Per_File_Handler_Returns() {
        Stream? callbackStream = null;
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ClearSelectionAfterUpload, false)
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask))
            .Add(component => component.OnUploadFile, EventCallback.Factory.Create<NTInputFileEventArgs>(this, args => callbackStream = args.Stream)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("payload", "dispose.txt"));
        cut.Find(".nt-input-file-upload-button button").Click();

        callbackStream.Should().NotBeNull();
        callbackStream!.CanRead.Should().BeFalse();
        var read = () => callbackStream.ReadByte();
        read.Should().Throw<ObjectDisposedException>();
        cut.Find(".nt-input-file-progress-subtitle").TextContent.Should().Be("Partially processed");
    }

    [Fact]
    public async Task DisposeAsync_Disposes_An_InFlight_Upload_Stream() {
        var handlerStarted = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.ClearSelectionAfterUpload, false)
            .Add(component => component.OnUploadButtonClick, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, _ => Task.CompletedTask))
            .Add(component => component.OnUploadFile, EventCallback.Factory.Create<NTInputFileEventArgs>(this, async args => {
                handlerStarted.SetResult(args.Stream!);
                await releaseHandler.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
            })));
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("payload", "active.txt"));

        var uploadTask = cut.Find(".nt-input-file-upload-button button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        var stream = await handlerStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        await cut.Instance.DisposeAsync();

        stream.CanRead.Should().BeFalse();
        releaseHandler.SetResult();
        await uploadTask;
    }

    [Fact]
    public void Optional_Field_Content_Renders_At_Its_Documented_Locations() {
        var cut = Render<NTInputFile>(parameters => parameters
            .Add(component => component.StartIcon, MaterialIcon.AttachFile)
            .Add(component => component.EndIcon, MaterialIcon.Upload)
            .Add(component => component.Label, "Documents")
            .Add(component => component.SupportingText, "PDF files only")
            .Add(component => component.Tooltip, builder => {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "upload-help");
                builder.AddContent(2, "Choose a document from this device");
                builder.CloseElement();
            }));

        cut.Find(".tnt-start-icon").TextContent.Should().Contain(MaterialIcon.AttachFile.Icon);
        cut.Find(".tnt-label").TextContent.Should().Be("Documents");
        cut.Find(".tnt-end-icon").TextContent.Should().Contain(MaterialIcon.Upload.Icon);
        cut.Find(".tnt-supporting-text").TextContent.Should().Be("PDF files only");
        cut.Find(".upload-help").TextContent.Should().Be("Choose a document from this device");
    }

    [Fact]
    public void WithoutParentForm_DefaultAppearance_UsesOutlinedCompactFallback() {
        // Arrange & Act
        var cut = Render<NTInputFile>();

        // Assert
        cut.Markup.Should().Contain("tnt-form-outlined");
        cut.Markup.Should().Contain("tnt-form-compact");
    }

    [Fact]
    public void WithoutParentForm_ConfiguredDefaultAppearance_IsApplied() {
        // Arrange
        using var context = CreateIsolatedContext(services => services.AddSingleton(new NTComponentsDefaultOptions {
            DefaultFormAppearance = FormAppearance.FilledXS
        }));

        // Act
        var cut = context.Render<NTInputFile>();

        // Assert
        cut.Markup.Should().Contain("tnt-form-filled");
        cut.Markup.Should().Contain("tnt-form-xs");
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

    private static global::Bunit.BunitContext CreateIsolatedContext(Action<IServiceCollection>? configureServices = null) {
        var context = new global::Bunit.BunitContext();
        configureServices?.Invoke(context.Services);
        context.SetRendererInfo(new RendererInfo("WebAssembly", true));

        var module = context.JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("removeSelectedFile", _ => true).SetVoidResult();

        return context;
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

    private sealed record TestForm(bool Disabled, bool ReadOnly) : ITnTForm {
        public FormAppearance Appearance => FormAppearance.Outlined;
    }
}

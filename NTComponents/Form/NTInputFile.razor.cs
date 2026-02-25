using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     A staged file-upload component that separates file selection from upload processing.
/// </summary>
public partial class NTInputFile : IAsyncDisposable {

    /// <summary>
    ///     The accepted MIME types or extensions, for example ".pdf,.docx".
    /// </summary>
    [Parameter]
    public string? Accept { get; set; }

    /// <summary>
    ///     Clears selected files after a successful upload run.
    /// </summary>
    [Parameter]
    public bool ClearSelectionAfterUpload { get; set; } = true;

    /// <summary>
    ///     Gets or sets the appearance of the input when not inherited from a parent form.
    /// </summary>
    [Parameter]
    public FormAppearance Appearance { get; set; }

    /// <summary>
    ///     Gets or sets whether the input should automatically receive focus.
    /// </summary>
    [Parameter]
    public bool? AutoFocus { get; set; }

    /// <summary>
    ///     Gets or sets the background color of the input.
    /// </summary>
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceContainerHighest;

    /// <summary>
    ///     Gets or sets the id attribute for the input element.
    /// </summary>
    [Parameter]
    public string? ElementId { get; set; }

    /// <summary>
    ///     Gets or sets the language attribute for the input element.
    /// </summary>
    [Parameter]
    public string? ElementLang { get; set; }

    /// <summary>
    ///     Gets or sets the name attribute for the input element.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <summary>
    ///     Gets or sets the title attribute for the input element.
    /// </summary>
    [Parameter]
    public string? ElementTitle { get; set; }

    /// <summary>
    ///     Gets or sets the error color for the input.
    /// </summary>
    [Parameter]
    public TnTColor ErrorColor { get; set; } = TnTColor.Error;

    /// <summary>
    ///     Indicates whether the picker is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     The display label shown next to the file input.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Maximum number of files allowed in a selection.
    /// </summary>
    [Parameter]
    public int MaximumFileCount { get; set; } = 1;

    /// <summary>
    ///     Maximum allowed size per file in bytes.
    /// </summary>
    [Parameter]
    public long MaximumFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    ///     Enables selecting multiple files.
    /// </summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>
    ///     Raised after all files in the current upload run have completed.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<NTInputFileEventArgs>> OnCompleted { get; set; }

    /// <summary>
    ///     Raised when selected file count exceeds <see cref="MaximumFileCount" />.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnFileCountExceeded { get; set; }

    /// <summary>
    ///     Raised when a file fails validation or processing.
    /// </summary>
    [Parameter]
    public EventCallback<NTInputFileEventArgs> OnFileError { get; set; }

    /// <summary>
    ///     Raised as tracked progress changes while a file stream is read by server-side code.
    /// </summary>
    [Parameter]
    public EventCallback<NTInputFileEventArgs> OnProgressChanged { get; set; }

    /// <summary>
    ///     Raised when users select files.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<IBrowserFile>> OnSelectionChanged { get; set; }

    /// <summary>
    ///     Raised when upload button is clicked with current selected files.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<IBrowserFile>> OnUploadButtonClick { get; set; }

    /// <summary>
    ///     Raised per file when upload processing begins. Consumer must read <see cref="NTInputFileEventArgs.Stream" /> to drive progress.
    /// </summary>
    [Parameter]
    public EventCallback<NTInputFileEventArgs> OnUploadFile { get; set; }

    /// <summary>
    ///     Gets or sets the color used for content on the file selector button.
    /// </summary>
    [Parameter]
    public TnTColor OnTintColor { get; set; } = TnTColor.OnPrimary;

    /// <summary>
    ///     Indicates the component is read-only.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     Shows per-file progress rows.
    /// </summary>
    [Parameter]
    public bool ShowProgress { get; set; } = true;

    /// <summary>
    ///     Supporting text below the control.
    /// </summary>
    [Parameter]
    public string? SupportingText { get; set; }

    /// <summary>
    ///     Gets or sets the text color for the input.
    /// </summary>
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnSurface;

    /// <summary>
    ///     Gets or sets the tint color for the input and file button.
    /// </summary>
    [Parameter]
    public TnTColor TintColor { get; set; } = TnTColor.Primary;

    /// <summary>
    ///     Gets or sets the native file-selector button size.
    /// </summary>
    [Parameter]
    public Size InputButtonSize { get; set; } = Size.Small;

    /// <summary>
    ///     Gets or sets the upload button size.
    /// </summary>
    [Parameter]
    public Size UploadButtonSize { get; set; } = Size.Small;

    /// <summary>
    ///     The upload button text.
    /// </summary>
    [Parameter]
    public string UploadButtonText { get; set; } = "Upload";

    /// <summary>
    ///     Additional attributes for the underlying input element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private const string ResourceReadyToUpload = "Ready to upload";
    private const string ResourceUploadingInProgress = "Uploading {0}/{1} - {2}";
    private const string ResourceProcessingInProgress = "Processing {0}/{1} - {2}";
    private const string ResourceLoadingCanceled = "Canceled";
    private const string ResourceLoadingCompleted = "Completed";
    private const string ResourceLoadingPartiallyProcessed = "Partially processed";
    private const string ResourceNoUploadHandler = "No upload handler";
    private const string MaxSizeErrorMessage = "The maximum size allowed is reached";

    private readonly List<FileProgressState> _fileProgressStates = [];
    private readonly List<Stream> _ownedStreams = [];
    private readonly List<IBrowserFile> _pendingFiles = [];
    private bool _isUploading;
    private int _progressPercent;
    private string _progressTitle = string.Empty;

    private string ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes?.ToDictionary())
        .AddClass("tnt-input")
        .AddClass(GetAppearanceClass(_tntForm, Appearance))
        .AddDisabled(FieldDisabled || _isUploading)
        .Build();

    private string? ElementStyle => CssStyleBuilder.Create()
        .AddVariable("tnt-input-on-tint-color", OnTintColor.ToCssTnTColorVariable())
        .AddVariable("tnt-input-tint-color", TintColor.ToCssTnTColorVariable())
        .AddVariable("tnt-input-background-color", BackgroundColor.ToCssTnTColorVariable())
        .AddVariable("tnt-input-text-color", TextColor.ToCssTnTColorVariable())
        .AddVariable("tnt-input-error-color", ErrorColor.ToCssTnTColorVariable())
        .AddVariable("nt-input-file-selector-height", GetSelectorButtonHeight(InputButtonSize))
        .AddVariable("nt-input-file-selector-padding-x", GetSelectorButtonPaddingX(InputButtonSize))
        .AddVariable("nt-input-file-selector-radius", GetSelectorButtonBorderRadius(InputButtonSize))
        .Build();

    private bool FieldDisabled => _tntForm?.Disabled == true || Disabled;

    private bool FieldReadonly => _tntForm?.ReadOnly == true || ReadOnly;

    [CascadingParameter]
    private ITnTForm? _tntForm { get; set; }

    private IReadOnlyDictionary<string, object> InputAttributes => BuildInputAttributes(useSsrNameFallback: false);

    private IReadOnlyDictionary<string, object> StaticInputAttributes => BuildInputAttributes(useSsrNameFallback: true);

    private IReadOnlyDictionary<string, object> BuildInputAttributes(bool useSsrNameFallback) {
        var attributes = AdditionalAttributes?.ToDictionary() ?? new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(Accept)) {
            attributes["accept"] = Accept;
        }

        if (Multiple || MaximumFileCount > 1) {
            attributes["multiple"] = "multiple";
        }

        if (!string.IsNullOrWhiteSpace(ElementName)) {
            attributes["name"] = ElementName;
        }
        else if (useSsrNameFallback && !attributes.ContainsKey("name")) {
            // In non-interactive SSR forms a name is required for multipart binding.
            attributes["name"] = "file";
        }

        if (AutoFocus == true) {
            attributes["autofocus"] = "autofocus";
        }

        if (FieldDisabled || _isUploading) {
            attributes["disabled"] = "disabled";
        }

        if (FieldReadonly) {
            attributes["readonly"] = "readonly";
        }

        return attributes;
    }

    private bool ShowUploadButton => OnUploadButtonClick.HasDelegate;

    private bool UploadButtonDisabled => _pendingFiles.Count == 0 || _isUploading || FieldDisabled || FieldReadonly;

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        await DisposeOwnedStreamsAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private async Task OnFilesSelectedAsync(InputFileChangeEventArgs e) {
        if (_isUploading) {
            return;
        }

        await DisposeOwnedStreamsAsync();

        if (e.FileCount > MaximumFileCount) {
            _pendingFiles.Clear();
            await InitializeFileProgressStatesAsync(Array.Empty<IBrowserFile>(), string.Empty);
            await UpdateOverallProgressAsync(0, string.Empty);

            if (OnFileCountExceeded.HasDelegate) {
                await OnFileCountExceeded.InvokeAsync(e.FileCount);
            }
            return;
        }

        var selectedFiles = e.GetMultipleFiles(MaximumFileCount);
        _pendingFiles.Clear();
        _pendingFiles.AddRange(selectedFiles);

        await InitializeFileProgressStatesAsync(selectedFiles, ResourceReadyToUpload);
        await UpdateOverallProgressAsync(0, ResourceReadyToUpload);

        if (OnSelectionChanged.HasDelegate) {
            await OnSelectionChanged.InvokeAsync(selectedFiles);
        }
    }

    private async Task OnUploadButtonClickedAsync(MouseEventArgs _) {
        if (UploadButtonDisabled || !ShowUploadButton) {
            return;
        }

        var selectedFiles = _pendingFiles.ToArray();
        if (selectedFiles.Length == 0) {
            return;
        }

        _isUploading = true;
        await InvokeAsync(StateHasChanged);

        var clearSelection = false;
        try {
            await OnUploadButtonClick.InvokeAsync(selectedFiles);
            await ProcessFilesAsync(selectedFiles);
            clearSelection = true;
        }
        finally {
            _isUploading = false;

            if (ClearSelectionAfterUpload && clearSelection) {
                _pendingFiles.Clear();
            }

            await DisposeOwnedStreamsAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ProcessFilesAsync(IReadOnlyList<IBrowserFile> files) {
        if (files.Count == 0) {
            await UpdateOverallProgressAsync(0, string.Empty);
            return;
        }

        await InitializeFileProgressStatesAsync(files, ResourceReadyToUpload);

        var allFilesSummary = files.Select(i => new NTUploadedFileDetails {
            Name = i.Name,
            Size = i.Size,
            ContentType = i.ContentType
        }).ToArray();

        var uploadedFiles = new List<NTInputFileEventArgs>(files.Count);
        var totalFileSizes = files.Sum(i => i.Size);
        var totalRead = 0L;

        for (var fileNumber = 0; fileNumber < files.Count; fileNumber++) {
            var file = files[fileNumber];
            var fileState = _fileProgressStates[fileNumber];

            var fileDetails = new NTInputFileEventArgs {
                AllFiles = allFilesSummary,
                Index = fileNumber,
                Name = file.Name,
                ContentType = file.ContentType,
                Size = file.Size,
                LastModified = file.LastModified
            };
            uploadedFiles.Add(fileDetails);

            if (file.Size > MaximumFileSize) {
                fileDetails.ErrorMessage = MaxSizeErrorMessage;
                await UpdateFileProgressAsync(fileState, 0, MaxSizeErrorMessage);
                if (OnFileError.HasDelegate) {
                    await OnFileError.InvokeAsync(fileDetails);
                }
                continue;
            }

            if (!OnUploadFile.HasDelegate) {
                await UpdateFileProgressAsync(fileState, 0, ResourceNoUploadHandler);
                continue;
            }

            var uploadTitle = string.Format(ResourceUploadingInProgress, fileNumber + 1, files.Count, file.Name);
            var processingTitle = string.Format(ResourceProcessingInProgress, fileNumber + 1, files.Count, file.Name);
            var fileRead = 0L;

            await UpdateFileProgressAsync(fileState, 0, uploadTitle);
            await UpdateOverallProgressAsync(totalRead, totalFileSizes, uploadTitle);

            var trackedStream = new NTInputFileProgressTrackingReadStream(
                file.OpenReadStream(MaximumFileSize),
                async bytesRead => {
                    fileRead += bytesRead;
                    totalRead += bytesRead;

                    if (fileRead > file.Size) {
                        var overflow = fileRead - file.Size;
                        fileRead = file.Size;
                        totalRead -= overflow;
                    }

                    await UpdateFileProgressAsync(fileState, fileRead, file.Size, uploadTitle);
                    await UpdateOverallProgressAsync(totalRead, totalFileSizes, uploadTitle);
                    await RaiseProgressChangedAsync(fileDetails);
                },
                async () => {
                    await EnterProcessingPhaseAsync(fileDetails, fileState, processingTitle);
                });

            TrackOwnedStream(trackedStream);
            fileDetails.Stream = trackedStream;

            try {
                await OnUploadFile.InvokeAsync(fileDetails);
            }
            finally {
                fileDetails.Stream = null;
                await DisposeOwnedStreamAsync(trackedStream);
                await ExitProcessingPhaseAsync(fileState, fileDetails.IsCancelled);
            }

            if (fileDetails.IsCancelled) {
                break;
            }
        }

        if (uploadedFiles.Any(i => i.IsCancelled)) {
            await UpdateOverallProgressAsync(totalRead, totalFileSizes, ResourceLoadingCanceled);
        }
        else if (totalRead >= totalFileSizes) {
            await UpdateOverallProgressAsync(100, ResourceLoadingCompleted);
        }
        else {
            await UpdateOverallProgressAsync(totalRead, totalFileSizes, ResourceLoadingPartiallyProcessed);
        }

        if (OnCompleted.HasDelegate) {
            await OnCompleted.InvokeAsync(uploadedFiles);
        }
    }

    private Task InitializeFileProgressStatesAsync(IReadOnlyList<IBrowserFile> files, string initialStatus) {
        return InvokeAsync(() => {
            _fileProgressStates.Clear();

            for (var i = 0; i < files.Count; i++) {
                var file = files[i];
                _fileProgressStates.Add(new FileProgressState {
                    Index = i,
                    Name = file.Name,
                    Size = file.Size,
                    Percentage = 0,
                    Status = initialStatus,
                    IsIndeterminate = false
                });
            }

            StateHasChanged();
        });
    }

    private async Task RaiseProgressChangedAsync(NTInputFileEventArgs fileDetails) {
        await InvokeAsync(async () => {
            fileDetails.ProgressPercent = _progressPercent;
            fileDetails.ProgressTitle = _progressTitle;

            if (OnProgressChanged.HasDelegate) {
                await OnProgressChanged.InvokeAsync(fileDetails);
            }

            StateHasChanged();
        });
    }

    private async Task EnterProcessingPhaseAsync(NTInputFileEventArgs fileDetails, FileProgressState fileState, string title) {
        await UpdateFileProgressAsync(fileState, fileState.Percentage, title, true);
        await UpdateOverallProgressAsync(_progressPercent, title);
        await RaiseProgressChangedAsync(fileDetails);
    }

    private Task ExitProcessingPhaseAsync(FileProgressState fileState, bool isCancelled) {
        return InvokeAsync(() => {
            ApplyFinalFileStatus(fileState, isCancelled);
            StateHasChanged();
        });
    }

    private Task UpdateOverallProgressAsync(long current, long size, string title) {
        var percent = Convert.ToInt32(decimal.Divide(current, size <= 0 ? 1 : size) * 100);
        return UpdateOverallProgressAsync(Math.Clamp(percent, 0, 100), title);
    }

    private Task UpdateOverallProgressAsync(int percent, string title) {
        return InvokeAsync(() => {
            _progressPercent = Math.Clamp(percent, 0, 100);
            _progressTitle = title;
        });
    }

    private Task UpdateFileProgressAsync(FileProgressState fileState, long current, long size, string status, bool isIndeterminate = false) {
        if (size <= 0) {
            return UpdateFileProgressAsync(fileState, 100, status, isIndeterminate);
        }

        var percent = Convert.ToInt32(decimal.Divide(current, size) * 100);
        return UpdateFileProgressAsync(fileState, Math.Clamp(percent, 0, 100), status, isIndeterminate);
    }

    private Task UpdateFileProgressAsync(FileProgressState fileState, int percent, string status, bool isIndeterminate = false) {
        var clampedPercent = Math.Clamp(percent, 0, 100);

        return InvokeAsync(() => {
            fileState.Percentage = clampedPercent;
            fileState.Status = status;
            fileState.IsIndeterminate = isIndeterminate;
        });
    }

    private static void ApplyFinalFileStatus(FileProgressState fileState, bool isCancelled) {
        fileState.IsIndeterminate = false;

        if (isCancelled) {
            fileState.Status = ResourceLoadingCanceled;
            return;
        }

        if (fileState.Percentage >= 100 || fileState.Size <= 0) {
            fileState.Percentage = 100;
            fileState.Status = ResourceLoadingCompleted;
            return;
        }

        fileState.Status = ResourceLoadingPartiallyProcessed;
    }

    private void TrackOwnedStream(Stream stream) => _ownedStreams.Add(stream);

    private async Task DisposeOwnedStreamAsync(Stream stream) {
        _ownedStreams.Remove(stream);

        try {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException) {
            // Stream was already disposed by consumer code.
        }
    }

    private async Task DisposeOwnedStreamsAsync() {
        if (_ownedStreams.Count == 0) {
            return;
        }

        var ownedStreams = _ownedStreams.ToArray();
        _ownedStreams.Clear();

        foreach (var stream in ownedStreams) {
            try {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException) {
                // Stream was already disposed by consumer code.
            }
        }
    }

    private static string GetAppearanceClass(ITnTForm? parentForm, FormAppearance appearance) {
        var effectiveAppearance = parentForm is not null ? parentForm.Appearance : appearance;

        var appearanceClass = effectiveAppearance switch {
            FormAppearance.Filled => "tnt-form-filled",
            FormAppearance.FilledCompact => "tnt-form-filled",
            FormAppearance.Outlined => "tnt-form-outlined",
            FormAppearance.OutlinedCompact => "tnt-form-outlined",
            _ => throw new NotSupportedException()
        };

        if (effectiveAppearance is FormAppearance.FilledCompact or FormAppearance.OutlinedCompact) {
            appearanceClass += " tnt-form-compact";
        }

        return appearanceClass;
    }

    private static string GetSelectorButtonHeight(Size size) => size switch {
        Size.Smallest or Size.XS => "32px",
        Size.Small => "40px",
        Size.Medium => "56px",
        Size.Large => "96px",
        Size.Largest or Size.XL => "136px",
        _ => "40px"
    };

    private static string GetSelectorButtonPaddingX(Size size) => size switch {
        Size.Smallest or Size.XS => "12px",
        Size.Small => "16px",
        Size.Medium => "24px",
        Size.Large => "48px",
        Size.Largest or Size.XL => "64px",
        _ => "16px"
    };

    private static string GetSelectorButtonBorderRadius(Size size) => size switch {
        Size.Smallest or Size.XS => "32px / 100%",
        Size.Small => "40px / 100%",
        Size.Medium => "56px / 100%",
        Size.Large => "96px / 100%",
        Size.Largest or Size.XL => "136px / 100%",
        _ => "40px / 100%"
    };

    private sealed class FileProgressState {
        public required int Index { get; init; }

        public bool IsIndeterminate { get; set; }

        public required string Name { get; init; }

        public int Percentage { get; set; }

        public required long Size { get; init; }

        public string Status { get; set; } = string.Empty;
    }
}

/// <summary>
///     Represents a file involved in an <see cref="NTInputFile" /> operation.
/// </summary>
public sealed class NTInputFileEventArgs : EventArgs {

    /// <summary>
    ///     All files selected in the current run.
    /// </summary>
    public IReadOnlyList<NTUploadedFileDetails> AllFiles { get; internal set; } = default!;

    /// <summary>
    ///     MIME type.
    /// </summary>
    public string ContentType { get; internal set; } = string.Empty;

    /// <summary>
    ///     Error details when validation or processing fails.
    /// </summary>
    public string? ErrorMessage { get; internal set; }

    /// <summary>
    ///     Index in current batch.
    /// </summary>
    public int Index { get; internal set; }

    /// <summary>
    ///     Allows consumer code to stop remaining uploads.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    ///     Last modified timestamp.
    /// </summary>
    public DateTimeOffset LastModified { get; internal set; }

    /// <summary>
    ///     File name.
    /// </summary>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    ///     Current overall progress percent.
    /// </summary>
    public int ProgressPercent { get; internal set; }

    /// <summary>
    ///     Current overall progress title.
    /// </summary>
    public string ProgressTitle { get; internal set; } = string.Empty;

    /// <summary>
    ///     File size.
    /// </summary>
    public long Size { get; internal set; }

    /// <summary>
    ///     Progress-aware stream for the current file.
    /// </summary>
    public Stream? Stream { get; internal set; }
}

/// <summary>
///     File metadata summary.
/// </summary>
public readonly record struct NTUploadedFileDetails {
    /// <summary>
    ///     File name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     File size.
    /// </summary>
    public required long Size { get; init; }

    /// <summary>
    ///     File MIME type.
    /// </summary>
    public required string ContentType { get; init; }
}

internal sealed class NTInputFileProgressTrackingReadStream : Stream {
    private readonly Stream _inner;
    private readonly Func<int, Task> _onBytesRead;
    private readonly Func<Task>? _onCompleted;
    private int _completed;

    public NTInputFileProgressTrackingReadStream(Stream inner, Func<int, Task> onBytesRead, Func<Task>? onCompleted = null) {
        _inner = inner;
        _onBytesRead = onBytesRead;
        _onCompleted = onCompleted;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;

    public override long Position {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) {
        var bytesRead = _inner.Read(buffer, offset, count);
        ReportRead(bytesRead);
        return bytesRead;
    }

    public override int Read(Span<byte> buffer) {
        var bytesRead = _inner.Read(buffer);
        ReportRead(bytesRead);
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
        var bytesRead = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        await ReportReadAsync(bytesRead).ConfigureAwait(false);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        var bytesRead = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        await ReportReadAsync(bytesRead).ConfigureAwait(false);
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => _inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
    public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.WriteAsync(buffer, cancellationToken);

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync() {
        await _inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private void ReportRead(int bytesRead) {
        if (bytesRead > 0) {
            _ = _onBytesRead(bytesRead);
            return;
        }

        _ = CompleteAsync();
    }

    private Task ReportReadAsync(int bytesRead) {
        if (bytesRead > 0) {
            return _onBytesRead(bytesRead);
        }

        return CompleteAsync();
    }

    private Task CompleteAsync() {
        if (Interlocked.Exchange(ref _completed, 1) == 1) {
            return Task.CompletedTask;
        }

        return _onCompleted?.Invoke() ?? Task.CompletedTask;
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NTComponents.CodeDocumentation;
using NTComponents.Core;
using NTComponents.DebugConsole;
using NTComponents.Ext;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace NTComponents;

/// <summary>
///     A debug console collapsed at the bottom of the page that shows the current session's logs and feedback, so people can see and share what happened while reproducing a bug.
/// </summary>
/// <remarks>
///     <para>
///         Place it once in a layout and register the console with <c>AddNTDebugConsole</c> for WebAssembly or <c>AddNTDebugConsoleForServer</c> for Blazor Server. It
///         shows the <c>ILogger</c> output of the current user's session only, messages written with <c>INTDebugConsole.Write</c>, and the toasts and snackbars the user saw. It renders
///         nothing until <see cref="NTDebugConsoleOptions.Authorize" /> allows the current user.
///     </para>
///     <para>
///         Collapsed, it is a slim bar with the error, warning, and total counts. Expanded, it is a bottom sheet with level filters, search, Clear, Copy, and Download; drag its handle,
///         or focus the handle and use the arrow keys, Home, and End, to resize it, and double-click the handle to restore the default height. The list stays at the
///         newest entry while it is scrolled to the bottom; otherwise it keeps its position and offers a button to jump to the new entries. <see cref="Shortcut" /> shows and hides it, and
///         <see cref="Expanded" /> and the chosen height are remembered per browser; <see cref="Visible" /> is not, so a console hidden with the shortcut is back after the next load.
///     </para>
///     <para>
///         While it is shown, the console sets <c>--nt-debug-console-inset</c> on the document root to its height. Add <c>padding-block-end: var(--nt-debug-console-inset, 0px)</c> to the page's
///         scrolling content so nothing is hidden behind it; the library does not add page padding itself.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Requires an interactive render mode to receive entries and respond to its controls.",
    CompatibilityDetails = "Static SSR and prerendering render nothing, because entries are captured per interactive session. The browser module handles the shortcut, the scrolling rule, resizing, the page inset, and the remembered state.")]
public partial class NTDebugConsole {
    internal const int MaxRenderedEntries = 500;
    private const string DefaultShortcut = "Ctrl+`";
    private const string JsModulePath = "./_content/NTComponents/DebugConsole/NTDebugConsole.razor.js";
    private static readonly TimeSpan RenderInterval = TimeSpan.FromMilliseconds(250);

    private readonly ConditionalWeakTable<NTDebugConsoleEntry, StrongBox<long>> _keys = [];
    private DotNetObjectReference<NTDebugConsole>? _dotNetRef;
    private IJSObjectReference? _module;
    private long _nextKey;
    private int _renderScheduled;
    private string? _search;
    private bool _showDebug = true;
    private bool _showErrors = true;
    private bool _showInformation = true;
    private bool _showWarnings = true;
    private string? _status;
    private (bool Shown, bool Visible, bool Expanded, string? Shortcut)? _syncedState;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-debug-console")
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-debug-console-expanded", Expanded)
        .AddElevation(NTElevation.Lowest)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Gets or sets a value indicating whether the console shows the bottom sheet with the entries, or only the collapsed bar. Defaults to <see langword="false" />.
    /// </summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>
    ///     Gets or sets a callback invoked when <see cref="Expanded" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    /// <summary>
    ///     Gets or sets the keyboard shortcut that shows and hides the console, such as <c>Ctrl+`</c>, the default, or <c>Ctrl+Shift+L</c>. Combine <c>Ctrl</c>, <c>Alt</c>, <c>Shift</c>, and
    ///     <c>Meta</c> with one key. Set it to <see langword="null" /> or empty to turn the shortcut off.
    /// </summary>
    [Parameter]
    public string? Shortcut { get; set; } = DefaultShortcut;

    /// <summary>
    ///     Gets or sets a value indicating whether the console is shown. When hidden, it renders nothing and only <see cref="Shortcut" /> brings it back; hiding is not remembered, so it shows again after the page reloads. Defaults to <see langword="true" />.
    /// </summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>
    ///     Gets or sets a callback invoked when <see cref="Visible" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Inject]
    private INTDebugConsole DebugConsole { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private string PanelId => $"{ElementId ?? ComponentIdentifier}-entries";

    private bool IsShown => DebugConsole.IsEnabled && Visible;

    /// <summary>
    ///     Applies the state remembered by the browser. Called by the browser module after the first render.
    /// </summary>
    [JSInvokable]
    public Task RestoreStateAsync(bool expanded) => InvokeAsync(async () => {
        await SetExpandedAsync(expanded);
        StateHasChanged();
    });

    /// <summary>
    ///     Shows or hides the console. Called by the browser module when <see cref="Shortcut" /> is pressed.
    /// </summary>
    [JSInvokable]
    public Task ToggleVisibleAsync() => InvokeAsync(async () => {
        await SetVisibleAsync(!Visible);
        StateHasChanged();
    });

    internal static string FormatLevel(LogLevel level) => level switch {
        LogLevel.Critical => "CRIT",
        LogLevel.Error => "ERROR",
        LogLevel.Warning => "WARN",
        LogLevel.Information => "INFO",
        LogLevel.Debug => "DEBUG",
        _ => "TRACE"
    };

    internal static string FormatTime(DateTimeOffset timestamp) => timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    internal static string ShortCategory(string category) {
        // Generic categories such as Namespace.Type<Other.Type> keep their type arguments.
        var genericStart = category.IndexOf('<');
        var lastDot = (genericStart < 0 ? category : category[..genericStart]).LastIndexOf('.');
        return lastDot < 0 ? category : category[(lastDot + 1)..];
    }

    internal static string ToJson(IEnumerable<NTDebugConsoleEntry> entries) {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true })) {
            writer.WriteStartArray();
            foreach (var entry in entries) {
                writer.WriteStartObject();
                writer.WriteString("timestamp", entry.Timestamp);
                writer.WriteString("level", entry.Level.ToString());
                writer.WriteString("category", entry.Category);
                writer.WriteString("message", entry.Message);
                writer.WriteString("exception", entry.Exception);
                writer.WriteNumber("eventId", entry.EventId.Id);
                writer.WriteString("eventName", entry.EventId.Name);
                writer.WriteString("source", entry.Source.ToString());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static string ToText(IEnumerable<NTDebugConsoleEntry> entries) {
        var builder = new StringBuilder();
        foreach (var entry in entries) {
            builder.Append(FormatTime(entry.Timestamp)).Append(' ').Append(FormatLevel(entry.Level)).Append(" [").Append(entry.Category).Append("] ");
            if (entry.Source != NTDebugConsoleEntrySource.Log) {
                builder.Append('(').Append(entry.Source).Append(") ");
            }
            // Plain line feeds keep copied text the same on every host.
            builder.Append(entry.Message).Append('\n');
            if (entry.Exception is not null) {
                builder.Append(entry.Exception).Append('\n');
            }
        }
        return builder.ToString();
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore() {
        DebugConsole.Changed -= OnDebugConsoleChanged;
        if (_module is not null) {
            try {
                await _module.InvokeVoidAsync("onDispose", ComponentIdentifier);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) {
                // The browser is gone, so there is nothing left to clean up.
            }
            _module = null;
        }
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            DebugConsole.Changed -= OnDebugConsoleChanged;
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        // The module loads once the console is first enabled, and is called again only when what it tracks changes, so the console's renders never cause interop of their own.
        if (DisposalStarted || (_module is null && !DebugConsole.IsEnabled)) {
            return;
        }

        var state = (IsShown, Visible, Expanded, Shortcut);
        if (_syncedState == state) {
            return;
        }

        try {
            if (_module is null) {
                _dotNetRef ??= DotNetObjectReference.Create(this);
                var module = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                if (DisposalStarted) {
                    await module.DisposeAsync();
                    return;
                }

                _module = module;
                _syncedState = state;
                await module.InvokeVoidAsync("onLoad", ComponentIdentifier, RootElement(state.IsShown), _dotNetRef, Shortcut);
                return;
            }

            _syncedState = state;
            await _module.InvokeVoidAsync("onUpdate", ComponentIdentifier, RootElement(state.IsShown), Shortcut, Expanded);
        }
        catch (JSDisconnectedException) {
            // The circuit is gone; nothing to update.
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        DebugConsole.Changed += OnDebugConsoleChanged;
    }

    // One pass over the buffer per render instead of a LINQ count per level.
    private static (int Errors, int Warnings) CountProblems(IReadOnlyList<NTDebugConsoleEntry> entries) {
        var errors = 0;
        var warnings = 0;
        for (var i = 0; i < entries.Count; i++) {
            switch (entries[i].Level) {
                case LogLevel.Error or LogLevel.Critical:
                    errors++;
                    break;
                case LogLevel.Warning:
                    warnings++;
                    break;
            }
        }
        return (errors, warnings);
    }

    private static string LevelClass(LogLevel level) => level switch {
        LogLevel.Critical or LogLevel.Error => "nt-debug-console-level-error",
        LogLevel.Warning => "nt-debug-console-level-warning",
        LogLevel.Information => "nt-debug-console-level-info",
        _ => "nt-debug-console-level-debug"
    };

    private Task ClearAsync() {
        DebugConsole.Clear();
        _status = null;
        return Task.CompletedTask;
    }

    private async Task CopyAsync() {
        var visible = VisibleEntries(DebugConsole.Entries);
        bool copied;
        try {
            copied = _module is not null && await _module.InvokeAsync<bool>("copyText", ToText(visible));
        }
        catch (JSException) {
            copied = false;
        }
        _status = copied ? $"Copied {visible.Count} {(visible.Count == 1 ? "entry" : "entries")}" : "Copy failed";
    }

    private async Task DownloadAsync() {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ToJson(VisibleEntries(DebugConsole.Entries))));
        try {
            await JSRuntime.DownloadFileFromStreamAsync(stream, $"debug-console-{DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.json");
            _status = null;
        }
        catch (JSException) {
            _status = "Download failed";
        }
    }

    private long GetKey(NTDebugConsoleEntry entry) => _keys.TryGetValue(entry, out var key) ? key.Value : 0;

    private bool IsLevelShown(LogLevel level) => level switch {
        LogLevel.Critical or LogLevel.Error => _showErrors,
        LogLevel.Warning => _showWarnings,
        LogLevel.Information => _showInformation,
        _ => _showDebug
    };

    private bool Matches(NTDebugConsoleEntry entry) =>
        IsLevelShown(entry.Level) &&
        (string.IsNullOrWhiteSpace(_search) ||
            entry.Message.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
            entry.Category.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
            entry.Exception?.Contains(_search, StringComparison.OrdinalIgnoreCase) == true);

    private void OnDebugConsoleChanged() {
        // Entries can arrive from any thread and in bursts; render at most every RenderInterval.
        if (Interlocked.Exchange(ref _renderScheduled, 1) == 0) {
            _ = RenderSoonAsync();
        }
    }

    private async Task RenderSoonAsync() {
        await Task.Delay(RenderInterval);
        Volatile.Write(ref _renderScheduled, 0);
        if (DisposalStarted) {
            return;
        }

        try {
            await InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException) {
            // The renderer was disposed while the render was pending.
        }
    }

    private object? RootElement(bool shown) => shown ? Element : null;

    private async Task SetExpandedAsync(bool expanded) {
        if (Expanded != expanded) {
            Expanded = expanded;
            await ExpandedChanged.InvokeAsync(expanded);
        }
    }

    private async Task SetVisibleAsync(bool visible) {
        if (Visible != visible) {
            Visible = visible;
            await VisibleChanged.InvokeAsync(visible);
        }
    }

    private Task ToggleExpandedAsync() => SetExpandedAsync(!Expanded);

    // Keys follow capture order across the whole buffer, so entries a filter hides keep their place when they are shown again.
    private void UpdateKeys(IReadOnlyList<NTDebugConsoleEntry> entries) {
        foreach (var entry in entries) {
            if (!_keys.TryGetValue(entry, out _)) {
                _keys.Add(entry, new StrongBox<long>(++_nextKey));
            }
        }
    }

    private List<NTDebugConsoleEntry> VisibleEntries(IReadOnlyList<NTDebugConsoleEntry> entries) {
        var visible = new List<NTDebugConsoleEntry>(entries.Count);
        for (var i = 0; i < entries.Count; i++) {
            if (Matches(entries[i])) {
                visible.Add(entries[i]);
            }
        }
        return visible;
    }
}

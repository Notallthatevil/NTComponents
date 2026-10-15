using Microsoft.Extensions.Logging;

namespace NTComponents.DebugConsole;

/// <summary>
///     One entry captured by the debug console.
/// </summary>
/// <param name="Timestamp">When the entry was captured, in the host's local time.</param>
/// <param name="Level">The entry's level.</param>
/// <param name="Category">The logger category, or the category given to <c>INTDebugConsole.Write</c>.</param>
/// <param name="Message">The formatted message.</param>
/// <param name="Exception">The exception text, when the log had one. See <see cref="NTDebugConsoleOptions.IncludeExceptionDetails" />.</param>
/// <param name="EventId">The logger event id.</param>
/// <param name="Source">Where the entry came from.</param>
public sealed record NTDebugConsoleEntry(DateTimeOffset Timestamp, LogLevel Level, string Category, string Message, string? Exception, EventId EventId, NTDebugConsoleEntrySource Source);

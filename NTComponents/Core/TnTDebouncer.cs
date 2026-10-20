namespace NTComponents.Core;

/// <summary>
///     Obsolete compatibility alias for <see cref="NTDebouncer" />.
/// </summary>
/// <param name="millisecondsDelay">The delay in milliseconds to wait before executing the action.</param>
[Obsolete("TnTDebouncer is obsolete. Use NTDebouncer instead.")]
public class TnTDebouncer(int millisecondsDelay = 300) : NTDebouncer(millisecondsDelay);

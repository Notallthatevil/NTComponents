namespace NTComponents.DebugConsole;

/// <summary>
///     Where a debug console entry came from.
/// </summary>
public enum NTDebugConsoleEntrySource {

    /// <summary>
    ///     <c>ILogger</c> output of the current session.
    /// </summary>
    Log,

    /// <summary>
    ///     An app or user message written with <c>INTDebugConsole.Write</c>.
    /// </summary>
    App,

    /// <summary>
    ///     A toast shown through <see cref="INTToastService" />.
    /// </summary>
    Toast,

    /// <summary>
    ///     A snackbar shown through <see cref="INTSnackbarService" />.
    /// </summary>
    Snackbar
}

namespace NTComponents.Toast;

/// <summary>
///     Supported semantic variants for <see cref="NTToast" /> messages.
/// </summary>
public enum NTToastVariant {
    /// <summary>
    ///     Neutral toast message.
    /// </summary>
    Default,

    /// <summary>
    ///     Success confirmation toast message.
    /// </summary>
    Success,

    /// <summary>
    ///     Informational toast message.
    /// </summary>
    Info,

    /// <summary>
    ///     Warning toast message.
    /// </summary>
    Warning,

    /// <summary>
    ///     Error toast message.
    /// </summary>
    Error,

    /// <summary>
    ///     Assertion toast message.
    /// </summary>
    Assert
}

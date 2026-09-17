namespace NTComponents;

/// <summary>Controls scroll anchoring when a virtualized data source is refreshed.</summary>
public enum NTVirtualizeAnchorMode {
    /// <summary>Preserves the existing pixel scroll position without tracking item identity.</summary>
    None,
    /// <summary>Follows prepended items at the start; otherwise preserves the visible item.</summary>
    Start,
    /// <summary>Follows appended items at the end; otherwise preserves the visible item.</summary>
    End
}

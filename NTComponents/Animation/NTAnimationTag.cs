namespace NTComponents;

/// <summary>
///     HTML tags an <see cref="NTAnimation" /> wrapper can render.
/// </summary>
public enum NTAnimationTag {

    /// <summary>
    ///     Renders a <c>div</c>.
    /// </summary>
    Div,

    /// <summary>
    ///     Renders a <c>section</c>.
    /// </summary>
    Section,

    /// <summary>
    ///     Renders an <c>article</c>.
    /// </summary>
    Article,

    /// <summary>
    ///     Renders a <c>ul</c>; pair with <see cref="NTAnimation.Stagger" /> to animate its <c>li</c> children in sequence.
    /// </summary>
    UnorderedList,

    /// <summary>
    ///     Renders an <c>ol</c>; pair with <see cref="NTAnimation.Stagger" /> to animate its <c>li</c> children in sequence.
    /// </summary>
    OrderedList,

    /// <summary>
    ///     Renders an <c>li</c>, for animating a single list item.
    /// </summary>
    ListItem
}

namespace NTComponents;

/// <summary>
///     HTML tags supported by modern NT layout components.
/// </summary>
public enum NTLayoutTag {

    /// <summary>
    ///     Renders a neutral <c>div</c> container.
    /// </summary>
    Div,

    /// <summary>
    ///     Renders a <c>section</c> sectioning container.
    /// </summary>
    Section,

    /// <summary>
    ///     Renders a <c>header</c> introductory region.
    /// </summary>
    Header,

    /// <summary>
    ///     Renders a <c>footer</c> concluding region.
    /// </summary>
    Footer,

    /// <summary>
    ///     Renders a <c>main</c> primary document content region. Use only once per document.
    /// </summary>
    Main,

    /// <summary>
    ///     Renders an <c>aside</c> complementary region.
    /// </summary>
    Aside,

    /// <summary>
    ///     Renders an <c>article</c> self-contained section.
    /// </summary>
    Article

}

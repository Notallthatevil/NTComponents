namespace NTComponents;

/// <summary>
///     Rebuilds the root attribute dictionary shared by the form layout components, keeping the insertion order they render with.
/// </summary>
internal static class NTFormElementAttributes {

    /// <summary>
    ///     Copies the additional attributes, then sets class, style, autofocus, id, lang, and title in that order. Null or blank values remove the attribute.
    /// </summary>
    public static void Build(Dictionary<string, object> target, IReadOnlyDictionary<string, object>? additionalAttributes, string cssClass, string? style, bool? autoFocus, string? id, string? lang, string? title) {
        target.Clear();
        if (additionalAttributes is not null) {
            foreach (var (attributeName, attributeValue) in additionalAttributes) {
                target[attributeName] = attributeValue;
            }
        }

        target["class"] = cssClass;
        if (!string.IsNullOrWhiteSpace(style)) {
            target["style"] = style;
        }
        else {
            target.Remove("style");
        }

        if (autoFocus == true) {
            target["autofocus"] = true;
        }

        Set(target, "id", id);
        Set(target, "lang", lang);
        Set(target, "title", title);
    }

    private static void Set(Dictionary<string, object> target, string attributeName, string? value) {
        if (value is null) {
            target.Remove(attributeName);
            return;
        }

        target[attributeName] = value;
    }
}

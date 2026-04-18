using System.Text;

namespace NTComponents.Site.Documentation;

/// <summary>
/// Creates stable URL slugs for documentation entries.
/// </summary>
public static class DocumentationSlugs {

    /// <summary>
    /// Creates a stable slug from a display value.
    /// </summary>
    /// <param name="value">The value to convert to a slug.</param>
    /// <returns>A lower-case slug containing only letters, numbers, and hyphens.</returns>
    public static string Create(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "item";
        }

        var builder = new StringBuilder(value.Length + 8);
        var previousWasSeparator = true;
        var previousWasLetterOrDigit = false;

        var trimmed = value.Trim();
        for (var index = 0; index < trimmed.Length; index++) {
            var character = trimmed[index];
            if (char.IsLetterOrDigit(character)) {
                var nextIsLower = index + 1 < trimmed.Length && char.IsLower(trimmed[index + 1]);
                var startsWord = char.IsUpper(character) &&
                    previousWasLetterOrDigit &&
                    !previousWasSeparator &&
                    (char.IsLower(trimmed[index - 1]) || nextIsLower);

                if (startsWord) {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                previousWasLetterOrDigit = true;
                continue;
            }

            if (!previousWasSeparator && builder.Length > 0) {
                builder.Append('-');
                previousWasSeparator = true;
            }

            previousWasLetterOrDigit = false;
        }

        while (builder.Length > 0 && builder[^1] == '-') {
            builder.Length--;
        }

        return builder.Length == 0 ? "item" : builder.ToString();
    }
}

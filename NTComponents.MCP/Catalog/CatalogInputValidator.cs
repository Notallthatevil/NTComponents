namespace NTComponents.MCP.Catalog;

internal static class CatalogInputValidator {
    public const int MinimumLimit = 1;
    public const int MaximumLimit = 200;
    public const int MaximumQueryLength = 512;
    public const int MaximumSearchTerms = 16;
    public const string EnumReferenceKind = "Enum";
    public const string HelperReferenceKind = "Helper";

    public static void ValidateLimit(int limit) {
        if (limit is < MinimumLimit or > MaximumLimit) {
            throw new CatalogValidationException(nameof(limit), $"limit must be between {MinimumLimit} and {MaximumLimit}.");
        }
    }

    public static void ValidateReferenceKind(string? kind) {
        if (kind is null) {
            return;
        }

        if (string.IsNullOrWhiteSpace(kind)
            || (!string.Equals(kind, EnumReferenceKind, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(kind, HelperReferenceKind, StringComparison.OrdinalIgnoreCase))) {
            throw new CatalogValidationException(nameof(kind), $"kind must be {EnumReferenceKind} or {HelperReferenceKind}.");
        }
    }

    public static void ValidateRequiredText(string? value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new CatalogValidationException(parameterName, $"{parameterName} is required and cannot be blank.");
        }
    }

    public static void ValidateOptionalQuery(string? query) {
        if (query?.Length > MaximumQueryLength) {
            throw new CatalogValidationException(nameof(query), $"query cannot exceed {MaximumQueryLength} characters.");
        }
    }

    public static void ValidateRequiredQuery(string? query) {
        ValidateRequiredText(query, nameof(query));
        ValidateOptionalQuery(query);
    }

    public static void ValidateQueryTermCount(int termCount) {
        if (termCount > MaximumSearchTerms) {
            throw new CatalogValidationException("query", $"query cannot contain more than {MaximumSearchTerms} distinct search terms.");
        }
    }
}

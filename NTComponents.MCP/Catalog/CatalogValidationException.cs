namespace NTComponents.MCP.Catalog;

public sealed class CatalogValidationException(string parameterName, string message) : Exception(message) {
    public string ParameterName { get; } = parameterName;
}

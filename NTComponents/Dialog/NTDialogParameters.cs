using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     Parameters supplied to <see cref="NTDialog" /> when it opens or refreshes.
/// </summary>
public sealed class NTDialogParameters : IEnumerable<KeyValuePair<string, object?>> {
    private readonly Dictionary<string, object?> _parameters = [];

    /// <summary>
    ///     Gets a named parameter.
    /// </summary>
    public object? this[string name] => _parameters[name];

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTDialogParameters" /> class.
    /// </summary>
    public NTDialogParameters() {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTDialogParameters" /> class.
    /// </summary>
    public NTDialogParameters(IEnumerable<KeyValuePair<string, object?>>? parameters) {
        if (parameters is null) {
            return;
        }

        foreach (var parameter in parameters) {
            Add(parameter.Key, parameter.Value);
        }
    }

    /// <summary>
    ///     Converts dialog parameters to a dictionary.
    /// </summary>
    public static implicit operator Dictionary<string, object?>(NTDialogParameters parameters) => parameters is null ? [] : new(parameters._parameters);

    /// <summary>
    ///     Converts a dictionary to dialog parameters.
    /// </summary>
    public static implicit operator NTDialogParameters(Dictionary<string, object?>? parameters) => new(parameters);

    /// <summary>
    ///     Adds a named parameter.
    /// </summary>
    public void Add(string name, object? value) => _parameters.Add(name, value);

    /// <summary>
    ///     Gets a named parameter and casts it to <typeparamref name="T" />.
    /// </summary>
    public T Get<T>(string name) {
        if (!_parameters.TryGetValue(name, out var value)) {
            throw new KeyNotFoundException($"Dialog parameter '{name}' was not found.");
        }

        if (TryCast(value, out T? typedValue)) {
            return typedValue!;
        }

        throw new InvalidCastException($"Dialog parameter '{name}' cannot be cast to {typeof(T).Name}.");
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _parameters.GetEnumerator();

    /// <summary>
    ///     Attempts to get a named parameter and cast it to <typeparamref name="T" />.
    /// </summary>
    public bool TryGet<T>(string name, [MaybeNull] out T value) {
        if (_parameters.TryGetValue(name, out var parameterValue) && TryCast(parameterValue, out value)) {
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static bool TryCast<T>(object? value, [MaybeNull] out T typedValue) {
        if (value is T matchingValue) {
            typedValue = matchingValue;
            return true;
        }

        if (value is null && default(T) is null) {
            typedValue = default;
            return true;
        }

        typedValue = default;
        return false;
    }
}
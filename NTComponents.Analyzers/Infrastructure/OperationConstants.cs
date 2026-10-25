using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>Reads compile-time constants out of the values Razor passes to component parameters.</summary>
internal static class OperationConstants {

    /// <summary>Strips conversions, parentheses, and Razor's <c>RuntimeHelpers.TypeCheck</c> wrapper.</summary>
    public static IOperation? Unwrap(IOperation? operation) {
        while (Next(operation) is { } next) {
            operation = next;
        }

        return operation;
    }

    public static bool IsNull(IOperation? operation) => Unwrap(operation)?.ConstantValue is { HasValue: true, Value: null };

    public static bool TryGetString(IOperation? operation, out string? value) {
        var constant = Unwrap(operation)?.ConstantValue;
        value = constant is { HasValue: true } ? constant.Value.Value as string : null;
        return constant is { HasValue: true } && constant.Value.Value is null or string;
    }

    public static bool TryGetBool(IOperation? operation, out bool value) {
        var constant = Unwrap(operation)?.ConstantValue;
        value = constant is { HasValue: true, Value: true };
        return constant is { HasValue: true, Value: bool };
    }

    public static bool TryGetNumber(IOperation? operation, out double value) {
        var number = Unwrap(operation)?.ConstantValue is { HasValue: true } constant ? ToDouble(constant.Value) : null;
        value = number ?? 0;
        return number.HasValue;
    }

    /// <summary>Reads an enum constant, including undefined casts. Returns false for null and non-constant values.</summary>
    public static bool TryGetEnum(IOperation? operation, out EnumConstant value) {
        value = default;
        INamedTypeSymbol? enumType = null;
        var current = operation;
        while (current is not null) {
            if (current is IFieldReferenceOperation { Field: { ContainingType.TypeKind: TypeKind.Enum } field }) {
                value = new EnumConstant(field.ContainingType, field);
                return true;
            }

            // Keep the innermost enum type so a cast such as (NTColor)99 is read against NTColor.
            enumType = EnumTypeOf(current.Type) ?? enumType;
            if (Next(current) is not { } next) {
                break;
            }

            current = next;
        }

        if (enumType is null || current?.ConstantValue is not { HasValue: true, Value: { } constant } || ToInt64(constant) is not { } number) {
            return false;
        }

        value = new EnumConstant(enumType, enumType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(field => field.HasConstantValue && ToInt64(field.ConstantValue) == number));
        return true;
    }

    private static IOperation? Next(IOperation? operation) => operation switch {
        IConversionOperation conversion => conversion.Operand,
        IParenthesizedOperation parenthesized => parenthesized.Operand,
        IInvocationOperation { TargetMethod.Name: "TypeCheck", Arguments.Length: 1 } invocation
            when invocation.TargetMethod.ContainingType.ToDisplayString() == "Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers" => invocation.Arguments[0].Value,
        _ => null
    };

    private static INamedTypeSymbol? EnumTypeOf(ITypeSymbol? type) => type switch {
        INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType => enumType,
        INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable => EnumTypeOf(nullable.TypeArguments[0]),
        _ => null
    };

    private static long? ToInt64(object? value) => value switch {
        byte number => number,
        sbyte number => number,
        short number => number,
        ushort number => number,
        int number => number,
        uint number => number,
        long number => number,
        ulong number when number <= long.MaxValue => (long)number,
        _ => null
    };

    private static double? ToDouble(object? value) => value switch {
        float number => number,
        double number => number,
        decimal number => (double)number,
        ulong number => number,
        _ => ToInt64(value)
    };
}

using NTComponents.GeneratedDocumentation;

namespace NTComponents.Site.Pages;

public sealed record MemberRow(
    string Name,
    string Signature,
    string Summary,
    string DeclaringTypeFullName,
    bool IsFromBaseType) {

    public static MemberRow FromMethod(MethodDocumentation method) =>
        new(method.Name, method.Signature, method.Summary, method.DeclaringTypeFullName, method.IsFromBaseType);

    public static MemberRow FromProperty(PropertyDocumentation property) =>
        new(property.Name, property.Signature, property.Summary, property.DeclaringTypeFullName, property.IsFromBaseType);

    public static MemberRow FromField(FieldDocumentation field) =>
        new(field.Name, field.Signature, field.Summary, field.DeclaringTypeFullName, field.IsFromBaseType);
}

using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Text;

namespace NTComponents;

/// <summary>
///     Represents the theme design component for TnT.
/// </summary>
public partial class TnTMeasurements
{
    private const string LegacyTntFontStyles =
        "body{padding:0;margin:0;}" +
        ".tnt-display-large{font-family:Roboto;font-weight:400;font-size:57px;line-height:64px;letter-spacing:-0.25px;}" +
        ".tnt-display-medium{font-family:Roboto;font-weight:400;font-size:45px;line-height:52px;letter-spacing:0px;}" +
        ".tnt-display-small{font-family:Roboto;font-weight:400;font-size:36px;line-height:44px;letter-spacing:0px;}" +
        ".tnt-headline-large{font-family:Roboto;font-weight:400;font-size:32px;line-height:40px;letter-spacing:0px;}" +
        ".tnt-headline-medium{font-family:Roboto;font-weight:400;font-size:28px;line-height:36px;letter-spacing:0px;}" +
        ".tnt-headline-small{font-family:Roboto;font-weight:400;font-size:24px;line-height:32px;letter-spacing:0px;}" +
        ".tnt-body-large{font-family:Roboto;font-weight:400;font-size:16px;line-height:24px;letter-spacing:0.50px;}" +
        ".tnt-body-medium{font-family:Roboto;font-weight:400;font-size:14px;line-height:20px;letter-spacing:0.25px;}" +
        ".tnt-body-small{font-family:Roboto;font-weight:400;font-size:12px;line-height:16px;letter-spacing:0.40px;}" +
        ".tnt-label-large{font-family:Roboto;font-weight:500;font-size:14px;line-height:20px;letter-spacing:0.10px;}" +
        ".tnt-label-medium{font-family:Roboto;font-weight:500;font-size:12px;line-height:16px;letter-spacing:0.50px;}" +
        ".tnt-label-small{font-family:Roboto;font-weight:500;font-size:11px;line-height:16px;letter-spacing:0.50px;}" +
        ".tnt-title-large{font-family:Roboto;font-weight:400;font-size:22px;line-height:28px;letter-spacing:0px;}" +
        ".tnt-title-medium{font-family:Roboto;font-weight:500;font-size:18px;line-height:24px;letter-spacing:0.15px;}" +
        ".tnt-title-small{font-family:Roboto;font-weight:500;font-size:14px;line-height:20px;letter-spacing:0.10px;}";

    private static readonly IReadOnlyList<TypographyStyle> BaselineNtTypographyStyles = new[] {
        new TypographyStyle("display-large", "brand", 400, "57px", "64px", "-0.25px"),
        new TypographyStyle("display-medium", "brand", 400, "45px", "52px", "0px"),
        new TypographyStyle("display-small", "brand", 400, "36px", "44px", "0px"),
        new TypographyStyle("headline-large", "brand", 400, "32px", "40px", "0px"),
        new TypographyStyle("headline-medium", "brand", 400, "28px", "36px", "0px"),
        new TypographyStyle("headline-small", "brand", 400, "24px", "32px", "0px"),
        new TypographyStyle("title-large", "brand", 400, "22px", "28px", "0px"),
        new TypographyStyle("title-medium", "brand", 500, "16px", "24px", "0.15px"),
        new TypographyStyle("title-small", "brand", 500, "14px", "20px", "0.10px"),
        new TypographyStyle("body-large", "plain", 400, "16px", "24px", "0.50px"),
        new TypographyStyle("body-medium", "plain", 400, "14px", "20px", "0.25px"),
        new TypographyStyle("body-small", "plain", 400, "12px", "16px", "0.40px"),
        new TypographyStyle("label-large", "brand", 500, "14px", "20px", "0.10px"),
        new TypographyStyle("label-medium", "plain", 500, "12px", "16px", "0.50px"),
        new TypographyStyle("label-small", "plain", 500, "11px", "16px", "0.50px"),
    };

    private static readonly IReadOnlyList<TypographyStyle> EmphasizedNtTypographyStyles = new[] {
        new TypographyStyle("display-large", "brand", 500, "57px", "64px", "-0.25px"),
        new TypographyStyle("display-medium", "brand", 500, "45px", "52px", "0px"),
        new TypographyStyle("display-small", "brand", 500, "36px", "44px", "0px"),
        new TypographyStyle("headline-large", "brand", 500, "32px", "40px", "0px"),
        new TypographyStyle("headline-medium", "brand", 500, "28px", "36px", "0px"),
        new TypographyStyle("headline-small", "brand", 500, "24px", "32px", "0px"),
        new TypographyStyle("title-large", "brand", 500, "22px", "28px", "0px"),
        new TypographyStyle("title-medium", "brand", 700, "16px", "24px", "0.15px"),
        new TypographyStyle("title-small", "brand", 700, "14px", "20px", "0.10px"),
        new TypographyStyle("body-large", "plain", 500, "16px", "24px", "0.50px"),
        new TypographyStyle("body-medium", "plain", 500, "14px", "20px", "0.25px"),
        new TypographyStyle("body-small", "plain", 500, "12px", "16px", "0.40px"),
        new TypographyStyle("label-large", "brand", 700, "14px", "20px", "0.10px"),
        new TypographyStyle("label-medium", "plain", 700, "12px", "16px", "0.50px"),
        new TypographyStyle("label-small", "plain", 700, "11px", "16px", "0.50px"),
    };

    private static readonly string NtFontStyles = BuildNtFontStyles();
    private static readonly string NtCornerRadiusVariables = BuildNtCornerRadiusVariables();
    private static readonly string NtCornerRadiusStyles = BuildNtCornerRadiusStyles();

    /// <summary>
    /// Gets or sets the footer height.
    /// </summary>
    [Parameter]
    public double FooterHeight { get; set; } = 64;

    /// <summary>
    /// Gets or sets the header height.
    /// </summary>
    [Parameter]
    public double HeaderHeight { get; set; } = 64;

    /// <summary>
    /// Gets or sets the side navigation width.
    /// </summary>
    [Parameter]
    public double SideNavWidth { get; set; } = 256;

    private static string BuildNtFontStyles()
    {
        var builder = new StringBuilder();

        builder.Append(":root{");
        builder.Append("--nt-ref-typeface-brand:Roboto;");
        builder.Append("--nt-ref-typeface-plain:Roboto;");
        builder.Append("--nt-ref-typeface-weight-regular:400;");
        builder.Append("--nt-ref-typeface-weight-medium:500;");
        builder.Append("--nt-ref-typeface-weight-bold:700;");
        AppendTypographyTokens(builder, BaselineNtTypographyStyles, false);
        AppendTypographyTokens(builder, EmphasizedNtTypographyStyles, true);
        builder.Append('}');

        AppendTypographyClasses(builder, BaselineNtTypographyStyles, false);
        AppendTypographyClasses(builder, EmphasizedNtTypographyStyles, true);

        return builder.ToString();
    }

    private static string BuildNtCornerRadiusVariables()
    {
        var builder = new StringBuilder();

        foreach (var cornerRadius in Enum.GetValues<NTCornerRadius>()) {
            builder.Append("--").Append(GetCornerRadiusTokenName(cornerRadius)).Append(':').Append(cornerRadius.ToCssValue()).Append(';');
        }

        return builder.ToString();
    }

    private static string BuildNtCornerRadiusStyles()
    {
        var builder = new StringBuilder();

        foreach (var cornerRadius in Enum.GetValues<NTCornerRadius>()) {
            builder.Append('.').Append(cornerRadius.ToCssClass()).Append('{');
            builder.Append("border-radius:var(--").Append(GetCornerRadiusTokenName(cornerRadius)).Append(',').Append(cornerRadius.ToCssValue()).Append(");");
            builder.Append('}');
        }

        return builder.ToString();
    }

    private static void AppendTypographyTokens(StringBuilder builder, IEnumerable<TypographyStyle> styles, bool emphasized)
    {
        foreach (var style in styles) {
            var tokenPrefix = GetTokenPrefix(style.Name, emphasized);
            builder.Append("--").Append(tokenPrefix).Append("-font:var(--nt-ref-typeface-").Append(style.FontRefTokenName).Append(",Roboto);");
            builder.Append("--").Append(tokenPrefix).Append("-weight:").Append(style.FontWeight).Append(';');
            builder.Append("--").Append(tokenPrefix).Append("-size:").Append(style.FontSize).Append(';');
            builder.Append("--").Append(tokenPrefix).Append("-line-height:").Append(style.LineHeight).Append(';');
            builder.Append("--").Append(tokenPrefix).Append("-tracking:").Append(style.Tracking).Append(';');
        }
    }

    private static void AppendTypographyClasses(StringBuilder builder, IEnumerable<TypographyStyle> styles, bool emphasized)
    {
        foreach (var style in styles) {
            builder.Append('.').Append(GetUtilityClassName(style.Name, emphasized)).Append('{');
            builder.Append("font-family:var(--").Append(GetTokenPrefix(style.Name, emphasized)).Append("-font,var(--nt-ref-typeface-").Append(style.FontRefTokenName).Append(",Roboto));");
            builder.Append("font-weight:var(--").Append(GetTokenPrefix(style.Name, emphasized)).Append("-weight,").Append(style.FontWeight).Append(");");
            builder.Append("font-size:var(--").Append(GetTokenPrefix(style.Name, emphasized)).Append("-size,").Append(style.FontSize).Append(");");
            builder.Append("line-height:var(--").Append(GetTokenPrefix(style.Name, emphasized)).Append("-line-height,").Append(style.LineHeight).Append(");");
            builder.Append("letter-spacing:var(--").Append(GetTokenPrefix(style.Name, emphasized)).Append("-tracking,").Append(style.Tracking).Append(");");
            builder.Append('}');
        }
    }

    private static string GetTokenPrefix(string styleName, bool emphasized) => emphasized
        ? $"nt-sys-typescale-emphasized-{styleName}"
        : $"nt-sys-typescale-{styleName}";

    private static string GetCornerRadiusTokenName(NTCornerRadius cornerRadius) => cornerRadius switch {
        NTCornerRadius.None => "nt-sys-shape-corner-none",
        NTCornerRadius.ExtraSmall => "nt-sys-shape-corner-extra-small",
        NTCornerRadius.Small => "nt-sys-shape-corner-small",
        NTCornerRadius.Medium => "nt-sys-shape-corner-medium",
        NTCornerRadius.Large => "nt-sys-shape-corner-large",
        NTCornerRadius.LargeIncreased => "nt-sys-shape-corner-large-increased",
        NTCornerRadius.ExtraLarge => "nt-sys-shape-corner-extra-large",
        NTCornerRadius.ExtraLargeIncreased => "nt-sys-shape-corner-extra-large-increased",
        NTCornerRadius.ExtraExtraLarge => "nt-sys-shape-corner-extra-extra-large",
        NTCornerRadius.Full => "nt-sys-shape-corner-full",
        _ => throw new ArgumentOutOfRangeException(nameof(cornerRadius), cornerRadius, null)
    };

    private static string GetUtilityClassName(string styleName, bool emphasized) => emphasized
        ? $"nt-{styleName}-emphasized"
        : $"nt-{styleName}";

    private sealed record TypographyStyle(
        string Name,
        string FontRefTokenName,
        int FontWeight,
        string FontSize,
        string LineHeight,
        string Tracking);
}

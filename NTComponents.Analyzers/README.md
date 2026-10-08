# NTComponents.Analyzers

Roslyn analyzers for NTComponents applications. The package reports invalid component configurations, inaccessible icon-only controls, conflicting data-grid options, and other mistakes that NTComponents can identify at build time.

## Install

```xml
<PackageReference Include="NTComponents.Analyzers" Version="VERSION">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Diagnostics use the `NTC` prefix and run during IDE analysis and builds.

## Color Migration

`NTC1077` migrates `TnTColor` references to `NTColor`; `NTC1079` migrates `ToCssTnTColorVariable()` calls to `ToCssNTColorVariable()`. Both diagnostics provide code fixes and Fix All support. The package includes `NTComponents.Analyzers.CodeFixes.dll` alongside the analyzer.

After updating NTComponents, analyze and apply supported C# fixes from your application directory:

```shell
dotnet format analyzers --diagnostics NTC1077 NTC1079 --severity warn
```

Pass a project or solution path after `analyzers` when needed. The CLI covers C# and `.razor.cs` files; update `.razor` expressions through your editor. `NTC1078` reports removed `None`, `Black`, and `White` values for manual replacement. Use an omitted/null optional color for defaults or choose a semantic role; those members have no safe automatic replacement.

Fix All converts color references throughout the selected C# scope. Legacy TnT components and obsolete `NTInputSelect` still accept `TnTColor`; retain their legacy arguments or use `ToLegacyColor()` for migrated shared colors, and review mixed NT/TnT bindings after applying fixes.

Source and issue tracking: <https://github.com/Notallthatevil/NTComponents>

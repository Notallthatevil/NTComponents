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

Source and issue tracking: <https://github.com/Notallthatevil/NTComponents>

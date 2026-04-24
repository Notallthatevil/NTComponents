; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
NTC1003 | Usage | Warning | NTButton text and outlined variants require a transparent background.
NTC1004 | Usage | Warning | NTButton filled, tonal, and elevated variants require a visible container background.
NTC1005 | Usage | Warning | NTButton TextColor cannot be None or Transparent.
NTC1006 | Usage | Warning | NTButton elevation must match the selected variant.
NTC1007 | Usage | Warning | NTButton text variant does not support toggle behavior.
NTC1008 | Usage | Warning | NTButton Label cannot be empty.

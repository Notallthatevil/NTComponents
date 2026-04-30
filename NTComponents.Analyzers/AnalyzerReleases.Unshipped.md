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
NTC1009 | Usage | Warning | NTSplitButton Label cannot be empty unless LeadingIcon is supplied.
NTC1010 | Usage | Warning | Icon-only NTSplitButton actions require ActionAriaLabel.
NTC1011 | Usage | Warning | NTSplitButton text and outlined variants require a transparent background.
NTC1012 | Usage | Warning | NTSplitButton filled, tonal, and elevated variants require a visible container background.
NTC1013 | Usage | Warning | NTSplitButton TextColor cannot be None or Transparent.
NTC1014 | Usage | Warning | NTSplitButton elevation must match the selected variant.
NTC1015 | Usage | Warning | NTSplitButton menu colors cannot be None or Transparent.
NTC1016 | Usage | Warning | NTSplitButton requires at least one actionable menu item.
NTC1017 | Usage | Warning | NTSplitButton menu item Label cannot be empty.
NTC1018 | Usage | Warning | NTSplitButtonAnchorItem Href cannot be empty.
NTC1019 | Usage | Warning | NTButtonGroup text variant does not support selectable behavior.
NTC1020 | Usage | Warning | NTButtonGroup text and outlined variants require a transparent background.
NTC1021 | Usage | Warning | NTButtonGroup filled, tonal, and elevated variants require a visible container background.
NTC1022 | Usage | Warning | NTButtonGroup TextColor cannot be None or Transparent.
NTC1023 | Usage | Warning | Selectable NTButtonGroup selected backgrounds cannot be None or Transparent.
NTC1024 | Usage | Warning | Selectable NTButtonGroup SelectedTextColor cannot be None or Transparent.
NTC1025 | Usage | Warning | Icon-only NTButtonGroupItem children require AriaLabel.
NTC1026 | Usage | Warning | NTIconButton Icon cannot be omitted or null.
NTC1027 | Usage | Warning | NTIconButton AriaLabel cannot be empty.
NTC1028 | Usage | Warning | NTIconButton text and unselected outlined variants require a transparent background.
NTC1029 | Usage | Warning | NTIconButton filled, tonal, elevated, and selected outlined toggle variants require a visible container background.
NTC1030 | Usage | Warning | NTIconButton TextColor cannot be None or Transparent.
NTC1031 | Usage | Warning | NTIconButton elevation must match the selected variant.

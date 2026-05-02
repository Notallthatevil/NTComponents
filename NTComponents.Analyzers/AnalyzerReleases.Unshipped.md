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
NTC1032 | Usage | Warning | NTFabButton Icon cannot be omitted or null.
NTC1033 | Usage | Warning | Icon-only NTFabButton requires AriaLabel.
NTC1034 | Usage | Warning | NTFabButton Label cannot contain line breaks.
NTC1035 | Usage | Warning | NTFabButton BackgroundColor cannot be None or Transparent.
NTC1036 | Usage | Warning | NTFabButton TextColor cannot be None or Transparent.
NTC1037 | Usage | Warning | NTFabButton ButtonSize Smallest/XS and Largest/XL are remapped to the closest supported FAB size.
NTC1038 | Usage | Warning | NTFabButton Placement must be a defined NTFabButtonPlacement value.
NTC1039 | Usage | Warning | NTFabMenu Icon cannot be omitted or null.
NTC1040 | Usage | Warning | NTFabMenu AriaLabel cannot be empty.
NTC1041 | Usage | Warning | NTFabMenu color overrides cannot be None or Transparent.
NTC1042 | Usage | Warning | NTFabMenu ButtonSize Smallest/XS and Largest/XL are remapped to the closest supported FAB size.
NTC1043 | Usage | Warning | NTFabMenu Placement must be a defined NTFabButtonPlacement value.
NTC1044 | Usage | Warning | NTFabMenu requires 2 to 6 actionable menu items.
NTC1045 | Usage | Warning | NTFabMenu item Label cannot be empty.
NTC1046 | Usage | Warning | NTFabMenuAnchorItem Href cannot be empty.
NTC1047 | Usage | Warning | NTMenu AriaLabel cannot be empty.
NTC1048 | Usage | Warning | NTMenu requires at least one actionable menu item.
NTC1049 | Usage | Warning | NTMenu item Label cannot be empty.
NTC1050 | Usage | Warning | NTMenuAnchorItem Href cannot be empty.
NTC1051 | Usage | Warning | NTMenu color overrides cannot be None or Transparent.

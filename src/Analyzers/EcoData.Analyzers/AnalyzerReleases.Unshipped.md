; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ECO001 | Style | Warning | Single-statement if should not use braces
ECO002 | Style | Warning | Method call result should not be passed inline as an argument
ECO003 | Style | Warning | Method group should not be passed as an argument
ECO004 | Style | Warning | Private method should be static
ECO005 | Style | Warning | Private method with one call site should be inlined
ECO006 | Style | Warning | OneOf failure should be handled first
ECO007 | Naming | Warning | Type name should not end in a generic suffix
ECO008 | Design | Warning | Contract record should carry only data
ECO009 | Usage | Warning | Warning suppression needs a justification
ECO010 | Style | Warning | Razor markup should not use inline styles

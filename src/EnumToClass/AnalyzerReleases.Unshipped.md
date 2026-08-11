; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ETC001 | EnumToClass | Error | Host type must be partial
ETC002 | EnumToClass | Warning | Enum has no named members
ETC003 | EnumToClass | Error | Nested host types are not supported
ETC010 | EnumToClass | Warning | Attribute type cannot be reconstructed for a property
ETC011 | EnumToClass | Error | Duplicate EnumToClassProperty for the same attribute type
ETC012 | EnumToClass | Error | Invalid or conflicting projected property name

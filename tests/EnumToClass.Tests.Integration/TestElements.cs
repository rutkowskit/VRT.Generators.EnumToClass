using VRT.Generators.EnumToClass;

namespace EnumToClass.Tests.Integration;

public enum TestElements
{
    /// <summary>
    /// Empty element
    /// </summary>
    None,
    /// <summary>
    /// First element of enum
    /// </summary>
    Element1,
    /// <summary>
    /// Second element of enum
    /// </summary>
    Element2,
    Element3
}

[EnumToClass<TestElements>]
internal sealed partial class TestElementClass
{
}

[EnumToClass<TestElements>]
public sealed partial record TestElementRecord
{
}
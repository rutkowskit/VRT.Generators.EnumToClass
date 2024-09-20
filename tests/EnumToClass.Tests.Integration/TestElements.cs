using VRT.Generators.EnumToClass;

namespace EnumToClass.Tests.Integration;

public enum TestElements
{
    None,
    Element1,
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
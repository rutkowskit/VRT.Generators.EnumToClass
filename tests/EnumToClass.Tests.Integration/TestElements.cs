using System;
using System.ComponentModel;
using System.Net;

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

    [Description("This is element 3 of the test enum")]
    Element3,


    /// <summary>
    /// This test element calculates the factorial of a given non-negative integer.
    /// </summary>
    /// <remarks>
    /// The factorial of a number \( n \) is the product of all positive integers less than or equal to \( n \).
    /// For example, <c>factorial(5)</c> returns <c>120</c>.
    /// </remarks>    
    /// A non-negative integer whose factorial is to be calculated.
    /// This parameter must be greater than or equal to 0.
    /// </param>
    /// <returns>
    /// The factorial of the specified number. If the number is 0, the method returns 1.
    /// </returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when <paramref name="number"/> is less than 0.
    /// </exception>
    /// <see cref="Math"/>
    /// <seealso cref="System.Numerics.BigInteger"/>
    /// <example>
    /// <code>
    /// // Example usage:
    /// int result = Factorial(5);
    /// Console.WriteLine(result); // Output: 120
    /// </code>
    /// </example>
    Element4,

    /// <summary>
    /// This is a fifth element.
    /// Multiline comment
    /// </summary>
    /// <remarks>
    /// A fifth element
    /// For example, <c>xxx(5)</c> returns <c>120</c>.
    /// </remarks>    
    [Description("The fifth element")]
    Element5,
    // Don't care comment
    Element6,

    /// <summary>
    /// First line of multi summary.
    /// Second line of multi summary.
    /// </summary>
    Element7,
}

[VRT.Generators.EnumToClass.EnumToClass<TestElements>]
internal sealed partial class TestElementClass
{
}

[VRT.Generators.EnumToClass.EnumToClass<TestElements>(WithDescription = true)]
public sealed partial record TestElementRecord
{
}

[VRT.Generators.EnumToClass.EnumToClass<HttpResponseHeader>]
public sealed partial class HttpResponseHeaderClass
{
}

/// <summary>
/// First member is not default(0); default is Zero at the end.
/// </summary>
public enum NonZeroFirstElements
{
    Alpha = 1,
    Beta = 2,
    Zero = 0,
}

[VRT.Generators.EnumToClass.EnumToClass<NonZeroFirstElements>]
public sealed partial class NonZeroFirstElementClass
{
}

public enum SpecialDescriptionElements
{
    [Description("He said \"hi\"\nand left")]
    Quoted,

    [Description("plain")]
    Plain = 1,
}

[VRT.Generators.EnumToClass.EnumToClass<SpecialDescriptionElements>(WithDescription = true)]
public sealed partial class SpecialDescriptionElementClass
{
}

public enum ByteBackedElements : byte
{
    None = 0,
    One = 1,
    Two = 2,
}

[VRT.Generators.EnumToClass.EnumToClass<ByteBackedElements>]
public sealed partial class ByteBackedElementClass
{
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class Metadata1Attribute : Attribute
{
    public Metadata1Attribute() { }

    public Metadata1Attribute(string label) => Label = label;

    public string? Label { get; }
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class Metadata2Attribute : Attribute
{
}

public enum MetadataElements
{
    [Metadata1("alpha")]
    [Metadata2]
    Both,

    [Metadata2]
    OnlySecond = 1,

    Bare = 2,
}

[VRT.Generators.EnumToClass.EnumToClass<MetadataElements>]
[VRT.Generators.EnumToClass.EnumToClassProperty<Metadata1Attribute>]
[VRT.Generators.EnumToClass.EnumToClassProperty<Metadata2Attribute>(Name = "Meta2")]
public sealed partial class MetadataElementClass
{
}

[AttributeUsage(AttributeTargets.Field)]
internal sealed class InternalMetaAttribute : Attribute
{
}

public enum InternalMetaElements
{
    [InternalMeta]
    A = 0,
}

[VRT.Generators.EnumToClass.EnumToClass<InternalMetaElements>]
[VRT.Generators.EnumToClass.EnumToClassProperty<InternalMetaAttribute>]
public sealed partial class InternalMetaElementClass
{
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class PermissionAttribute : Attribute
{
    public PermissionAttribute(string name) => Name = name;

    public string Name { get; }
}

public enum RoleTypes
{
    [Permission("read")]
    [Permission("write")]
    Editor = 0,

    [Permission("read")]
    Viewer = 1,

    Guest = 2,
}

[VRT.Generators.EnumToClass.EnumToClass<RoleTypes>]
[VRT.Generators.EnumToClass.EnumToClassProperty<PermissionAttribute>(AsArray = true, Name = "Permissions")]
public sealed partial class RoleTypeClass
{
}

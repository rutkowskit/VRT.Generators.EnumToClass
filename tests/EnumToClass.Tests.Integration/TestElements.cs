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

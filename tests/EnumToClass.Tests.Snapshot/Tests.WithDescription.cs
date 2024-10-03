using VRT.Generators;

namespace EnumToClass.Tests.Snapshot;

public sealed partial class Tests
{
    [Fact]
    public Task EnumToClass_WithDescription()
    {
        var sourceCode = """            
            using System.ComponentModel;            
            using VRT.Generators.EnumToClass;

            #nullable enable
            
            namespace VRT.Generators.Tests;

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
            }

            [EnumToClass<TestElements>(WithDescription = true)]
            public sealed partial record TestElementRecord;            
            """;
        return CheckSourceCode<EnumToClassGenerator>(sourceCode);
    }
}
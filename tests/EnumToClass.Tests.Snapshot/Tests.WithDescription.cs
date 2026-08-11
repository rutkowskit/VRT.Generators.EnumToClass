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

    [Fact]
    public Task EnumToClass_WithEscapedDescription()
    {
        var sourceCode = """
            using System.ComponentModel;
            using VRT.Generators.EnumToClass;

            #nullable enable

            namespace VRT.Generators.Tests;

            public enum SpecialElements
            {
                [Description("He said \"hi\"\nand left")]
                Quoted,
                [Description("plain")]
                Plain = 1,
            }

            [EnumToClass<SpecialElements>(WithDescription = true)]
            public sealed partial class SpecialElementClass;
            """;
        return CheckSourceCode<EnumToClassGenerator>(sourceCode);
    }

    [Fact]
    public Task EnumToClass_NonZeroFirstMember()
    {
        var sourceCode = """
            using VRT.Generators.EnumToClass;

            #nullable enable

            namespace VRT.Generators.Tests;

            public enum NonZeroFirst
            {
                Alpha = 1,
                Beta = 2,
                Zero = 0,
            }

            [EnumToClass<NonZeroFirst>]
            public sealed partial class NonZeroFirstClass;
            """;
        return CheckSourceCode<EnumToClassGenerator>(sourceCode);
    }

    [Fact]
    public Task EnumToClass_NonPartial_ReportsETC001()
    {
        var sourceCode = """
            using VRT.Generators.EnumToClass;

            #nullable enable

            namespace VRT.Generators.Tests;

            public enum SimpleEnum
            {
                A,
                B,
            }

            [EnumToClass<SimpleEnum>]
            public sealed class NotPartialClass
            {
            }
            """;
        return CheckSourceCode<EnumToClassGenerator>(sourceCode);
    }

    [Fact]
    public Task EnumToClass_DuplicateAttributeProperty_ReportsETC011()
    {
        var sourceCode = """
            using System;
            using VRT.Generators.EnumToClass;

            #nullable enable

            namespace VRT.Generators.Tests;

            [AttributeUsage(AttributeTargets.Field)]
            public sealed class MetaAttribute : Attribute { }

            public enum E { [Meta] A }

            [EnumToClass<E>]
            [EnumToClassProperty<MetaAttribute>]
            [EnumToClassProperty<MetaAttribute>]
            public sealed partial class Host;
            """;
        return CheckSourceCode<EnumToClassGenerator>(sourceCode);
    }

    [Fact]
    public Task EnumToClass_AttributeProperties()
    {
        var sourceCode = """
            using System;
            using VRT.Generators.EnumToClass;

            #nullable enable

            namespace VRT.Generators.Tests;

            [AttributeUsage(AttributeTargets.Field)]
            public sealed class Metadata1Attribute : Attribute
            {
                public Metadata1Attribute(string label) => Label = label;
                public string Label { get; }
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

            [EnumToClass<MetadataElements>]
            [EnumToClassProperty<Metadata1Attribute>]
            [EnumToClassProperty<Metadata2Attribute>(Name = "Meta2")]
            public sealed partial class MetadataElementClass;
            """;
        return CheckSourceCode<EnumToClassGenerator>(sourceCode);
    }

    [Fact]
    public Task EnumToClass_NestedType_ReportsETC003()
    {
        var sourceCode = """
            using VRT.Generators.EnumToClass;

            #nullable enable

            namespace VRT.Generators.Tests;

            public enum NestedHostEnum
            {
                A,
                B,
            }

            public sealed class OuterType
            {
                [EnumToClass<NestedHostEnum>]
                public sealed partial class NestedSmartEnum
                {
                }
            }
            """;
        return CheckSourceCode<EnumToClassGenerator>(sourceCode);
    }
}
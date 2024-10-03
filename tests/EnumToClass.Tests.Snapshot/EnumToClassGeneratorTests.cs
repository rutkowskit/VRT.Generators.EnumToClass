using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using VRT.Generators;

namespace EnumToClass.Tests.Snapshot;

public class EnumToClassGeneratorTests
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

    private static async Task CheckSourceCode<T>(string sourceText,
        [CallerMemberName] string? callerName = null,
        CancellationToken cancellationToken = default)
        where T : IIncrementalGenerator, new()
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            sourceText,
            options: new CSharpParseOptions(LanguageVersion.Preview),
            cancellationToken: cancellationToken);

        var runtimeAssemblyName = @"System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
        var coreLibAssemblyName = @"System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
        var primitivesLibAssemblyName = @"System.ComponentModel.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";


        var runtimeAssembly = Assembly.Load(runtimeAssemblyName);
        var coreLibAssembly = Assembly.Load(coreLibAssemblyName);
        var primitivesLibAssembly = Assembly.Load(primitivesLibAssemblyName);

        var references = new[]
        {
            MetadataReference.CreateFromFile(runtimeAssembly.Location),
            MetadataReference.CreateFromFile(coreLibAssembly.Location),
            MetadataReference.CreateFromFile(primitivesLibAssembly.Location),
            //MetadataReference.CreateFromFile(typeof(DescriptionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        // Define the assembly version
        //var assemblyVersion = "1.0.0.0";

        // Create compilation options
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
            {
                { "CS1701", ReportDiagnostic.Suppress },  // Suppress assembly version mismatch warnings
                { "CS1747", ReportDiagnostic.Suppress },  // Suppress assembly version mismatch warnings
                { "CS8019" , ReportDiagnostic.Suppress}, // unneccecery usings
            })
            .WithAllowUnsafe(true)
            .WithAssemblyIdentityComparer(AssemblyIdentityComparer.Default);

        Compilation compilation = CSharpCompilation
            .Create("SnapshotTests")
            .AddSyntaxTrees(syntaxTree)
            .WithOptions(compilationOptions)
            .AddReferences(references)
            ;

        var generator = new T();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));

        driver = driver
            .RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _, cancellationToken);
        var diagnostics = compilation.GetDiagnostics(cancellationToken);

        await Task.WhenAll(
            Verify(NormalizeLocations(diagnostics))
                .UseDirectory($"Snapshots/{callerName}")
                .UseTextForParameters("Diagnostics"),
            Verify(driver)
                .UseDirectory($"Snapshots/{callerName}")
                .UseFileName("_"));
    }
    private static ImmutableArray<Diagnostic> NormalizeLocations(ImmutableArray<Diagnostic> diagnostics)
    {
        return diagnostics
            .Select(NormalizeLocation)
            .ToImmutableArray();
    }
    private static Diagnostic NormalizeLocation(Diagnostic diagnostic)
    {
        if (diagnostic.Location.ToString().Contains('\\') is false)
        {
            return diagnostic;
        }
        var newLocation = Location.Create(diagnostic.Location.GetLineSpan().Path.Replace('\\', '/'),
            diagnostic.Location.SourceSpan,
            diagnostic.Location.GetLineSpan().Span);

        var result = Diagnostic.Create(diagnostic.Descriptor, newLocation, diagnostic.AdditionalLocations, diagnostic.Properties);
        return result;
    }
}
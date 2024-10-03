﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace VRT.Generators;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[Generator]
public class EnumToClassGenerator : IIncrementalGenerator
{
    private const string EndOfLine = "\r\n";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // _ = System.Diagnostics.Debugger.Launch();
        // add attribute source code to calling assembly        
        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddSource(
                hintName: $"{EnumToClassAttributeDefinition.AttributeTypeName}.g.cs",
                source: EnumToClassAttributeDefinition.SourceCode);
        });

        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: EnumToClassAttributeDefinition.FullyQualifiedName,
            predicate: static (syntaxNode, _) => syntaxNode is TypeDeclarationSyntax,
            transform: static (context, _) =>
            {
                var classDecl = context.TargetSymbol as INamedTypeSymbol;
                return EnumToClassData.FromClass(classDecl);
            })
            .Where(n => n is not null);

        // Generate the partial class for each matched class
        context.RegisterSourceOutput(pipeline, static (context, model) =>
        {
            var classSource = GenerateCodeForEnumMembers(model!);
            context.AddSource($"{model!.ClassName}_Constants.g.cs", SourceText.From(classSource, Encoding.UTF8));

            var constructorSource = GeneratePropertiesAndConstructor(model);
            context.AddSource($"{model.ClassName}_Constructors.g.cs", SourceText.From(constructorSource, Encoding.UTF8));
        });
    }

    private static string GeneratePropertiesAndConstructor(EnumToClassData data)
    {
        var code = $$"""
            //<auto-generated />                        
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using System.Linq;

            #nullable enable
            namespace {{data.ClassNamespace}}
            {
                {{data.ClassPartialDeclaration}}            
                {                    
                    {{data.GetConstructorDeclaration()}}
                    {
                        Name = value.ToString();
                        IsEmpty = value == default({{data.EnumTypeFullName}});
                        Value = value;
                        {{data.GetDescriptionFieldInitialization()}}
                    }
                    {{data.GetEmptyItemDefinition()}}
                    public string Name { get; }
                    public {{data.EnumTypeFullName}} Value { get; }
                    public bool IsEmpty { get; }                    

                    {{data.GetDescriptionFieldDeclaration()}}                                        
                    {{GenerateEqualityComparer(data)}}

                    public override string ToString() => Name;    
                    
                    public static IReadOnlyCollection<{{data.ClassName}}> GetAll() => ValueByNameMap.Values;
                        
                    public static IEnumerable<{{data.ClassName}}> GetByName(IEnumerable<string> names)
                        => names.Select(GetByName).Where(p => p.IsEmpty == false);

                    public static {{data.ClassName}} GetByName(string name)
                    {
                        return ValueByNameMap.TryGetValue(name, out var value)
                            ? value
                            : Empty;
                    }
                    public static implicit operator {{data.ClassName}}(string name) => GetByName(name);
                    public static implicit operator string({{data.ClassName}} value) => value.Name;
                    public static implicit operator {{data.EnumTypeFullName}}({{data.ClassName}} value) => value.Value;
                    public static implicit operator {{data.ClassName}}({{data.EnumTypeFullName}} value) => GetByName(value.ToString());
                    public static implicit operator {{data.EnumTypeUnderlyingTypeName}}({{data.ClassName}} value) => ({{data.EnumTypeUnderlyingTypeName}}) value.Value;
                }
            }
            """;
        return code;
    }
    private static string GenerateEqualityComparer(EnumToClassData classSymbol)
    {
        return classSymbol switch
        {
            { IsRecord: true } => "",
            _ => $$"""
                   public override bool Equals(object? obj)
                   {
                       return obj is {{classSymbol.ClassName}} element
                           ? element.Value == Value
                           : base.Equals(obj);
                   }
                   public override int GetHashCode() => Value.GetHashCode();
                   """
        };
    }
    private static string GenerateCodeForEnumMembers(EnumToClassData data)
    {
        string className = data.ClassName;

        return $$"""
            //<auto-generated />     
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            #nullable enable

            namespace {{data.ClassNamespace}}
            {
                {{data.ClassPartialDeclaration}}            
                {
                    private static readonly ReadOnlyDictionary<string, {{className}}> ValueByNameMap = new ReadOnlyDictionary<string, {{className}}>(new Dictionary<string, {{className}}>()
                    {
                        {{string.Join($",{EndOfLine}            ", ToDictionaryEntries(data))}}
                    });
                    {{string.Join($"{EndOfLine}        ", ToConstDeclaration(data))}}
                }
            }
            """;
    }
    private static IEnumerable<string> ToDictionaryEntries(EnumToClassData data)
    {
        foreach (var member in data.EnumFields)
        {
            yield return $"""["{member.Name}"] = {data.GetClassContruction(member)}""";
        }
    }
    private static IEnumerable<string> ToConstDeclaration(EnumToClassData data)
    {
        foreach (var member in data.EnumFields)
        {
            yield return member.DocumentationComment ?? "";
            yield return $"public const string {member.Name} = \"{member.Name}\";";
        }
    }
}

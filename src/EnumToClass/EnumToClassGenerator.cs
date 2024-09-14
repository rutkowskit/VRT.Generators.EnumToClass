﻿using EnumToClass;
using EnumToClass.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Text;

namespace VRT.Generators;

[Generator]
public class EnumToClassGenerator : IIncrementalGenerator
{
    private static readonly Type AttributeType = typeof(EnumToClass.EnumToClassAttribute<>);
    private static readonly string AttributeTypeName = AttributeType.GetSimpleTypeName();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // System.Diagnostics.Debugger.Launch();
        // Gather all classes that have the GenerateConstants attribute

        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddSource(
                hintName: $"{AttributeTypeName}.g.cs",
                source: Assembly.GetExecutingAssembly().GetString($"{AttributeTypeName}.cs"));
        });

        var classesWithAttribute = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => HasExpectedAttribute(s),
                transform: static (ctx, _) => (TypeDeclarationSyntax)ctx.Node)
            .Where(static classDecl => classDecl != null)
            .Select((classDecl, _) => classDecl)
            .Combine(context.CompilationProvider)
            .Select((pair, ct) =>
            {
                var (classDecl, compilation) = pair;
                var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(classDecl, ct) as INamedTypeSymbol;

                var attributeData = classSymbol?.GetAttributes()
                    .Where(a => a.AttributeClass != null)
                    .Where(a => a.AttributeClass!.ContainingNamespace.ToDisplayString() == AttributeType.Namespace)
                    .FirstOrDefault(a => a.AttributeClass!.Name == AttributeTypeName);

                if (attributeData?.AttributeClass != null && attributeData.AttributeClass.TypeArguments.Length > 0)
                {
                    return (classSymbol, enumType: attributeData.AttributeClass.TypeArguments[0]);
                }
                return (null!, null!);
            })
            .Where(pair => pair.classSymbol != null);

        // Generate the partial class for each matched class
        context.RegisterSourceOutput(classesWithAttribute, (spc, pair) =>
        {
            var (classSymbol, enumType) = pair;
            if (enumType != null && classSymbol != null)
            {
                var enumMembers = enumType.GetMembers()
                    .Where(m => m.Kind == SymbolKind.Field)
                    .Cast<IFieldSymbol>();

                var classSource = GeneratePartialClass(classSymbol, enumMembers);
                spc.AddSource($"{classSymbol.Name}_Constants.g.cs", SourceText.From(classSource, Encoding.UTF8));

                var constructorSource = GeneratePropertiesAndConstructor(classSymbol, enumType);
                spc.AddSource($"{classSymbol.Name}_Constructors.g.cs", SourceText.From(constructorSource, Encoding.UTF8));
            }
        });
    }
    private static bool HasExpectedAttribute(SyntaxNode syntaxNode)
    {
        return syntaxNode switch
        {
            ClassDeclarationSyntax classDecl when classDecl.AttributeLists.Count > 0 => true,
            RecordDeclarationSyntax recordDecl when recordDecl.AttributeLists.Count > 0 => true,
            _ => false
        };
    }
    private static string GeneratePropertiesAndConstructor(INamedTypeSymbol classSymbol, ITypeSymbol enumType)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        var fullEnumTypeName = string.IsNullOrWhiteSpace(enumType.ContainingNamespace.Name)
            ? enumType.Name
            : $"{enumType.ContainingNamespace.Name}.{enumType.Name}";
        var code = $$"""
            //<auto-generated />            
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using System.Linq;

            #nullable enable

            namespace {{namespaceName}}
            {
                {{GetPartialDeclaration(classSymbol)}}            
                {
                    private static readonly ReadOnlyDictionary<string, {{className}}> ValueByNameMap = Enum
                        .GetValues<{{fullEnumTypeName}}>()
                        .Select(p => new {{className}}(p))
                        .ToDictionary(p => p.Name, p => p)
                        .AsReadOnly();
                    private {{className}}({{fullEnumTypeName}} value)
                    {
                        Name = value.ToString();
                        IsEmpty = value == default({{fullEnumTypeName}});
                        Value = value;
                    }
                    public string Name { get; }
                    public {{fullEnumTypeName}} Value { get; }
                    public bool IsEmpty { get; }                            

                    public static {{className}} Empty { get; } = new {{className}}(default({{fullEnumTypeName}}));
                    public override string ToString() => Name;
                    
                    {{GenerateEqualityComparer(classSymbol)}}
                   
                    public static IEnumerable<{{className}}> GetByName(IEnumerable<string> names)
                        => names.Select(GetByName).Where(p => p.IsEmpty == false);

                    public static {{className}} GetByName(string name)
                    {
                        return ValueByNameMap.TryGetValue(name, out var value)
                            ? value
                            : Empty;
                    }
                    public static implicit operator {{className}}(string name) => GetByName(name);
                    public static implicit operator string({{className}} value) => value.Name;
                    public static implicit operator {{fullEnumTypeName}}({{className}} value) => value.Value;
                    public static implicit operator {{className}}({{fullEnumTypeName}} value) => GetByName(value.ToString());
                }
            }
            """;
        return code;
    }
    private static string GenerateEqualityComparer(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.IsRecord)
        {
            return "";
        }
        return $$"""
                    public override bool Equals(object? obj)
                    {
                        return obj is {{classSymbol.Name}} element
                            ? element.Value == Value
                            : base.Equals(obj);
                    }
                    public override int GetHashCode() => Value.GetHashCode();
            """;
    }
    private static string GeneratePartialClass(INamedTypeSymbol classSymbol, IEnumerable<IFieldSymbol> enumMembers)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        return $$"""
            //<auto-generated />     
            #nullable enable

            namespace {{namespaceName}}
            {
                {{GetPartialDeclaration(classSymbol)}}            
                {
                    {{string.Join("\r\n        ", ToConstDeclaration(enumMembers))}}
                }
            }
            """;
    }
    private static IEnumerable<string> ToConstDeclaration(IEnumerable<IFieldSymbol> fieldSymbols)
    {
        foreach (var member in fieldSymbols)
        {
            var constName = member.Name;
            var constValue = member.Name;
            yield return $"public const string {constName} = \"{constValue}\";";
        }
    }
    private static string GetPartialDeclaration(INamedTypeSymbol symbol)
        => $"{symbol.GetAccessibility()} partial {(symbol.IsRecord ? "record" : "class")} {symbol.Name}";
}

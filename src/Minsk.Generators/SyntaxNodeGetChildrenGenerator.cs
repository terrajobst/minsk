using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Minsk.Generators
{
    [Generator]
    public class SyntaxNodeGetChildrenGenerator : ISourceGenerator
    {
        public void Initialize(InitializationContext context)
        {
        }

        public void Execute(SourceGeneratorContext context)
        {
#pragma warning disable IDE0063 // Use simple 'using' statement: we want to control when the 'using' varibales go out of scope
            SourceText sourceText;

            CSharpCompilation? compilation = (CSharpCompilation)context.Compilation;

            INamedTypeSymbol? immutableArrayType = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1");
            INamedTypeSymbol? separatedSyntaxListType = compilation.GetTypeByMetadataName("Minsk.CodeAnalysis.Syntax.SeparatedSyntaxList`1");
            INamedTypeSymbol? syntaxNodeType = compilation.GetTypeByMetadataName("Minsk.CodeAnalysis.Syntax.SyntaxNode");

            if (immutableArrayType == null || separatedSyntaxListType == null || syntaxNodeType == null)
            {
                return;
            }

            IReadOnlyList<INamedTypeSymbol>? types = GetAllTypes(compilation.Assembly);
            IEnumerable<INamedTypeSymbol>? syntaxNodeTypes = types.Where(t => !t.IsAbstract && IsPartial(t) && IsDerivedFrom(t, syntaxNodeType));

            string indentString = "    ";
            using (StringWriter? stringWriter = new StringWriter())
            using (IndentedTextWriter? indentedTextWriter = new IndentedTextWriter(stringWriter, indentString))
            {
                indentedTextWriter.WriteLine("using System;");
                indentedTextWriter.WriteLine("using System.Collections.Generic;");
                indentedTextWriter.WriteLine("using System.Collections.Immutable;");
                indentedTextWriter.WriteLine();
                using (CurlyIndenter? nameSpaceCurly = new CurlyIndenter(indentedTextWriter, "namespace Minsk.CodeAnalysis.Syntax"))
                {
                    foreach (INamedTypeSymbol? type in syntaxNodeTypes)
                    {
                        using (CurlyIndenter? classCurly = new CurlyIndenter(indentedTextWriter, $"partial class {type.Name}"))
                        using (CurlyIndenter? getChildCurly = new CurlyIndenter(indentedTextWriter, "public override IEnumerable<SyntaxNode> GetChildren()"))
                        {
                            foreach (IPropertySymbol? property in type.GetMembers().OfType<IPropertySymbol>())
                            {
                                if (property.Type is INamedTypeSymbol propertyType)
                                {
                                    if (IsDerivedFrom(propertyType, syntaxNodeType))
                                    {
                                        bool canBeNull = property.NullableAnnotation == NullableAnnotation.Annotated;
                                        if (canBeNull)
                                        {
                                            indentedTextWriter.WriteLine($"if ({property.Name} != null)");
                                            indentedTextWriter.Indent++;
                                        }

                                        indentedTextWriter.WriteLine($"yield return {property.Name};");

                                        if (canBeNull)
                                        {
                                            indentedTextWriter.Indent--;
                                        }
                                    }
                                    else if (propertyType.TypeArguments.Length == 1 &&
                                             IsDerivedFrom(propertyType.TypeArguments[0], syntaxNodeType) &&
                                             SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, immutableArrayType))
                                    {
                                        indentedTextWriter.WriteLine($"foreach (var child in {property.Name})");
                                        indentedTextWriter.WriteLine($"{indentString}yield return child;");

                                    }
                                    else if (SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, separatedSyntaxListType) &&
                                             IsDerivedFrom(propertyType.TypeArguments[0], syntaxNodeType))
                                    {
                                        indentedTextWriter.WriteLine($"foreach (var child in {property.Name}.GetWithSeparators())");
                                        indentedTextWriter.WriteLine($"{indentString}yield return child;");
                                    }
                                }
                            }
                        }
                    }
                }

                indentedTextWriter.Flush();
                stringWriter.Flush();

                sourceText = SourceText.From(stringWriter.ToString(), Encoding.UTF8);
            }

            string? hintName = "SyntaxNode_GetChildren.g.cs";
            context.AddSource(hintName, sourceText);

            // HACK
            //
            // Make generator work in VS Code. See src\Directory.Build.props for
            // details.

            string? fileName = "SyntaxNode_GetChildren.g.cs";
            string? syntaxNodeFilePath = syntaxNodeType.DeclaringSyntaxReferences.First().SyntaxTree.FilePath;
            string? syntaxDirectory = Path.GetDirectoryName(syntaxNodeFilePath);
            string? filePath = Path.Combine(syntaxDirectory, fileName);

            if (File.Exists(filePath))
            {
                string? fileText = File.ReadAllText(filePath);
                SourceText? sourceFileText = SourceText.From(fileText, Encoding.UTF8);
                if (sourceText.ContentEquals(sourceFileText))
                {
                    return;
                }
            }

            using (StreamWriter? writer = new StreamWriter(filePath))
            {
                sourceText.Write(writer);
            }
#pragma warning restore IDE0063 // Use simple 'using' statement: we want to control when the variable goes out of scope
        }

        private IReadOnlyList<INamedTypeSymbol> GetAllTypes(IAssemblySymbol symbol)
        {
            List<INamedTypeSymbol>? result = new List<INamedTypeSymbol>();
            GetAllTypes(result, symbol.GlobalNamespace);
            result.Sort((x, y) => x.MetadataName.CompareTo(y.MetadataName));
            return result;
        }

        private void GetAllTypes(List<INamedTypeSymbol> result, INamespaceOrTypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol type)
            {
                result.Add(type);
            }

            foreach (ISymbol? child in symbol.GetMembers())
            {
                if (child is INamespaceOrTypeSymbol nsChild)
                {
                    GetAllTypes(result, nsChild);
                }
            }
        }

        private static bool IsDerivedFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            ITypeSymbol? current = type;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        private static bool IsPartial(INamedTypeSymbol type)
        {
            foreach (SyntaxReference? declaration in type.DeclaringSyntaxReferences)
            {
                SyntaxNode? syntax = declaration.GetSyntax();
                if (syntax is TypeDeclarationSyntax typeDeclaration)
                {
                    foreach (SyntaxToken modifer in typeDeclaration.Modifiers)
                    {
                        if (modifer.ValueText == "partial")
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}

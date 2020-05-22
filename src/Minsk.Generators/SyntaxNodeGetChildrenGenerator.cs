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

            var compilation = (CSharpCompilation)context.Compilation;

            var immutableArrayType = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1");
            var separatedSyntaxListType = compilation.GetTypeByMetadataName("Minsk.CodeAnalysis.Syntax.SeparatedSyntaxList`1");
            var syntaxNodeType = compilation.GetTypeByMetadataName("Minsk.CodeAnalysis.Syntax.SyntaxNode");

            if (immutableArrayType == null || separatedSyntaxListType == null || syntaxNodeType == null)
                return;

            var types = GetAllTypes(compilation.Assembly);
            var syntaxNodeTypes = types.Where(t => !t.IsAbstract && IsPartial(t) && IsDerivedFrom(t, syntaxNodeType));

            string indentString = "    ";
            using (var stringWriter = new StringWriter())
            using (var indentedTextWriter = new IndentedTextWriter(stringWriter, indentString))
            {
                indentedTextWriter.WriteLine("using System;");
                indentedTextWriter.WriteLine("using System.Collections.Generic;");
                indentedTextWriter.WriteLine("using System.Collections.Immutable;");
                indentedTextWriter.WriteLine();
                using (var nameSpaceCurly = new CurlyIndenter(indentedTextWriter, "namespace Minsk.CodeAnalysis.Syntax"))
                {
                    foreach (var type in syntaxNodeTypes)
                    {
                        using (var classCurly = new CurlyIndenter(indentedTextWriter, $"partial class {type.Name}"))
                        using (var getChildCurly = new CurlyIndenter(indentedTextWriter, "public override IEnumerable<SyntaxNode> GetChildren()"))
                        {
                            foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
                            {
                                if (property.Type is INamedTypeSymbol propertyType)
                                {
                                    if (IsDerivedFrom(propertyType, syntaxNodeType))
                                    {
                                        var canBeNull = property.NullableAnnotation == NullableAnnotation.Annotated;
                                        if (canBeNull)
                                        {
                                            indentedTextWriter.WriteLine($"if ({property.Name} != null)");
                                            indentedTextWriter.Indent++;
                                        }

                                        indentedTextWriter.WriteLine($"yield return {property.Name};");

                                        if (canBeNull)
                                            indentedTextWriter.Indent--;
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

            var hintName = "SyntaxNode_GetChildren.g.cs";
            context.AddSource(hintName, sourceText);

            // HACK
            //
            // Make generator work in VS Code. See src\Directory.Build.props for
            // details.

            var fileName = "SyntaxNode_GetChildren.g.cs";
            var syntaxNodeFilePath = syntaxNodeType.DeclaringSyntaxReferences.First().SyntaxTree.FilePath;
            var syntaxDirectory = Path.GetDirectoryName(syntaxNodeFilePath);
            var filePath = Path.Combine(syntaxDirectory, fileName);

            if (File.Exists(filePath))
            {
                var fileText = File.ReadAllText(filePath);
                var sourceFileText = SourceText.From(fileText, Encoding.UTF8);
                if (sourceText.ContentEquals(sourceFileText))
                    return;
            }

            using (var writer = new StreamWriter(filePath))
                sourceText.Write(writer);
#pragma warning restore IDE0063 // Use simple 'using' statement: we want to control when the variable goes out of scope
        }

        private IReadOnlyList<INamedTypeSymbol> GetAllTypes(IAssemblySymbol symbol)
        {
            var result = new List<INamedTypeSymbol>();
            GetAllTypes(result, symbol.GlobalNamespace);
            result.Sort((x, y) => x.MetadataName.CompareTo(y.MetadataName));
            return result;
        }

        private void GetAllTypes(List<INamedTypeSymbol> result, INamespaceOrTypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol type)
                result.Add(type);

            foreach (var child in symbol.GetMembers())
                if (child is INamespaceOrTypeSymbol nsChild)
                    GetAllTypes(result, nsChild);
        }

        private bool IsDerivedFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            var current = type;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;

                current = current.BaseType;
            }

            return false;
        }

        private bool IsPartial(INamedTypeSymbol type)
        {
            foreach (var declaration in type.DeclaringSyntaxReferences)
            {
                var syntax = declaration.GetSyntax();
                if (syntax is TypeDeclarationSyntax typeDeclaration)
                {
                    foreach (var modifer in typeDeclaration.Modifiers)
                    {
                        if (modifer.ValueText == "partial")
                            return true;
                    }
                }
            }

            return false;
        }
    }
}

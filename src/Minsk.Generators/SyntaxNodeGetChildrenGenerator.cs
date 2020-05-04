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
            SourceText sourceText;

            var compilation = (CSharpCompilation)context.Compilation;

            var immutableArrayType = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1");
            var separatedSyntaxListType = compilation.GetTypeByMetadataName("Minsk.CodeAnalysis.Syntax.SeparatedSyntaxList`1");
            var syntaxNodeType = compilation.GetTypeByMetadataName("Minsk.CodeAnalysis.Syntax.SyntaxNode");

            var types = GetAllTypes(compilation.Assembly);
            var syntaxNodeTypes = types.Where(t => !t.IsAbstract && IsPartial(t) && IsDerivedFrom(t, syntaxNodeType));

            using (var stringWriter = new StringWriter())
            using (var indentedTextWriter = new IndentedTextWriter(stringWriter, "    "))
            {
                indentedTextWriter.WriteLine("using System;");
                indentedTextWriter.WriteLine("using System.Collections.Generic;");
                indentedTextWriter.WriteLine("using System.Collections.Immutable;");
                indentedTextWriter.WriteLine();
                indentedTextWriter.WriteLine("namespace Minsk.CodeAnalysis.Syntax");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;

                foreach (var type in syntaxNodeTypes)
                {
                    indentedTextWriter.WriteLine($"partial class {type.Name}");
                    indentedTextWriter.WriteLine("{");
                    indentedTextWriter.Indent++;

                    indentedTextWriter.WriteLine("public override IEnumerable<SyntaxNode> GetChildren()");
                    indentedTextWriter.WriteLine("{");
                    indentedTextWriter.Indent++;

                    foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
                    {
                        if (property.Type is INamedTypeSymbol propertyType)
                        {
                            if (IsDerivedFrom(propertyType, syntaxNodeType))
                            {
                                indentedTextWriter.WriteLine($"yield return {property.Name};");
                            }
                            else if (propertyType.TypeArguments.Length == 1 &&
                                     IsDerivedFrom(propertyType.TypeArguments[0], syntaxNodeType) &&
                                     SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, immutableArrayType))
                            {
                                indentedTextWriter.WriteLine($"foreach (var child in {property.Name})");
                                indentedTextWriter.Indent++;
                                indentedTextWriter.WriteLine("yield return child;");
                                indentedTextWriter.Indent--;
                            }
                            else if (SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, separatedSyntaxListType) &&
                                     IsDerivedFrom(propertyType.TypeArguments[0], syntaxNodeType))
                            {
                                indentedTextWriter.WriteLine($"foreach (var child in {property.Name}.GetWithSeparators())");
                                indentedTextWriter.Indent++;
                                indentedTextWriter.WriteLine("yield return child;");
                                indentedTextWriter.Indent--;
                            }
                        }
                    }

                    indentedTextWriter.Indent--;
                    indentedTextWriter.WriteLine("}");

                    indentedTextWriter.Indent--;
                    indentedTextWriter.WriteLine("}");
                }

                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");

                indentedTextWriter.Flush();
                stringWriter.Flush();

                sourceText = SourceText.From(stringWriter.ToString(), Encoding.UTF8);
            }

            var hintName = "SyntaxNode_GetChildren.g.cs";
            context.AddSource(hintName, sourceText);

            // HACK: Partially Fixes DesignTime Builds. See terrajobst/minsk#121
#if DEBUG
            var fileName = "SyntaxNode_GetChildren.g.cs";
            var syntaxNodeFilePath = syntaxNodeType.DeclaringSyntaxReferences.First().SyntaxTree.FilePath;
            var syntaxDirectory = Path.GetDirectoryName(syntaxNodeFilePath);
            var filePath = Path.Combine(syntaxDirectory, fileName);

            if(File.Exists(filePath))
            {
                var fileText = File.ReadAllText(filePath);
                var sourceFileText = SourceText.From(fileText, Encoding.UTF8);
                if(sourceText.ContentEquals(sourceFileText)) return;
            }
            using (var writer = new StreamWriter(filePath))
                sourceText.Write(writer);
#endif
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
            while (type != null)
            {
                if (SymbolEqualityComparer.Default.Equals(type, baseType))
                    return true;

                type = type.BaseType;
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

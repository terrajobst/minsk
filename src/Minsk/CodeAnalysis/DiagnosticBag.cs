using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;
using Minsk.Resources;
using Mono.Cecil;

namespace Minsk.CodeAnalysis
{
    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddRange(IEnumerable<Diagnostic> diagnostics)
        {
            _diagnostics.AddRange(diagnostics);
        }

        private void Report(TextLocation location, string message)
        {
            var diagnostic = Diagnostic.Error(location, message);
            _diagnostics.Add(diagnostic);
        }

        private void ReportWarning(TextLocation location, string message)
        {
            var diagnostic = Diagnostic.Warning(location, message);
            _diagnostics.Add(diagnostic);
        }

        public void ReportInvalidNumber(TextLocation location, string text, TypeSymbol type)
        {
            var message = string.Format(Strings.Diagnostic_InvalidNumber, text, type);
            Report(location, message);
        }

        public void ReportBadCharacter(TextLocation location, char character)
        {
            var message = string.Format(Strings.Diagnostic_BadCharacter, character);
            Report(location, message);
        }

        public void ReportUnterminatedString(TextLocation location)
        {
           var x = Strings.ResourceManager.GetResourceSet(CultureInfo.CurrentCulture, false, true);
            var message = Strings.Diagnostic_UnterminatedString;
            Report(location, message);
        }

        public void ReportUnterminatedMultiLineComment(TextLocation location)
        {
            var message = Strings.Diagnostic_UnterminatedMultiLineComment;
            Report(location, message);
        }

        public void ReportUnexpectedToken(TextLocation location, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            var message = string.Format(Strings.Diagnostic_UnexpectedToken, actualKind, expectedKind);
            Report(location, message);
        }

        public void ReportUndefinedUnaryOperator(TextLocation location, string operatorText, TypeSymbol operandType)
        {
            var message = string.Format(Strings.Diagnostic_UndefinedUnaryOperator, operatorText, operandType);
            Report(location, message);
        }

        public void ReportUndefinedBinaryOperator(TextLocation location, string operatorText, TypeSymbol leftType,
            TypeSymbol rightType)
        {
            var message = string.Format(Strings.Diagnostic_UndefinedBinaryOperator, operatorText, leftType, rightType);
            Report(location, message);
        }

        public void ReportParameterAlreadyDeclared(TextLocation location, string parameterName)
        {
            var message = string.Format(Strings.Diagnostic_ParameterAlreadyDeclared, parameterName);
            Report(location, message);
        }

        public void ReportUndefinedVariable(TextLocation location, string name)
        {
            var message = string.Format(Strings.Diagnostic_UndefinedVariable, name);
            Report(location, message);
        }

        public void ReportNotAVariable(TextLocation location, string name)
        {
            var message = string.Format(Strings.Diagnostic_NotAVariable, name);
            Report(location, message);
        }

        public void ReportUndefinedType(TextLocation location, string name)
        {
            var message = string.Format(Strings.Diagnostic_UndefinedType, name);
            Report(location, message);
        }

        public void ReportCannotConvert(TextLocation location, TypeSymbol fromType, TypeSymbol toType)
        {
            var message = string.Format(Strings.Diagnostic_CannotConvert, fromType, toType);
            Report(location, message);
        }

        public void ReportCannotConvertImplicitly(TextLocation location, TypeSymbol fromType, TypeSymbol toType)
        {
            var message =
                string.Format(Strings.Diagnostic_CannotConvertImplicitly, fromType, toType);
            Report(location, message);
        }

        public void ReportSymbolAlreadyDeclared(TextLocation location, string name)
        {
            var message = string.Format(Strings.Diagnostic_SymbolAlreadyDeclared, name);
            Report(location, message);
        }

        public void ReportCannotAssign(TextLocation location, string name)
        {
            var message = string.Format(Strings.Diagnostic_CannotAssign, name);
            Report(location, message);
        }

        public void ReportUndefinedFunction(TextLocation location, string name)
        {
            var message = string.Format(Strings.Diagnostic_UndefinedFunction, name);
            Report(location, message);
        }

        public void ReportNotAFunction(TextLocation location, string name)
        {
            var message = string.Format(Strings.Diagnostic_NotAFunction, name);
            Report(location, message);
        }

        public void ReportWrongArgumentCount(TextLocation location, string name, int expectedCount, int actualCount)
        {
            var message = string.Format(Strings.Diagnostic_WrongArgumentCount, name, expectedCount, actualCount);
            Report(location, message);
        }

        public void ReportExpressionMustHaveValue(TextLocation location)
        {
            var message = Strings.Diagnostic_ExpressionMustHaveValue;
            Report(location, message);
        }

        public void ReportInvalidBreakOrContinue(TextLocation location, string text)
        {
            var message = string.Format(Strings.Diagnostic_InvalidBreakOrContinue, text);
            Report(location, message);
        }

        public void ReportAllPathsMustReturn(TextLocation location)
        {
            var message = Strings.Diagnostic_AllPathsMustReturn;
            Report(location, message);
        }

        public void ReportInvalidReturnExpression(TextLocation location, string functionName)
        {
            var message =
                string.Format(Strings.Diagnostic_InvalidReturnExpression, functionName);
            Report(location, message);
        }

        public void ReportInvalidReturnWithValueInGlobalStatements(TextLocation location)
        {
            var message = Strings.Diagnostic_InvalidReturnWithValueInGlobalStatements;
            Report(location, message);
        }

        public void ReportMissingReturnExpression(TextLocation location, TypeSymbol returnType)
        {
            var message = string.Format(Strings.Diagnostic_MissingReturnExpression, returnType);
            Report(location, message);
        }

        public void ReportInvalidExpressionStatement(TextLocation location)
        {
            var message = Strings.Diagnostic_InvalidExpressionStatement;
            Report(location, message);
        }

        public void ReportOnlyOneFileCanHaveGlobalStatements(TextLocation location)
        {
            var message = Strings.Diagnostic_OnlyOneFileCanHaveGlobalStatements;
            Report(location, message);
        }

        public void ReportMainMustHaveCorrectSignature(TextLocation location)
        {
            var message = Strings.Diagnostic_MainMustHaveCorrectSignature;
            Report(location, message);
        }

        public void ReportCannotMixMainAndGlobalStatements(TextLocation location)
        {
            var message = Strings.Diagnostic_CannotMixMainAndGlobalStatements;
            Report(location, message);
        }

        public void ReportInvalidReference(string path)
        {
            var message = string.Format(Strings.Diagnostic_InvalidReference, path);
            Report(default, message);
        }

        public void ReportRequiredTypeNotFound(string? minskName, string metadataName)
        {
            var message = minskName == null
                ? string.Format(Strings.Diagnostic_RequiredTypeNotFound, metadataName)
                : string.Format(Strings.Diagnostic_RequiredTypeNotFoundWithMinsk, minskName, metadataName);
            Report(default, message);
        }

        public void ReportRequiredTypeAmbiguous(string? minskName, string metadataName, TypeDefinition[] foundTypes)
        {
            var assemblyNames = foundTypes.Select(t => t.Module.Assembly.Name.Name);
            var assemblyNameList = string.Join(", ", assemblyNames);
            var message = minskName == null
                ? string.Format(Strings.Diagnostic_RequiredTypeAmbiguous, metadataName, assemblyNameList)
                : string.Format(Strings.Diagnostic_RequiredTypeAmbiguousWithMinsk, minskName, metadataName, assemblyNameList);
            Report(default, message);
        }

        public void ReportRequiredMethodNotFound(string typeName, string methodName, string[] parameterTypeNames)
        {
            var parameterTypeNameList = string.Join(", ", parameterTypeNames);
            var methodDescription = $"{typeName}.{methodName}({parameterTypeNameList})";
            var message =
                string.Format(Strings.Diagnostic_RequiredMethodNotFound, methodDescription);
            Report(default, message);
        }

        public void ReportUnreachableCode(TextLocation location)
        {
            var message = Strings.Diagnostic_UnreachableCode;
            ReportWarning(location, message);
        }

        public void ReportUnreachableCode(SyntaxNode node)
        {
            switch (node.Kind)
            {
                case SyntaxKind.BlockStatement:
                    var firstStatement = ((BlockStatementSyntax) node).Statements.FirstOrDefault();
                    // Report just for non empty blocks.
                    if (firstStatement != null)
                        ReportUnreachableCode(firstStatement);
                    return;
                case SyntaxKind.VariableDeclaration:
                    ReportUnreachableCode(((VariableDeclarationSyntax) node).Keyword.Location);
                    return;
                case SyntaxKind.IfStatement:
                    ReportUnreachableCode(((IfStatementSyntax) node).IfKeyword.Location);
                    return;
                case SyntaxKind.WhileStatement:
                    ReportUnreachableCode(((WhileStatementSyntax) node).WhileKeyword.Location);
                    return;
                case SyntaxKind.DoWhileStatement:
                    ReportUnreachableCode(((DoWhileStatementSyntax) node).DoKeyword.Location);
                    return;
                case SyntaxKind.ForStatement:
                    ReportUnreachableCode(((ForStatementSyntax) node).Keyword.Location);
                    return;
                case SyntaxKind.BreakStatement:
                    ReportUnreachableCode(((BreakStatementSyntax) node).Keyword.Location);
                    return;
                case SyntaxKind.ContinueStatement:
                    ReportUnreachableCode(((ContinueStatementSyntax) node).Keyword.Location);
                    return;
                case SyntaxKind.ReturnStatement:
                    ReportUnreachableCode(((ReturnStatementSyntax) node).ReturnKeyword.Location);
                    return;
                case SyntaxKind.ExpressionStatement:
                    var expression = ((ExpressionStatementSyntax) node).Expression;
                    ReportUnreachableCode(expression);
                    return;
                case SyntaxKind.CallExpression:
                    ReportUnreachableCode(((CallExpressionSyntax) node).Identifier.Location);
                    return;
                default:
                    throw new Exception($"Unexpected syntax {node.Kind}");
            }
        }
    }
}
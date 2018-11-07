using System.Collections.Generic;
using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Syntax;
using Xunit;

namespace Minsk.Tests.CodeAnalysis
{
    public class EvaluationTests
    {
        [Theory]
        [InlineData("1", 1)]
        [InlineData("+1", 1)]
        [InlineData("-1", -1)]
        [InlineData("14 + 12", 26)]
        [InlineData("12 - 3", 9)]
        [InlineData("4 * 2", 8)]
        [InlineData("9 / 3", 3)]
        [InlineData("(10)", 10)]
        [InlineData("12 == 3", false)]
        [InlineData("3 == 3", true)]
        [InlineData("12 != 3", true)]
        [InlineData("3 != 3", false)]
        [InlineData("false == false", true)]
        [InlineData("true == false", false)]
        [InlineData("false != false", false)]
        [InlineData("true != false", true)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("!true", false)]
        [InlineData("!false", true)]
        [InlineData("{ var a = 0 (a = 10) * a }", 100)]
        public void Evaluator_Computes_CorrectValues(string text, object expectedValue)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var compilation = new Compilation(syntaxTree);
            var variables = new Dictionary<VariableSymbol, object>();
            var result = compilation.Evaluate(variables);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(expectedValue, result.Value);
        }
    }
}

using Xunit;

namespace Minsk.Tests.Compiler
{
    public class BuildErrorProjectTests : ProjectTestsBase
    {
        [Fact]
        public void EmptyDeclaration()
        {
            var (buildSucceeded, buildOutput) = BuildTestProject("error_emptydeclaration");
            Assert.False(buildSucceeded);

            Assert.Contains(" Unexpected token <CloseBraceToken>, expected <IdentifierToken>", buildOutput);
        }
        
        [Fact]
        public void UnterminatedString()
        {
            var (buildSucceeded, buildOutput) = BuildTestProject("error_unterminatedstring");
            Assert.False(buildSucceeded);

            Assert.Contains("Unterminated string literal", buildOutput);
        }
    }
}
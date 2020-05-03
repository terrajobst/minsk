using Xunit;

namespace Minsk.Tests.Compiler
{

    public class HelloProjectTests : ProjectTestsBase
    {
        [Fact]
        public void AsksForName()
        {           
            using var helloProcess = RunTestProject("hello");

            Assert.Equal("What's you name?", helloProcess.StandardOutput.ReadLine());
            helloProcess.StandardInput.WriteLine("Immo");
            Assert.Equal("Hello Immo!", helloProcess.StandardOutput.ReadLine());
        }
    }

}
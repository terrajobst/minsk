using System.Collections.Generic;

using Xunit;

namespace Minsk.Tests.Compiler
{
    public class IntsProjectTests : ProjectTestsBase
    {
        [Fact]
        public void Arithmetic()
        {

            var expectedOutput = new List<string>
            {
                "1 + 2 =",
                "3",
                "1 * 2 =",
                "2",
                "1 - 2 =",
                "-1",
                "1 / 2 =",
                "0",
                "1 + 1 * 2 =",
                "3",
                "(1 + 1) * 2 =",
                "4",
            };
            
            using var intProcess = RunTestProject("ints");

            foreach (var line in expectedOutput)
            {
                Assert.Equal(line, intProcess.StandardOutput.ReadLine());
            }
        }
    }

}
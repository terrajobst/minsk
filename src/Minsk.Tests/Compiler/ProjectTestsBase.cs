using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Xunit;

namespace Minsk.Tests.Compiler
{
    public abstract class ProjectTestsBase
    {
        private string _testProjectsPath = @"../../../Compiler/TestProjects";

        /// <summary>
        /// Execute dotnet build on the named TestProject
        /// </summary>
        /// <param name="project">The name of the folder and msproj</param>
        /// <param name="configuration">Defaults to Debug</param>
        protected (bool succeeded, string buildOutput) BuildTestProject(string project, string configuration = "Debug")
        {
            var projectPath = GetMinskProjectPath(project);

            using var buildProcess = new Process
            {
                StartInfo =
                {
                    FileName = "dotnet",
                    Arguments = $"build {projectPath} -c {configuration}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                }
            };


            buildProcess.Start();
            buildProcess.WaitForExit();

            var succeeded = buildProcess.ExitCode == 0;
            var buildOutput = buildProcess.StandardOutput.ReadToEnd();

            return (succeeded, buildOutput);

        }

        /// <summary>
        /// Builds the project and starts the generated executable with redirected input/output
        /// </summary>
        /// <param name="project">The name of the folder and msproj</param>
        /// <param name="configuration">Defaults to Debug</param>
        protected Process RunTestProject(string project, string configuration = "Debug")
        {
            var (buildSucceeded, buildOutput) = BuildTestProject(project);
            Assert.True(buildSucceeded, buildOutput);

            var executablePath = GetExecutablePath(project, configuration);

            var process = new Process
            {
                StartInfo =
                {
                    FileName = executablePath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                }
            };


            process.Start();
            return process;
        }

        private string GetMinskProjectPath(string project)
        {
            var path = $@"{_testProjectsPath}/{project}/{project}.msproj";
            Assert.True(File.Exists(path), path);
            return path;
        }

        private string GetExecutablePath(string project, string configuration)
        {
            var path = $@"{_testProjectsPath}/{project}/bin/{configuration}/{project}";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path += ".exe";
            }
            Assert.True(File.Exists(path), path);
            return path;
        }
    }
}
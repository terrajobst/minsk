using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Minsk.Build.Tasks
{
    public class MinskCompile : ToolTask
    {
        private ITaskItem[]? sources;
        private ITaskItem? outputAssembly;
        private ITaskItem[]? references;

        protected override string ToolName => ToolExe;

        public ITaskItem[]? Sources { set => sources = value; }

        [Output]
        public ITaskItem? OutputAssembly { set => outputAssembly = value; }

        public ITaskItem[]? References { set => references = value; }

        [Output]
        public ITaskItem[]? CommandLineArgs { get; set; }

        protected override string GenerateFullPathToTool()
        {
            return Path.Combine(ToolPath, ToolExe);
        }

        protected override string GenerateCommandLineCommands()
        {
            CommandLineBuilder builder = new CommandLineBuilder();

            builder.AppendFileNamesIfNotNull(sources, " ");

            builder.AppendSwitchIfNotNull("/o ", outputAssembly);

            if (references != null)
            {
                foreach (var refItem in references)
                {
                    builder.AppendSwitchIfNotNull("/r ", refItem);
                }
            }

            return builder.ToString();
        }
    }
}
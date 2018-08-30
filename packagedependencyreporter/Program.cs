using System;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;

namespace packagedependencyreporter
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new CommandLineParser.CommandLineParser
            {
                ShowUsageOnEmptyCommandline = true,
                AcceptSlash = true
            };

            //Required
            var projectFilesPath = new ValueArgument<string>(
                    'p', "projectFilesPath",
                    "Sets root search path for project files")
            { Optional = false };
            parser.Arguments.Add(projectFilesPath);

            //Optional
            var waitOnCompletion = new SwitchArgument(
                    'w', "waitOnCompletion",
                    "Wait for user input key when done processing before exiting", false)
                { Optional = true };
            parser.Arguments.Add(waitOnCompletion);


            var repositoryName = new ValueArgument<string>(
                    'g', "repositoryName",
                    "Name of the repository for previous build comparison")
                { Optional = true };
            parser.Arguments.Add(repositoryName);
            var branchName = new ValueArgument<string>(
                    'b', "branchName",
                    "Name of the repository's branch for previous build comparison")
                { Optional = true };
            parser.Arguments.Add(branchName);
            var enableRunAndCompareMode = new SwitchArgument(
                    'c', "enableRunAndCompareMode",
                    "Process normally and compare against previous build. Return an error code if the number of warnings or errors has increased. Requires repository and branch names.", false)
                { Optional = true };
            parser.Arguments.Add(enableRunAndCompareMode);

            try
            {
                parser.ParseCommandLine(args);
                //parser.ShowParsedArguments();

                var processor = new Processor
                {
                    ProjectsDirectory = projectFilesPath.Value,
                    PauseBeforeExit =  waitOnCompletion.Value,
                    RepositoryName = repositoryName.Value,
                    BranchName = branchName.Value,
                    RunAndCompareMode = enableRunAndCompareMode.Value
                };

                processor.Process();

            }
            catch (CommandLineException e)
            {
                Console.WriteLine("Unknown CommandLineException error: " + e.Message);
            }

            if (waitOnCompletion.Value)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
}

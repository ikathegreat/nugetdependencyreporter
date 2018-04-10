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
            var outputReportFilePath = new ValueArgument<string>(
                    'o', "outputReportFilePath",
                    "Sets file path for output report file")
                { Optional = true };
            parser.Arguments.Add(outputReportFilePath);

            try
            {
                parser.ParseCommandLine(args);
                //parser.ShowParsedArguments();

                var processor = new Processor
                {
                    ProjectsDirectory = projectFilesPath.Value
                };

                processor.Process();

            }
            catch (CommandLineException e)
            {
                Console.WriteLine("Unknown CommandLineException error: " + e.Message);
            }
        }
    }
}

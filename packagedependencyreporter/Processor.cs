using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace packagedependencyreporter
{
    public class Processor
    {
        private List<Project> projectsList;
        public string ProjectsDirectory { get; set; }
        public string BranchName { get; set; }
        public string RepositoryName { get; set; }
        public bool PauseBeforeExit { get; set; } = false;
        public bool RunAndCompareMode { get; set; } = false;

        internal void Process()
        {
            var stopwatch = Stopwatch.StartNew();

            var csprojFilesList = new List<string>();
            if (!string.IsNullOrEmpty(ProjectsDirectory))
            {
                Console.Write(string.Format($"Scanning {ProjectsDirectory}..."));
                csprojFilesList = Directory.EnumerateFiles(ProjectsDirectory,
                    "*.csproj", SearchOption.AllDirectories).ToList();
            }
            else
            {
                Console.WriteLine("Error: No path was provided.");
                Exit(ErrorCodes.NoPathProvided);
            }

            Console.WriteLine("Done.");
            Console.WriteLine(string.Format($"Found {csprojFilesList.Count} .csproj files."));

            if (csprojFilesList.Count == 0)
            {
                Console.WriteLine(string.Format($"Error: No .csproj files found in {ProjectsDirectory}"));
            }

            projectsList = new List<Project>();

            //Get projects and their references
            Console.Write("Reading project references...");
            //Parallel.ForEach(csprojFilesList, CreateProjectAndGetPackages);
            foreach (var project in csprojFilesList)
            {
                CreateProjectAndGetPackages(project);
            }
            Console.WriteLine("Done.");

            var allPackagesList = new List<Package>();
            var summaryList = new List<string>();

            projectsList.ForEach(x => x.Packages.ForEach(y => allPackagesList.Add(y)));

            var packagesWithMultipleVersionsList = allPackagesList.Distinct(new DistinctItemComparer()).GroupBy(x => x.Name)
                .Where(y => y.Count() > 1).ToList();

            var totalOutofDatePackagesCount = 0;
            foreach (var packageName in packagesWithMultipleVersionsList)
            {
                var multipleVersionPackageList = allPackagesList.FindAll(x => x.Name == packageName.Key);
                var latestVersion = new Version(1, 0, 0);
                foreach (var foundPackage in multipleVersionPackageList)
                {
                    if (foundPackage.Version > latestVersion)
                        latestVersion = foundPackage.Version;
                }

                var outOfDatePackagesCount = 0;
                var totalPackagesUsedCount = 0;
                foreach (var foundPackage in multipleVersionPackageList)
                {
                    //if (!HideDetailedInfo)
                    //    Console.WriteLine($" {foundPackage.ParentProject} uses {foundPackage.Version} {foundPackage.TargetFramework}");

                    if (foundPackage.Version != latestVersion)
                    {
                        outOfDatePackagesCount++;
                    }

                    totalPackagesUsedCount++;
                }

                if (outOfDatePackagesCount > 0)
                {
                    summaryList.Add($"{packageName.Key} package (latest version: {latestVersion}) is out of date in {outOfDatePackagesCount} project(s) of {totalPackagesUsedCount} total used:");
                    allPackagesList.FindAll(x => (x.Version != latestVersion && x.Name == packageName.Key)).ForEach(y => summaryList.Add(
                        string.Format(($" {y.ParentProject} {y.Version} {y.TargetFramework}"))));
                }

                totalOutofDatePackagesCount += outOfDatePackagesCount;
            }

            Console.WriteLine("Package checking completed.");
            Console.WriteLine("---");

            summaryList.ForEach(Console.WriteLine);
            Console.WriteLine("---");
            stopwatch.Stop();
            var t = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"{totalOutofDatePackagesCount} total out of date packages of {allPackagesList.Count} total packages used");
            Console.WriteLine($"Elapsed time: {t.Hours:D2}h:{t.Minutes:D2}m:{t.Seconds:D2}s:{t.Milliseconds:D3}ms");


            if (!string.IsNullOrEmpty(RepositoryName) && !string.IsNullOrEmpty(BranchName))
            {
                var resultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "SigmaTEK", "Repository Integrity", RepositoryName, BranchName, "Packages");

                Directory.CreateDirectory(resultDir);
                var resultFile = Path.Combine(resultDir, "Result.xml");

                var result = new Result()
                {
                    OutOfDatePackagesCount = totalOutofDatePackagesCount,
                    SummaryStringList = summaryList
                };

                if (RunAndCompareMode)
                {
                    var prevResult = LoadPrevResult(resultFile, new Result());

                    if (prevResult.OutOfDatePackagesCount < result.OutOfDatePackagesCount)
                    {
                        Exit(ErrorCodes.OutOfDatePackagesFoundIncreased);
                    }
                }
                else
                {
                    SaveResult(resultFile, result);
                }
            }
            else
            {
                if (RunAndCompareMode)
                    Console.WriteLine($"Error: Run and compare mode enabled, but repository and/or branch names are empty.");
            }

            if (totalOutofDatePackagesCount > 0)
            {
                Exit(ErrorCodes.OutOfDatePackagesFound);
            }
        }


        private static Result LoadPrevResult(string resultFile, Result prevResult)
        {
            TextReader reader = new StreamReader(resultFile);
            var xmlSerializer = new XmlSerializer(typeof(Result));

            try
            {
                prevResult = (Result)xmlSerializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured while reading from XML: " + ex.Message);
            }
            finally
            {
                reader.Close();
            }

            return prevResult;
        }

        private static void SaveResult(string resultFile, Result result)
        {
            TextWriter writer = new StreamWriter(resultFile);
            var xmlSerializer = new XmlSerializer(typeof(Result));

            try
            {
                xmlSerializer.Serialize(writer, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured while writing to XML: " + ex.Message);
            }
            finally
            {
                writer.Close();
            }
        }

        private void CreateProjectAndGetPackages(string csprojFile)
        {
            if (string.IsNullOrEmpty(csprojFile))
                return;

            var project = new Project(csprojFile);
            var packagesPath = Path.Combine(Path.GetDirectoryName(csprojFile), "packages.config");

            if (!File.Exists(packagesPath))
                return;

            var fileLines = File.ReadAllLines(packagesPath);
            foreach (var s in fileLines)
            {
                if (!s.Contains(@"package id="))
                    continue;

                var col = Regex.Matches(s, "\"([^\"]*)\"");

                var package = new Package
                {
                    ParentProject = project.Name,
                    Name = col[0].Value.Trim('"'),
                    Version = new Version(col[1].Value.Trim('"')),
                    TargetFramework = col[2].Value.Trim('"')
                };

                project.Packages.Add(package);
            }
            projectsList.Add(project);
        }

        private void Exit(ErrorCodes errorCode)
        {

            if (PauseBeforeExit)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            Environment.Exit((int)errorCode);
        }
    }
}

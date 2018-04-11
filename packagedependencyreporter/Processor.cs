using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace packagedependencyreporter
{
    public class Processor
    {
        private List<Project> projectsList;
        public string ProjectsDirectory { get; set; }
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
                Environment.Exit(-1);
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
                foreach (var foundPackage in multipleVersionPackageList)
                {
                    //if (!HideDetailedInfo)
                    //    Console.WriteLine($" {foundPackage.ParentProject} uses {foundPackage.Version} {foundPackage.TargetFramework}");

                    if (foundPackage.Version != latestVersion)
                    {
                        outOfDatePackagesCount++;
                    }
                }

                if (outOfDatePackagesCount > 0)
                {
                    summaryList.Add($"{packageName.Key} package (latest version: {latestVersion}) is out of date in {outOfDatePackagesCount} project(s):");
                    allPackagesList.FindAll(x => (x.Version != latestVersion && x.Name == packageName.Key)).ForEach(y => summaryList.Add(
                        string.Format(($" {y.ParentProject} {y.Version} {y.TargetFramework}"))));
                }

                totalOutofDatePackagesCount += outOfDatePackagesCount;
            }

            Console.WriteLine("Package checking completed.");

            summaryList.ForEach(Console.WriteLine);
            stopwatch.Stop();
            var t = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"{totalOutofDatePackagesCount} total out of date packages");
            Console.WriteLine($"Elapsed time: {t.Hours:D2}h:{t.Minutes:D2}m:{t.Seconds:D2}s:{t.Milliseconds:D3}ms");
            if (totalOutofDatePackagesCount > 0)
                Environment.Exit(1);
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
    }
}

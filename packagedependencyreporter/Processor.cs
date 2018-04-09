using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace packagedependencyreporter
{
    public class Processor
    {
        private List<Project> projectsList;
        public string ProjectsDirectory { get; set; }
        internal void Process()
        {
            var stopwatch = Stopwatch.StartNew();

            Console.Write(string.Format($"Scanning {ProjectsDirectory}..."));
            var csprojFilesList = Directory.EnumerateFiles(ProjectsDirectory,
                "*.csproj", SearchOption.AllDirectories).ToList();
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

            projectsList.ForEach(x => x.Packages.ForEach(y => allPackagesList.Add(y)));

            var packagesWithMultipleVersionsList = allPackagesList.Distinct(new DistinctItemComparer()).GroupBy(x => x.Name)
                .Where(y => y.Count() > 1).ToList();

            foreach (var packageName in packagesWithMultipleVersionsList)
            {
                var multipleVersionPackageList = allPackagesList.FindAll(x => x.Name == packageName.Key);
                var latestVersion = new Version(1, 0, 0);
                foreach (var foundPackage in multipleVersionPackageList)
                {
                    if (foundPackage.Version > latestVersion)
                        latestVersion = foundPackage.Version;                    
                }
                Console.WriteLine(packageName.Key + " latest version: " + latestVersion);
                foreach (var foundPackage in multipleVersionPackageList)
                {
                    if(foundPackage.Version != latestVersion)
                    Console.WriteLine(" " + foundPackage.Name + " " + foundPackage.ParentProject + " "
                        + foundPackage.Version + " " + foundPackage.TargetFramework);
                }
            }

            Console.WriteLine("Package checking completed.");
            stopwatch.Stop();
            var t = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"Elapsed time: {t.Hours:D2}h:{t.Minutes:D2}m:{t.Seconds:D2}s:{t.Milliseconds:D3}ms");
        }
        private void CreateProjectAndGetPackages(string csprojFile)
        {
            var project = new Project(csprojFile);
            var packagesPath = Path.Combine(Path.GetDirectoryName(csprojFile), "packages.config");

            if (!File.Exists(packagesPath))
                return;

            var fileLines = File.ReadAllLines(packagesPath);
            foreach (var s in fileLines)
            {
                if (!s.Contains(@"package id="))
                    continue;

                var regex = new Regex("\"([^\"]*)\"");
                // var match = regex.Match(s).Value;

                try
                {
                    var col = Regex.Matches(s, "\"([^\"]*)\"");

                    var package = new Package();

                    package.ParentProject = project.Name;
                    package.Name = col[0].Value.Trim('"');
                    package.Version = new Version(col[1].Value.Trim('"'));
                    package.TargetFramework = col[2].Value.Trim('"');

                    project.Packages.Add(package);

                }
                catch (Exception)
                {

                }



            }
            projectsList.Add(project);
        }
    }
}

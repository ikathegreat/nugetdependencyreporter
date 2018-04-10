using System;
using System.Collections.Generic;
using System.IO;

namespace packagedependencyreporter
{
    public class Project
    {

        public Project(string aFilePath)
        {
            FilePath = aFilePath;
            Packages = new List<Package>();

            if (string.IsNullOrEmpty(FilePath))
                return;
            if (File.Exists(FilePath))
                Name = Path.GetFileNameWithoutExtension(FilePath);
        }

        public string FilePath { get; set; }
        public string Name { get; set; }

        public List<Package> Packages { get; set; }
    }

    public struct Package
    {
        public string ParentProject { get; set; }
        public string Name { get; set; }
        public Version Version { get; set; }
        public string TargetFramework { get; set; }

    }

    public class DistinctItemComparer : IEqualityComparer<Package>
    {

        public bool Equals(Package x, Package y)
        {
            return x.Name == y.Name &&
                x.Version == y.Version &&
                x.TargetFramework == y.TargetFramework;
        }

        public int GetHashCode(Package obj)
        {
            return obj.Name.GetHashCode() ^
                obj.Version.GetHashCode() ^
                obj.TargetFramework.GetHashCode();
        }
    }
}

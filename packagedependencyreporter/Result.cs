using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace packagedependencyreporter
{
    public class Result
    {
        public int OutOfDatePackagesCount { get; set; } = 0;
        public List<string> SummaryStringList { get; set; } = new List<string>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace packagedependencyreporter
{
    public enum ErrorCodes
    {
        OutOfDatePackagesFound = 1,
        OutOfDatePackagesFoundIncreased,
        NoPathProvided
    }
}

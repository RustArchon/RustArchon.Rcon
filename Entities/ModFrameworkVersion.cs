using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.Entities
{
    public class ModFrameworkVersion : EntityBase
    {
        // The regex that actually parses this (for both Carbon and Oxide) lives in
        // CarbonVersionParser/OxideVersionParser, where the parsing happens - a data entity isn't the
        // right place for it. An earlier, unused, and subtly broken copy of the Carbon pattern (a
        // missing "\" escape before the branch group) used to live here; removed rather than fixed,
        // since it was never referenced by anything.
        public string ModFramework { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string BuildDate { get; set; } = string.Empty;

        public string Branch { get; set; } = string.Empty;
        public string BuildType { get; set; } = string.Empty;
        public string RustVersion { get; set; } = string.Empty;
        public string RustBuild { get; set; } = string.Empty;
        public string RustBuildDate { get; set; } = string.Empty;
    }
}

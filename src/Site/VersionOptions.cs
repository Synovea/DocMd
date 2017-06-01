using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocMd.Site
{
    public class VersionOptions
    {
        public string Sha { get; set; } = "";
        public DateTime CommitDateTime { get; set; } = DateTime.MinValue;

        public string CommitLink { get; set; }
    }
}

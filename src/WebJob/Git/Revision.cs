using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocMd.WebJob.Git
{
    public class Revision
    {
        public string Author { get; set; }

        public DateTimeOffset LastRevisionDate { get; set; }

        public string Sha { get; set; }
    }
}

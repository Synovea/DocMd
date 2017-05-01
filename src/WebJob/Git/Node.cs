using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocMd.WebJob.Git
{
    public class Node
    {
        public string Path { get; set; }

        public ChangeKind Status { get; set; }
    }
}

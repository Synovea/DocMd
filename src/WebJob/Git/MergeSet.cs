using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocMd.WebJob.Git
{
    public class MergeSet
    {
        public string RepoName { get; set; }
        public string RepoPath { get; set; }

        public List<Node> Changes { get; set; }
    }
}

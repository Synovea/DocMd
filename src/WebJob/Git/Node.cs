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
        public string RepoName { get; set; }
        public string RepoPath { get; set; }

        public string Path { get; set; }

        public ChangeKind Status { get; set; }

        public string Author
        {
            get
            {
                return LastRevision.Author;
            }
        }

        public DateTimeOffset LastRevisionDate
        {
            get
            {
                return LastRevision.LastRevisionDate;
            }
        }

        public string Sha
        {
            get
            {
                return LastRevision.Sha;
            }
        }

        public List<Revision> Revisions { get; set; }

        public Revision LastRevision
        {
            get
            {
                var revision = Revisions.FirstOrDefault();

                if (revision == null)
                {
                    revision = new Revision()
                    {
                        Author = "No Author Found",
                        LastRevisionDate = DateTimeOffset.Now,
                        Sha = ""
                    };
                }

                return revision;
            }
        }
    }
}

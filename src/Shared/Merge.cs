using System;
using System.Collections.Generic;
using System.Text;

namespace DocMd.Shared
{
    public class Merge
    {
        public string RepositoryName { get; set; }

        public string CommitId { get; set; }

        public string RemoteUrl { get; set; }

        public string PersonalAccessToken { get; set; }
    }
}

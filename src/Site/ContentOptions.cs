using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocMd.Site
{
    public class ContentOptions
    {
        public string BasePath { get; set; } = "/";
        public string HtmlPath { get; set; } = "/html";
        public string RepositoryPath { get; set; } = "/repo";

        public string Layout { get; set; }
        public string Redirect { get; set; }
    }
}

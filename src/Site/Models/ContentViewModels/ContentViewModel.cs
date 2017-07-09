using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocMd.Site.Models.ContentViewModels
{
    public class ContentViewModel
    {
        public string Title { get; set; }
        public string Header { get; set; } = string.Empty;

        public string Body { get; set; }
        public string ContentType { get; set; }

        public List<Shared.Content.Node> TableOfContents { get; set; }
    }
}

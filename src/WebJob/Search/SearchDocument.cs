using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocMd.WebJob.Search
{
    [SerializePropertyNamesAsCamelCase]
    public class SearchDocument
    {
        [Key]
        [IsFilterable]
        public string DocumentId { get; set; }

        [IsSearchable]
        public string Title { get; set; }

        [IsSearchable]
        public string Excerpt { get; set; }

        [IsSearchable]
        public string Body { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public string Author { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public DateTimeOffset LastRevisionDate { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public string Sha { get; set; }
    }
}

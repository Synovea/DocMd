using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace DocMd.Shared.Search
{
    [SerializePropertyNamesAsCamelCase]
    public class SearchDocument
    {
        [Key]
        [IsFilterable]
        public string DocumentId { get; set; }

        public string Path { get; set; }

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

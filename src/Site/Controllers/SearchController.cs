using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search;
using DocMd.Site;
using Microsoft.Extensions.Options;

namespace Site.Controllers
{
    public class SearchController : Controller
    {
        private readonly SearchOptions _searchOptions;

        public SearchController(IOptions<SearchOptions> searchOptions)
        {
            _searchOptions = searchOptions.Value;
        }

        public IActionResult Index(string query)
        {
            var searchServiceClient = CreateSearchServiceClient(_searchOptions);

            if (searchServiceClient.Indexes.Exists(_searchOptions.Index))
            {
                ISearchIndexClient indexClient = searchServiceClient.Indexes.GetClient(_searchOptions.Index);

                var searchResults = indexClient.Documents.Search<DocMd.Shared.Search.SearchDocument>(query);

                return View(searchResults);
            }

            return View();
        }

        private static SearchServiceClient CreateSearchServiceClient(SearchOptions searchOptions)
        {
            SearchServiceClient serviceClient = new SearchServiceClient(searchOptions.Name, new SearchCredentials(searchOptions.ApiKey));

            return serviceClient;
        }
    }
}
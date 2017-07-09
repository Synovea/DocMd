using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search;
using DocMd.Site;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Site.Controllers
{
    public class SearchController : Controller
    {
        private readonly ContentOptions _contentOptions;
        private readonly SearchOptions _searchOptions;
        private readonly IHostingEnvironment _hostingEnvironment;

        public SearchController(IOptions<ContentOptions> contentOptions, IOptions<SearchOptions> searchOptions, IHostingEnvironment hostingEnvironment)
        {
            _contentOptions = contentOptions.Value;
            _searchOptions = searchOptions.Value;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index(string query)
        {
            if (!string.IsNullOrWhiteSpace(_contentOptions.Layout) &&
                System.IO.File.Exists(Path.Combine(_hostingEnvironment.ContentRootPath, _contentOptions.Layout)))
            {
                ViewBag.Layout = _contentOptions.Layout;
            }
            else
            {
                ViewBag.Layout = "~/Views/Shared/_Layout.cshtml";
            }

            var searchServiceClient = CreateSearchServiceClient(_searchOptions);

            if (searchServiceClient.Indexes.Exists(_searchOptions.Index))
            {
                ISearchIndexClient indexClient = searchServiceClient.Indexes.GetClient(_searchOptions.Index);

                var searchResults = indexClient.Documents.Search<DocMd.Shared.Search.SearchDocument>(query);

                return View(searchResults);
            }

            return NotFound();
        }

        private static SearchServiceClient CreateSearchServiceClient(SearchOptions searchOptions)
        {
            SearchServiceClient serviceClient = new SearchServiceClient(searchOptions.Name, new SearchCredentials(searchOptions.ApiKey));

            return serviceClient;
        }
    }
}
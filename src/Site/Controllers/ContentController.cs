using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace DocMd.Site.Controllers
{
    public class ContentController : Controller
    {
        private readonly ContentOptions _contentOptions;
        private readonly IHostingEnvironment _hostingEnvironment;

        public ContentController(IOptions<ContentOptions> contentOptions, IHostingEnvironment hostingEnvironment)
        {
            _contentOptions = contentOptions.Value;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            if (!System.IO.File.Exists(Path.Combine(_hostingEnvironment.ContentRootPath, "webhooks.json")))
            {
                return RedirectToAction(nameof(SetupController.Index), "Setup");
            }

            if (!string.IsNullOrWhiteSpace(_contentOptions.Redirect))
            {
                return RedirectToAction(nameof(Render), new { path = _contentOptions.Redirect });
            }

            if (!string.IsNullOrWhiteSpace(_contentOptions.Layout) &&
                System.IO.File.Exists(Path.Combine(_contentOptions.HtmlPath, _contentOptions.Layout)))
            {
                ViewBag.Layout = $"~/{_contentOptions.HtmlPath.Replace(_contentOptions.BasePath, "")}/{_contentOptions.Layout}".Replace("\\", "/");
            }
            else
            {
                ViewBag.Layout = "~/Views/Shared/_Layout.cshtml";
            }

            return View(GetTableOfContents(Path.Combine(_hostingEnvironment.ContentRootPath, _contentOptions.HtmlPath)));
        }

        private Dictionary<string, List<Shared.Content.Node>> GetTableOfContents(string basePath, bool includeAllDirectories = true)
        {
            Dictionary<string, List<Shared.Content.Node>> tableOfContents = new Dictionary<string, List<Shared.Content.Node>>();

            var directories = Directory.GetDirectories(basePath, "*", SearchOption.TopDirectoryOnly);

            foreach (var directory in directories)
            {
                if (!includeAllDirectories && !directory.ToLower().Equals(basePath))
                {
                    continue;
                }

                if (System.IO.File.Exists(Path.Combine(directory, "toc.generated.json")))
                {
                    var toc = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Shared.Content.Node>>(System.IO.File.ReadAllText(Path.Combine(directory, "toc.generated.json")));

                    tableOfContents.Add(new DirectoryInfo(directory).Name, toc
                        .Flatten(m => m.Children)
                        .Where(m => m.Type.Equals("text/html"))
                        .Where(m => !string.IsNullOrWhiteSpace(m.Path))
                        .Select(m => new Shared.Content.Node()
                        {
                            Title = m.Title,
                            Excerpt = m.GetExcerpt(300),
                            ChangedDateTime = m.ChangedDateTime,
                            Path = m.Path.Replace("\\", "/")
                        }).ToList());
                }
            }

            return tableOfContents;
        }

        public ActionResult Render(string path, bool currentToC = false)
        {
            var basePath = Path.Combine(_hostingEnvironment.ContentRootPath, _contentOptions.HtmlPath);
            var baseDirectory = new DirectoryInfo(basePath);

            var directories = Directory.GetDirectories(basePath);
            var directoryCount = directories.Count();

            var contentPath = path;

            if (directoryCount == 1 && !System.IO.File.Exists(Path.Combine(basePath, contentPath)))
            {
                contentPath = Path.Combine(directories.First(), contentPath);
            }

            contentPath = Path.Combine(basePath, contentPath);
            var fileInfo = new FileInfo(contentPath);

            if (!fileInfo.Exists)
            {
                fileInfo = new FileInfo(Path.Combine(_hostingEnvironment.ContentRootPath, path));
                var directoryInfo = new DirectoryInfo(Path.Combine(_hostingEnvironment.ContentRootPath, contentPath));

                if (!fileInfo.Exists && !directoryInfo.Exists)
                {
                    return NotFound();
                }
                else if (!fileInfo.Exists && directoryInfo.Exists)
                {
                    ViewBag.TableOfContents = GetTableOfContents(Path.Combine(_hostingEnvironment.ContentRootPath, contentPath));
                }
                else
                {
                    var contentType = "application/octet-stream";
                    new FileExtensionContentTypeProvider().TryGetContentType(fileInfo.FullName, out contentType);

                    return File(fileInfo.FullName, contentType);
                }
            }

            var securityCheck = CheckAccessRules(basePath, baseDirectory, contentPath, fileInfo);

            if (securityCheck != null)
            {
                return securityCheck;
            }

            ViewBag.Path = $"\\{path.ToLower().Replace("/", "\\")}";

            return GetContent(basePath, fileInfo, currentToC);
        }

        private ActionResult CheckAccessRules(string basePath, DirectoryInfo baseDirectory, string contentPath, FileInfo fileInfo)
        {
            var securityFile = new FileInfo(Path.Combine(basePath, $"{contentPath}.security"));

            var parentDirectory = fileInfo.Directory;
            var securityFiles = new List<FileInfo>();

            if (securityFile.Exists)
            {
                securityFiles.Add(securityFile);
            }

            while (!parentDirectory.FullName.ToLower().Equals(baseDirectory.FullName.ToLower()))
            {
                if (parentDirectory.GetFiles(".security").Count() > 0)
                {
                    securityFiles.Add(new FileInfo($"{Path.Combine(parentDirectory.FullName, ".security")}"));
                }

                parentDirectory = parentDirectory.Parent;
            }

            securityFiles.Reverse();

            var accessRules = new Dictionary<string, bool>();

            foreach (var security in securityFiles)
            {
                var securityFileRules = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Shared.Security>>(System.IO.File.ReadAllText(security.FullName));

                foreach (var securityRule in securityFileRules)
                {
                    if (!accessRules.ContainsKey(GetRoleName(securityRule)))
                    {
                        accessRules.Add(GetRoleName(securityRule), GetRuleAllowDeny(securityRule));
                    }
                    else
                    {
                        accessRules[GetRoleName(securityRule)] = GetRuleAllowDeny(securityRule);
                    }
                }
            }

            if (User.Identity.IsAuthenticated)
            {
                foreach (var entry in accessRules)
                {
                    if (User.IsInRole(entry.Key) && !entry.Value)
                    {
                        // TODO: Enable sending a custom message back with the 401 Unauthorized payload
                        return Unauthorized(); // ($"This content is not available for users that belong to the '{entry.Key}' group.");
                    }
                }
            }
            else
            {
                if (accessRules.ContainsKey("ANONYMOUS") && !accessRules["ANONYMOUS"])
                {
                    // TODO: Enable sending a custom message back with the 401 Unauthorized payload
                    return Unauthorized(); // ("This content is not available for anonymous users.");
                }
            }

            return null;
        }

        private ActionResult GetContent(string basePath, FileInfo fileInfo, bool currentToC)
        {
            var baseDirectory = new DirectoryInfo(basePath);

            var contentType = "application/octet-stream";
            new FileExtensionContentTypeProvider().TryGetContentType(fileInfo.FullName, out contentType);

            var viewType = contentType.Split('/')[0];
            var viewDocumentType = contentType.Split('/')[1];

            if (viewType.Equals("text") && (!viewDocumentType.Equals("css")))
            {
                var model = new Models.ContentViewModels.ContentViewModel();

                model.ContentType = contentType;

                var parentDirectory = fileInfo.Directory;
                var layout = "~/Views/Shared/_Layout.cshtml";

                while (!parentDirectory.FullName.ToLower().Equals(baseDirectory.Parent.FullName.ToLower()))
                {
                    if (parentDirectory.GetFiles("_Layout.cshtml").Count() > 0)
                    {
                        layout = $"{Path.Combine(parentDirectory.FullName, "_Layout.cshtml")}";
                        break;
                    }

                    parentDirectory = parentDirectory.Parent;
                }

                if (!layout.Equals("~/Views/Shared/_Layout.cshtml"))
                    ViewBag.Layout = $"~/{_contentOptions.HtmlPath.Replace(_contentOptions.BasePath, "")}/{layout.ToLower().Replace(basePath.ToLower(), "")}".Replace("\\", "/");
                else
                    ViewBag.Layout = layout;

                var tocPath = fileInfo.Directory.FullName;

                if (!currentToC)
                {
                    var repo = fileInfo.FullName.Replace(basePath, "").Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries)[0];

                    tocPath = Path.Combine(basePath, repo);
                }

                var tocFile = Path.Combine(tocPath, "toc.generated.json");

                if (System.IO.File.Exists(tocFile))
                {
                    var toc = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Shared.Content.Node>>(System.IO.File.ReadAllText(tocFile));

                    model.TableOfContents = toc;
                }

                model.Body = System.IO.File.ReadAllText(fileInfo.FullName);

                var titleRegexPattern = "(?s)(?<=<h1.+>)(.+?)(?=</h1>)";
                var titleMatch = System.Text.RegularExpressions.Regex.Match(model.Body, titleRegexPattern);

                if (titleMatch.Success)
                {
                    model.Title = titleMatch.Value;
                }
                else
                {
                    model.Title = fileInfo.Name;
                }

                if (System.IO.File.Exists(fileInfo.FullName.ToLower().Replace(".html", ".header.html")))
                {
                    model.Header = System.IO.File.ReadAllText(fileInfo.FullName.ToLower().Replace(".html", ".header.html"));
                }

                return View(viewType, model);
            }
            else
            {
                return File(fileInfo.OpenRead(), contentType, fileInfo.Name);
            }
        }

        public IActionResult Error()
        {
            return View();
        }

        private static string GetRoleName(Shared.Security rule)
        {
            return rule.Role.ToUpper();
        }

        private static bool GetRuleAllowDeny(Shared.Security rule)
        {
            return (rule.Access.Equals(Shared.Access.Deny)) ? false :
                ((rule.Access.Equals(Shared.Access.Allow)) ? true : true);
        }
    }
}
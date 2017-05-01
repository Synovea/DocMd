using DocMd.WebJob.Git;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Markdig;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DocMd.WebJob
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called merges.
        [Singleton]
        public static async Task ProcessMergeMessageAsync([QueueTrigger("merges")] Shared.Merge merge, TextWriter log)
        {
            var sessionId = Guid.NewGuid();

            var reposPath = new DirectoryInfo(ConfigurationManager.AppSettings["repoPath"]).FullName;
            var repoPath = Path.Combine(reposPath, merge.RepositoryName);

            await LogAsync($"Merge message received for commit '{merge.CommitId}'.", sessionId, log);

            var mergeSet = new MergeSet()
            {
                RepoName = merge.RepositoryName,
                RepoPath = repoPath,
                Changes = new List<Node>()
            };

            var cloneOptions = new CloneOptions()
            {
                CredentialsProvider = (_url, _user, _cred) => GetRepoCredentials(merge.PersonalAccessToken)
            };

            if (!Directory.Exists(repoPath))
            {
                await LogAsync($"Repo path not found, creating directory '{repoPath}'.", sessionId, log);

                await LogAsync($"Cloning repository '{merge.RemoteUrl}'.", sessionId, log);

                Directory.CreateDirectory(repoPath);

                var cloneResult = Repository.Clone(merge.RemoteUrl, repoPath, cloneOptions);

                var files = Directory.GetFiles(repoPath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var currentFileInfo = new FileInfo(file);

                    if (currentFileInfo.Directory.FullName.Replace(repoPath, "").ToLower().StartsWith("\\.git"))
                    {
                        continue;
                    }

                    mergeSet.Changes.Add(new Node()
                    {
                        Path = file,
                        Status = ChangeKind.Added
                    });
                }
            }
            else
            {
                await LogAsync($"Pulling from repository '{merge.RemoteUrl}'.", sessionId, log);

                Directory.CreateDirectory(repoPath);

                using (var repo = new Repository(repoPath))
                {
                    LibGit2Sharp.PullOptions options = new LibGit2Sharp.PullOptions()
                    {
                        FetchOptions = new FetchOptions()
                        {
                            CredentialsProvider = new CredentialsHandler(
                                (url, usernameFromUrl, types) => GetRepoCredentials(merge.PersonalAccessToken))
                        }
                    };

                    var lastCommit = repo.Head.Tip;

                    var mergeResult = Commands.Pull(repo, GetRepoSignature(), options);

                    var headCommit = repo.Branches["origin/master"].Commits.First();

                    var treeChanges = repo.Diff.Compare<TreeChanges>(lastCommit.Tree, headCommit.Tree);

                    foreach (var treeChange in treeChanges)
                    {
                        if (treeChange.Status.Equals(ChangeKind.Renamed))
                        {
                            mergeSet.Changes.Add(new Node()
                            {
                                Path = Path.Combine(repoPath, treeChange.OldPath),
                                Status = ChangeKind.Deleted
                            });
                        };

                        mergeSet.Changes.Add(new Node()
                        {
                            Path = Path.Combine(repoPath, treeChange.Path),
                            Status = treeChange.Status
                        });
                    }
                }

            }

            await Shared.Helpers.QueueHelper.SendQueueMessage("renders", mergeSet);
        }

        [Singleton]
        public static async Task ProcessRenderMessageAsync([QueueTrigger("renders")] MergeSet mergeSet, TextWriter log)
        {
            var sessionId = Guid.NewGuid();

            var repoPath = mergeSet.RepoPath;

            var outputBasePath = new DirectoryInfo(ConfigurationManager.AppSettings["outputPath"]).FullName;
            var htmlPath = Path.Combine(outputBasePath, mergeSet.RepoName);

            await LogAsync($"Render markdown message received '{mergeSet.RepoName}'.", sessionId, log);

            if (!Directory.Exists(htmlPath))
            {
                await LogAsync($"Html path not found, creating directory '{htmlPath}'.", sessionId, log);

                Directory.CreateDirectory(htmlPath);
            }

            var files = mergeSet.Changes.Select(m => m.Path);

            foreach (var file in files)
            {
                try
                {
                    var currentFileInfo = new FileInfo(file);

                    if (currentFileInfo.Directory.FullName.Replace(repoPath, "").ToLower().StartsWith("\\.git"))
                    {
                        continue;
                    }

                    if (file.ToLower().EndsWith(".md"))
                    {
                        await LogAsync($"Rendering file '{file}'.", sessionId, log);

                        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                        var renderedContent = Markdown.ToHtml(File.ReadAllText(file), pipeline);

                        var renderedFilename = file.Replace(repoPath, htmlPath).Replace(".md", ".html");

                        await CreateDirectoryAsync(log, sessionId, renderedFilename);

                        File.WriteAllText(renderedFilename, renderedContent);
                    }
                    else
                    {
                        await CreateDirectoryAsync(log, sessionId, file.Replace(repoPath, htmlPath));

                        await LogAsync($"Copying file '{file}'.", sessionId, log);

                        File.Copy(file, file.Replace(repoPath, htmlPath).Replace(".md", ".html"), true);
                    }

                    await Shared.Helpers.QueueHelper.SendQueueMessage("search-indexes", file.Replace(repoPath, htmlPath).Replace(".md", ".html"));
                }
                catch (Exception error)
                {
                    await LogAsync($"Failure copying file '{file}' with error '{error.Message}'", sessionId, log);
                }
            }

            foreach (var change in mergeSet.Changes)
            {
                change.Path = change.Path.Replace(repoPath, htmlPath).Replace(".md", ".html");
            }

            await Shared.Helpers.QueueHelper.SendQueueMessage("cleanups", mergeSet);
        }

        [Singleton]
        public static async Task ProcessCleanupMessageAsync([QueueTrigger("cleanups")] MergeSet mergeSet, TextWriter log)
        {
            var sessionId = Guid.NewGuid();

            foreach (var change in mergeSet.Changes)
            {
                try
                {
                    if (change.Status.Equals(ChangeKind.Deleted))
                    {
                        await LogAsync($"Removing file '{change.Path}'", sessionId, log);

                        File.Delete(change.Path);

                        await Shared.Helpers.QueueHelper.SendQueueMessage("search-indexes-remove", change.Path);
                    }
                }
                catch (Exception error)
                {
                    await LogAsync($"Failure removing file '{change.Path}' with error '{error.Message}'", sessionId, log);
                }
            }

            await Shared.Helpers.QueueHelper.SendQueueMessage("toc-indexes", mergeSet);
        }

        [Singleton]
        public static async Task ProcessTableOfContentsMessageAsync([QueueTrigger("toc-indexes")] MergeSet mergeSet, TextWriter log)
        {
            var sessionId = Guid.NewGuid();

            var outputBasePath = new DirectoryInfo(ConfigurationManager.AppSettings["outputPath"]).FullName;
            var htmlPath = Path.Combine(outputBasePath, mergeSet.RepoName);

            await LogAsync($"Clearing Table of Content files.", sessionId, log);
            var tocFiles = Directory.GetFiles(htmlPath, "toc.generated.json", SearchOption.AllDirectories);

            foreach (var tocFile in tocFiles)
            {
                File.Delete(tocFile);
            }

            var contentDirectories = Directory.GetDirectories(htmlPath, "*", SearchOption.TopDirectoryOnly).ToList();

            contentDirectories.Add(htmlPath);

            foreach (var contentDirectory in contentDirectories)
            {
                try
                {
                    await LogAsync($"Indexing directory '{contentDirectory}' for Table of Contents", sessionId, log);

                    List<Shared.Content.Node> toc = new List<Shared.Content.Node>();
                    if (File.Exists(Path.Combine(contentDirectory, "toc.generated.json")))
                    {
                        toc = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Shared.Content.Node>>(File.ReadAllText(Path.Combine(contentDirectory, "toc.generated.json")));
                    }

                    toc.AddRange(GetTableOfContentsForDirectory(contentDirectory, outputBasePath));

                    File.WriteAllText(Path.Combine(contentDirectory, "toc.generated.json"), Newtonsoft.Json.JsonConvert.SerializeObject(toc));
                }
                catch (Exception error)
                {
                    await LogAsync($"Failure indexing directory '{contentDirectory}' with error '{error.Message}'", sessionId, log);
                }
            }
        } 

        private static List<Shared.Content.Node> GetTableOfContentsForDirectory(string directory, string basePath)
        {
            var toc = new List<Shared.Content.Node>();
            var generatedToc = true;

            if (File.Exists(Path.Combine(directory, "toc.json")))
            {
                generatedToc = false;
                toc = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Shared.Content.Node>>(File.ReadAllText(Path.Combine(directory, "toc.json")));
            }
            else
            {
                var htmlFiles = Directory.GetFiles(directory, "*.html", SearchOption.TopDirectoryOnly);

                foreach (var htmlFile in htmlFiles)
                {
                    var titleRegexPattern = "(?s)(?<=<h1.+>)(.+?)(?=</h1>)";
                    var excerptRegexPattern = "(?s)(?<=<p>)(.+?)(?=</p>)";

                    var content = File.ReadAllText(htmlFile);

                    var titleMatch = System.Text.RegularExpressions.Regex.Match(content, titleRegexPattern);
                    var excerptMatch = System.Text.RegularExpressions.Regex.Match(content, excerptRegexPattern);

                    toc.Add(new Shared.Content.Node()
                    {
                        Title = (titleMatch.Success) ? titleMatch.Value : "No title found",
                        Excerpt = (excerptMatch.Success) ? System.Text.RegularExpressions.Regex.Replace(excerptMatch.Value, "<[^>]*>", "") : "No excerpt found",
                        Path = htmlFile.Replace(basePath, "\\"),
                        ChangedDateTime = new FileInfo(htmlFile).LastWriteTimeUtc,
                        Type = "text/html"
                    });
                }
            }

            var directories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);

            foreach (var childDirectory in directories)
            {
                var directoryInfo = new DirectoryInfo(childDirectory);
                if (!generatedToc)
                {
                    var directoryToc = toc.Where(m => m.Type.Equals("text/directory") && m.Title.ToLower().Equals(directoryInfo.Name.ToLower())).FirstOrDefault();
                    if (directoryToc != null)
                    {
                        if (directoryToc.Properties.HasValue && directoryToc.Properties.Value.HasFlag(Shared.Content.NodeProperties.MustRecurse))
                        {
                            directoryToc.Children = GetTableOfContentsForDirectory(childDirectory, basePath);
                            directoryToc.ChangedDateTime = directoryInfo.LastWriteTimeUtc;
                        }

                        continue;
                    }
                }

                toc.Add(new Shared.Content.Node()
                {
                    Title = directoryInfo.Name,
                    Path = childDirectory.Replace(basePath, "\\"),
                    Type = "text/directory",
                    ChangedDateTime = directoryInfo.LastWriteTimeUtc,
                    Children = GetTableOfContentsForDirectory(childDirectory, basePath)
                });
            }

            return toc;
        }

        public static async Task ProcessSearchMessageAsync([QueueTrigger("search-indexes")] string htmlFile, TextWriter log)
        {
            var sessionId = Guid.NewGuid();

            try
            {
                await LogAsync($"Indexing file '{htmlFile}' for Search", sessionId, log);
            }
            catch (Exception error)
            {
                await LogAsync($"Failure indexing file '{htmlFile}' with error '{error.Message}'", sessionId, log);
            }
        }

        public static async Task ProcessSearchRemoveMessageAsync([QueueTrigger("search-indexes-remove")] string htmlFile, TextWriter log)
        {
            var sessionId = Guid.NewGuid();

            try
            {
                await LogAsync($"Removing file from index '{htmlFile}' for Search", sessionId, log);
            }
            catch (Exception error)
            {
                await LogAsync($"Failure removing files from index '{htmlFile}' with error '{error.Message}'", sessionId, log);
            }
        }

        private static async Task CreateDirectoryAsync(TextWriter log, Guid sessionId, string renderedFilename)
        {
            var renderedFileInfo = new FileInfo(renderedFilename);

            if (!Directory.Exists(renderedFileInfo.Directory.FullName))
            {
                await LogAsync($"Html path not found, creating directory '{renderedFileInfo.Directory.FullName}'.", sessionId, log);

                Directory.CreateDirectory(renderedFileInfo.Directory.FullName);
            }
        }

        private static UsernamePasswordCredentials GetRepoCredentials(string personalAccessToken)
        {
            return new UsernamePasswordCredentials()
            {
                Username = personalAccessToken,
                Password = String.Empty
            };
        }

        private static Signature GetRepoSignature()
        {
            return new Signature("Stephan Johnson", "stephan@johnson.org.za", new DateTimeOffset(DateTime.Now));
        }

        private static async Task LogAsync(string message, Guid identifier, TextWriter log)
        {
            await log.WriteLineAsync($"{DateTime.UtcNow}\t{identifier}\t{message}");
        }
    }
}

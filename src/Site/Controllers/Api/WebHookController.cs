using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace DocMd.Site.Controllers.Api
{
    [Produces("application/json")]
    [Route("api")]
    public class WebHookController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly Shared.Helpers.QueueHelper _queueHelper;

        public WebHookController(IHostingEnvironment hostingEnvironment, Shared.Helpers.QueueHelper queueHelper)
        {
            _hostingEnvironment = hostingEnvironment;

            _queueHelper = queueHelper;
        }

        [Route("hook/TfsGit/{id}")]
        public async Task<bool> MergePost(string id, [FromBody] DocMd.Shared.Git.Vsts.PullRequest.RootObject merge)
        {
            try
            {
                var webHooks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Shared.WebHook>>(System.IO.File.ReadAllText(Path.Combine(_hostingEnvironment.ContentRootPath, "webhooks.json")));

                var webHook = webHooks.Where(m => m.Secret.Equals(id) && m.RemoteUrl.ToLower().Equals(merge.resource.repository.remoteUrl.ToLower())).First();

                if (merge.resource.status.Equals("completed"))
                {
                    await _queueHelper.SendQueueMessage("merges", new Shared.Merge()
                    {
                        RepositoryName = merge.resource.repository.name,
                        CommitId = merge.resource.lastMergeCommit.commitId,
                        RemoteUrl = merge.resource.repository.remoteUrl,
                        PersonalAccessToken = webHook.PersonalAccessToken
                    });

                    return true;
                }

                return false;
            }
            catch (Exception error)
            {
                throw;
            }
        }

        [Route("hook/GitHub/{id}")]
        public async Task<bool> GitHubMergePost(string id, [FromBody] DocMd.Shared.Git.GitHub.PullRequest.RootObject merge)
        {
            try
            {
                var webHooks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Shared.WebHook>>(System.IO.File.ReadAllText(Path.Combine(_hostingEnvironment.ContentRootPath, "webhooks.json")));

                var webHook = webHooks.Where(m => m.Secret.Equals(id) && m.RemoteUrl.ToLower().Equals(merge.repository.clone_url.ToLower())).First();

                if (merge.action.Equals("closed") && merge.pull_request.merged)
                {
                    await _queueHelper.SendQueueMessage("merges", new Shared.Merge()
                    {
                        RepositoryName = merge.repository.name,
                        CommitId = merge.pull_request.head.sha,
                        RemoteUrl = merge.repository.clone_url,
                        PersonalAccessToken = webHook.PersonalAccessToken
                    });

                    return true;
                }

                return false;
            }
            catch (Exception error)
            {
                throw;
            }
        }
    }
}
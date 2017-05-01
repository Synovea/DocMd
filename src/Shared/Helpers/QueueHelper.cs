using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DocMd.Shared.Helpers
{
    public class QueueHelper
    {
        private readonly IConfiguration _configuration;

        public QueueHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendQueueMessage(string QueueName, object Message)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(_configuration["ConnectionStrings:AzureQueue"]);
            var cloudQueueClient = new CloudQueueClient(cloudStorageAccount.QueueStorageUri, cloudStorageAccount.Credentials);

            var queue = cloudQueueClient.GetQueueReference(QueueName);

            await queue.CreateIfNotExistsAsync();

            var queueMessage = new CloudQueueMessage(Newtonsoft.Json.JsonConvert.SerializeObject(Message));

            await queue.AddMessageAsync(queueMessage);
        }
    }
}

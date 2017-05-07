using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace DocMd.WebJob.Helpers
{
    public static class QueueHelper
    {
        public static async Task SendQueueMessage(string QueueName, object Message)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);
            var cloudQueueClient = new CloudQueueClient(cloudStorageAccount.QueueStorageUri, cloudStorageAccount.Credentials);

            var queue = cloudQueueClient.GetQueueReference(QueueName);

            await queue.CreateIfNotExistsAsync();

            var queueMessage = new CloudQueueMessage(Newtonsoft.Json.JsonConvert.SerializeObject(Message));

            await queue.AddMessageAsync(queueMessage);
        }
    }
}

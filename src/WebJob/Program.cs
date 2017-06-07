using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Configuration;

namespace DocMd.WebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            Console.WriteLine($"Doc Md Web Job v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}");

            var sha = ConfigurationManager.AppSettings["Sha"];
            var commitDateTime = ConfigurationManager.AppSettings["CommitDateTime"];
            var commitLink = ConfigurationManager.AppSettings["CommitLink"];

            Console.WriteLine($"Sha\t\t{sha}");
            Console.WriteLine($"Date Time\t\t{commitDateTime}");
            Console.WriteLine($"Link\t\t{commitLink}");

            var config = new JobHostConfiguration();

            config.UseTimers();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            var host = new JobHost(config);
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }
}

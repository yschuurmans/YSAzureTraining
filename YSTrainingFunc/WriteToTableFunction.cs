using System;
using System.Diagnostics.SymbolStore;
using System.Threading.Tasks;
using Domain;
using Domain.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace YSTrainingFunc
{
    public class WriteToTableFunction
    {
        [FunctionName("WriteToTableFunction")]
        [return: Table("cosmosregistrations", Connection = "CosmosDBConnection")]
        public RegisterModel Run(
            [QueueTrigger("registrations", Connection = "AzureWebJobsStorage")]string message, ILogger log)
        {
            RegisterModel model = JsonConvert.DeserializeObject<RegisterModel>(message);

            log.LogInformation(message);
            Console.WriteLine(message);

            model.RowKey = model.ToString();
            model.PartitionKey = model.Lastname[0].ToString();

            return model;
        }
    }
}

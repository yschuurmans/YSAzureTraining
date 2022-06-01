using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Threading.Tasks;
using Domain;
using Domain.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace YSTrainingFunc
{
    public class WriteToTableFunction
    {
        //[FunctionName("WriteToTableFunction")]
        //[return: Table("cosmosregistrations", Connection = "CosmosDBConnection")]
        //public RegisterModel Run(
        //    [QueueTrigger("registrations", Connection = "AzureWebJobsStorage")]string message, ILogger log)
        //{
        //    RegisterModel model = JsonConvert.DeserializeObject<RegisterModel>(message);

        //    log.LogInformation(message);
        //    Console.WriteLine(message);

        //    model.RowKey = model.ToString();
        //    model.PartitionKey = model.Lastname[0].ToString();

        //    return model;
        //}

        [FunctionName("WriteToTableFunction")]
        [return: Table("cosmosregistrations", Connection = "CosmosDBConnection")]
        public RegisterModel Run(
            [ServiceBusTrigger("newRegistration", "sub", Connection = "ServiceBusConnection")] string message, ILogger log)
        {
            RegisterModel model = JsonConvert.DeserializeObject<RegisterModel>(message);

            log.LogInformation(message);
            Console.WriteLine(message);

            model.RowKey = model.ToString();
            model.PartitionKey = model.Lastname[0].ToString();


            return model;
        }

        //[FunctionName("WriteToTableFunction")]
        //public async Task Run(
        //    [ServiceBusTrigger("newRegistration", "sub", Connection = "ServiceBusConnection")] string message, ILogger log)
        //{
        //    RegisterModel model = JsonConvert.DeserializeObject<RegisterModel>(message);

        //    log.LogInformation(message);
        //    Console.WriteLine(message);

        //    //model.RowKey = model.ToString();
        //    //model.PartitionKey = model.Lastname[0].ToString();




        //    IDatabase cache = await Redis.GetDatabaseAsync();

        //    await cache.ListRightPushAsync("registrations", JsonConvert.SerializeObject(model));

        //    await Redis.CloseConnectionAsync(Redis.Connection);


        //    //return model;
        //}
    }
}

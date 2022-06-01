using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YSImagesApiFrontend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RegistrationsController : ControllerBase
    {
        CloudStorageAccount cosmosCsa;

        private readonly ILogger<RegistrationsController> _logger;

        public RegistrationsController(ILogger<RegistrationsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            cosmosCsa = CloudStorageAccount.Parse(configuration.GetConnectionString("cosmoscsa"));
        }

        [HttpGet("/{user}")]
        public async Task<RegisterModel> Get(string user)
        {
            IDatabase cache = await Redis.GetDatabaseAsync();

            var tableClient = cosmosCsa.CreateCloudTableClient();

            var table = tableClient.GetTableReference("cosmosregistrations");

            await table.CreateIfNotExistsAsync();

            var result = await table.ExecuteQuerySegmentedAsync<RegisterModel>(new TableQuery<RegisterModel>(), null);

            return result.FirstOrDefault(x => x.ToString() == user);


            //if (user.Split("_").Count() < 2)
            //    return null;

            //var firstname = user.Split("_")[0];
            //var lastname = user.Split("_")[1];

            //var result = await cache.StringGetAsync("registrations");

            //List<RegisterModel> registrations = JsonConvert.DeserializeObject<List<RegisterModel>>(result);

            //await Redis.CloseConnectionAsync(Redis.Connection);

            //return registrations.FirstOrDefault(x => x.ToString() == user);
        }

        [HttpGet("/")]
        public async Task<List<RegisterModel>> List()
        {
            var tableClient = cosmosCsa.CreateCloudTableClient();

            var table = tableClient.GetTableReference("cosmosregistrations");

            await table.CreateIfNotExistsAsync();

            //ViewBag.Registrations = context.Registers.ToList();
            var results = await table.ExecuteQuerySegmentedAsync(new TableQuery<RegisterModel>(), null);

            return results.Results;

            //IDatabase cache = await Redis.GetDatabaseAsync();
            //var result = await cache.StringGetAsync("registrations");
            //List<RegisterModel> registrations = JsonConvert.DeserializeObject<List<RegisterModel>>(result);
            //await Redis.CloseConnectionAsync(Redis.Connection);
            //return registrations;
        }
    }
}

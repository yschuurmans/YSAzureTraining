using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YSImagesApiBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BackendRegistrationsController : ControllerBase
    {
        CloudStorageAccount csa;

        private readonly ILogger<BackendRegistrationsController> _logger;

        public BackendRegistrationsController(ILogger<BackendRegistrationsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            csa = CloudStorageAccount.Parse(configuration.GetConnectionString("csa"));
        }

        [HttpPost]
        public async Task<IActionResult> RegisterAsync(RegisterModel model)
        {
            var client = csa.CreateCloudQueueClient();
            var blobClient = csa.CreateCloudBlobClient();

            var queue = client.GetQueueReference("registrations");
            var container = blobClient.GetContainerReference("photos");

            await queue.CreateIfNotExistsAsync();
            await container.CreateIfNotExistsAsync();

            await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(model)));

            _logger.LogWarning($"Username: {model.Firstname}");
            _logger.LogWarning($"Password: {model.Lastname}");

            string person = model.ToString();

            foreach (IFormFile postedFile in postedFiles)
            {
                CloudBlockBlob block = container.GetBlockBlobReference(person + ".jpg");

                await block.UploadFromStreamAsync(postedFile.OpenReadStream());
            }

            return RedirectToAction("Register");
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
        }
    }
}

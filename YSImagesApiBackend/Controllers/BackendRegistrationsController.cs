using Azure.Messaging.ServiceBus;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace YSImagesApiBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BackendRegistrationsController : ControllerBase
    {
        const string ConnectionString = "Endpoint=sb://ystraininglab.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=u2DKJclk35OKribISYTrdPdApHVFTzdf4aDuRkOExso="; // Mijn ConnectionString

        CloudStorageAccount csa;

        private readonly ILogger<BackendRegistrationsController> _logger;

        public BackendRegistrationsController(ILogger<BackendRegistrationsController> logger, IConfiguration configuration)
        {
            _logger = logger;


            _logger.LogWarning($"using secret: '{configuration.GetConnectionString("csa")}'");

            csa = CloudStorageAccount.Parse(configuration.GetConnectionString("csa"));
            //csa = CloudStorageAccount.Parse(configuration.GetConnectionString("csa"));
        }

        [HttpPost]
        public async Task<IActionResult> RegisterAsync(RegisterModel model)
        {
            //var client = csa.CreateCloudQueueClient();
            var blobClient = csa.CreateCloudBlobClient();

            //var queue = client.GetQueueReference("registrations");
            var container = blobClient.GetContainerReference("photos");

            //await queue.CreateIfNotExistsAsync();
            await container.CreateIfNotExistsAsync();


            string person = model.ToString();

            CloudBlockBlob block = container.GetBlockBlobReference(person + ".jpg");

            await block.UploadFromByteArrayAsync(model.Photo, 0, model.Photo.Length);

            model.Photo = null;

            //await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(model)));
            await SendMessageAsync("newRegistration", JsonConvert.SerializeObject(model));
            await SendMessageAsync("newBlobToResize", JsonConvert.SerializeObject(new PhotoModel(person)));

            _logger.LogWarning($"Username: {model.Firstname}");
            _logger.LogWarning($"Password: {model.Lastname}");


            return Ok();
        }

        async Task SendMessageAsync(string topic, string text)
        {
            _logger.LogWarning($"Sending '{text}' to '{topic}'");
            Console.WriteLine($"Sending '{text}' to '{topic}'");
            ManagementClient mgr = new ManagementClient(ConnectionString);
            if (!await mgr.TopicExistsAsync(topic))
            {
            _logger.LogWarning($"Creating Topic...'{topic}'");
                Console.WriteLine("Creating Topic...");
                await mgr.CreateTopicAsync(topic);
            }

            _logger.LogWarning($"Sending Message...");
            Console.WriteLine("Sending Message...");
            var client = new ServiceBusClient(ConnectionString);
            var sender = client.CreateSender(topic);

            try
            {
                await sender.SendMessageAsync(new ServiceBusMessage(text));
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }


    }
}

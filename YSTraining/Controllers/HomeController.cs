using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Configuration;
using Domain.Models;
using Domain;

namespace YSTraining.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        CloudStorageAccount csa;
        CloudStorageAccount cosmosCsa;


        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            csa = CloudStorageAccount.Parse(configuration.GetConnectionString("csa"));
            cosmosCsa = CloudStorageAccount.Parse(configuration.GetConnectionString("cosmoscsa"));
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Details(string user)
        {
            var tableClient = cosmosCsa.CreateCloudTableClient();

            var table = tableClient.GetTableReference("registrations");

            await table.CreateIfNotExistsAsync();

            var firstname = user.Split("_")[0];
            var lastname = user.Split("_")[1];
            
            var result = await table.ExecuteQuerySegmentedAsync<RegisterModel>(new TableQuery<RegisterModel>(), null);

            var viewModel = new DetailsModel()
            {
                Registration = result.FirstOrDefault(x=>x.ToString() == user)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> RegisterAsync()
        {
            var tableClient = cosmosCsa.CreateCloudTableClient();

            var table = tableClient.GetTableReference("registrations");

            await table.CreateIfNotExistsAsync();

            //ViewBag.Registrations = context.Registers.ToList();
            var results = await table.ExecuteQuerySegmentedAsync(new TableQuery<RegisterModel>(), null);
            ViewBag.Registrations = results.Results;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterAsync(RegisterModel model, List<IFormFile> postedFiles)
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

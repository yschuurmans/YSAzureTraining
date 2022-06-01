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
using Refit;
using Microsoft.ApplicationInsights;

namespace YSTraining.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        CloudStorageAccount csa;
        CloudStorageAccount cosmosCsa;
        TelemetryClient aiClient;

        IFrontendApi frontend;
        IBackendApi backend;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, TelemetryClient aiClient)
        {
            _logger = logger;
            csa = CloudStorageAccount.Parse(configuration.GetConnectionString("csa"));
            cosmosCsa = CloudStorageAccount.Parse(configuration.GetConnectionString("cosmoscsa"));

            frontend = RestService.For<IFrontendApi>("https://ystrainingfe.azurewebsites.net/");
            backend = RestService.For<IBackendApi>("https://ystrainingbe.azurewebsites.net/");
            this.aiClient = aiClient;
        }

        public IActionResult Index()
        {
            aiClient.TrackEvent("GetIndex");

            return View();
        }

        public async Task<IActionResult> Details(string user)
        {
            aiClient.TrackEvent("GetDetails", new Dictionary<string, string>()
                { {"user",user } });

            var viewModel = new DetailsModel()
            {
                Registration = await frontend.Get(user)
            };
            return View(viewModel);


            //var tableClient = cosmosCsa.CreateCloudTableClient();

            //var table = tableClient.GetTableReference("cosmosregistrations");

            //await table.CreateIfNotExistsAsync();

            //var firstname = user.Split("_")[0];
            //var lastname = user.Split("_")[1];

            //var result = await table.ExecuteQuerySegmentedAsync<RegisterModel>(new TableQuery<RegisterModel>(), null);

            //var viewModel = new DetailsModel()
            //{
            //    Registration = result.FirstOrDefault(x=>x.ToString() == user)
            //};

            //return View(viewModel);
        }

        public async Task<IActionResult> RegisterAsync()
        {
            aiClient.TrackEvent("ListAllRegistrations");

            ViewBag.Registrations = await frontend.List();

            return View();

            //var tableClient = cosmosCsa.CreateCloudTableClient();

            //var table = tableClient.GetTableReference("cosmosregistrations");

            //await table.CreateIfNotExistsAsync();

            ////ViewBag.Registrations = context.Registers.ToList();
            //var results = await table.ExecuteQuerySegmentedAsync(new TableQuery<RegisterModel>(), null);
            //ViewBag.Registrations = results.Results;

            //return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterAsync(RegisterModel model, List<IFormFile> postedFiles)
        {
            aiClient.TrackEvent("Register New User",
                new Dictionary<string, string>(){
                    {"FirstName", model.Firstname},
                    {"LastName", model.Lastname }
                });

            foreach (IFormFile postedFile in postedFiles)
            {
                MemoryStream ms = new MemoryStream();
                postedFile.CopyTo(ms);
                model.Photo = ms.ToArray();
            }
            await backend.Register(model);
            return RedirectToAction("Register");


            //var client = csa.CreateCloudQueueClient();
            //var blobClient = csa.CreateCloudBlobClient();

            //var queue = client.GetQueueReference("registrations");
            //var container = blobClient.GetContainerReference("photos");

            //await queue.CreateIfNotExistsAsync();
            //await container.CreateIfNotExistsAsync();

            //await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(model)));

            //_logger.LogWarning($"Username: {model.Firstname}");
            //_logger.LogWarning($"Password: {model.Lastname}");

            //string person = model.ToString();

            //foreach (IFormFile postedFile in postedFiles)
            //{
            //    MemoryStream ms = new MemoryStream();
            //    postedFile.CopyTo(ms);
            //    model.Photo = ms.ToArray();
            //}

            //return RedirectToAction("Register");
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

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YSTrainingWebjob.Models;

namespace YSTrainingWebjob
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static async Task ProcessQueueMessageAsync([QueueTrigger("registrations")] string message, TextWriter log)
        {
            CloudStorageAccount csa = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

            var blobClient = csa.CreateCloudBlobClient();
            var tableClient = csa.CreateCloudTableClient();

            var container = blobClient.GetContainerReference("photos");
            var table = tableClient.GetTableReference("regTable");

            await container.CreateIfNotExistsAsync();
            await table.CreateIfNotExistsAsync();

            RegisterModel model = JsonConvert.DeserializeObject<RegisterModel>(message);

            var blobPhoto = container.GetBlockBlobReference($"{model}.jpg");
            MemoryStream stream = new MemoryStream();

            using(Image image = Image.Load(blobPhoto.OpenRead()))
            {
                float scale = image.Height > image.Width ?
                    100f / image.Height : 100f / image.Width;

                int width = (int)(image.Width * scale);
                int height = (int)(image.Height * scale);
                image.Mutate(x => x.Resize(width, height));
                image.SaveAsJpeg(stream);
            }
            stream.Position = 0;
            var thumpPhoto = container.GetBlockBlobReference($"{model}_th.jpg");
            await thumpPhoto.UploadFromStreamAsync(stream);

            model.RowKey = model.ToString();
            model.PartitionKey = model.Lastname[0].ToString();
            await table.ExecuteAsync(TableOperation.InsertOrReplace(model));

            log.Write(message);
            Console.WriteLine(message);
        }
    }
}

using System;
using System.IO;
using Domain.Models;
using ImageMagick;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace YSTrainingFunc
{
    public class ImageResize
    {
        //[FunctionName("ImageResize")]
        //public void Run(
        //    [BlobTrigger("photos/{blobName}.{ext}", Connection = "AzureWebJobsStorage")] Stream myBlob, string blobName,
        //    [Blob("thumbs/{blobName}_th.{ext}", FileAccess.Write)] Stream thumbnailStream, ILogger log)
        //{
        //    log.LogInformation($"C# Blob trigger function Processed blob\n Name:{blobName} \n Size: {myBlob.Length} Bytes");

        //    using (var image = new MagickImage(myBlob))
        //    {
        //        float scale = image.Height > image.Width ?
        //            100f / image.Height : 100f / image.Width;

        //        int width = (int)(image.Width * scale);
        //        int height = (int)(image.Height * scale);

        //        image.Format = MagickFormat.Jpg;
        //        image.Resize(width, height);
        //        image.Write(thumbnailStream);
        //    }


        //    myBlob.CopyTo(thumbnailStream);

        //    log.LogInformation($"Processed {blobName}");
        //    Console.WriteLine($"Processed {blobName}");
        //}


        [FunctionName("ImageResize")]
        public void Run(
            [ServiceBusTrigger("newBlobToResize", "sub", Connection = "ServiceBusConnection")] PhotoModel photo, 
            [Blob("photos/{BlobName}.jpg", FileAccess.Read)] Stream myBlob,
            [Blob("thumbs/{BlobName}_th.jpg", FileAccess.Write)] Stream thumbnailStream, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{photo.BlobName} \n Size: {myBlob.Length} Bytes");

            using (var image = new MagickImage(myBlob))
            {
                float scale = image.Height > image.Width ?
                    100f / image.Height : 100f / image.Width;

                int width = (int)(image.Width * scale);
                int height = (int)(image.Height * scale);

                image.Format = MagickFormat.Jpg;
                image.Resize(width, height);
                image.Write(thumbnailStream);
            }


            myBlob.CopyTo(thumbnailStream);

            log.LogInformation($"Processed {photo.BlobName}");
            Console.WriteLine($"Processed {photo.BlobName}");
        }
    }
}

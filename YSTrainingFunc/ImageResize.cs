using System;
using System.IO;
using ImageMagick;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace YSTrainingFunc
{
    public class ImageResize
    {
        [FunctionName("ImageResize")]
        public void Run(
            [BlobTrigger("photos/{blobName}.{ext}", Connection = "AzureWebJobsStorage")] Stream myBlob, string blobName,
            [Blob("thumbs/{blobName}_th.{ext}", FileAccess.Write)] Stream thumbnailStream, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{blobName} \n Size: {myBlob.Length} Bytes");

            using (var image = new MagickImage(myBlob))
            {

                //// Create a new memory stream
                //using (var memoryStream = new MemoryStream())
                //{

                float scale = image.Height > image.Width ?
                    100f / image.Height : 100f / image.Width;

                int width = (int)(image.Width * scale);
                int height = (int)(image.Height * scale);

                image.Format = MagickFormat.Jpg;
                image.Resize(width, height);
                image.Write(thumbnailStream);
                //memoryStream.Position = 0;

                //// Create a new blob block to hold our image
                //var blockBlob = container.GetBlockBlobReference(Guid.NewGuid().ToString() + ".png");

                //// Upload to azure
                //await blockBlob.UploadFromStreamAsync(memoryStream);

                //// Return the blobs url
                //return blockBlob.StorageUri.PrimaryUri.ToString();
                //}
            }


            myBlob.CopyTo(thumbnailStream);

            log.LogInformation($"Processed {blobName}");
            Console.WriteLine($"Processed {blobName}");
        }
    }
}

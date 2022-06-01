using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Domain.Models
{
    public class PhotoModel
    {
        public PhotoModel()
        {
        }

        public PhotoModel(string blobname)
        {
            this.BlobName = blobname;
        }

        public string BlobName { get; set; }
    }
}

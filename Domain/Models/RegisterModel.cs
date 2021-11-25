using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Domain.Models
{
    public class RegisterModel : TableEntity
    {
        public string Firstname { get; set; }

        public string Lastname { get; set; }

        [JsonIgnore]
        [IgnoreProperty]
        public List<string> Photos { get; set; }

        [IgnoreProperty]
        public byte[] Photo { get; set; }

        public override string ToString()
        {
            return $"{Firstname}_{Lastname}";
        }

        public string GetPhoto()
        {
            return "https://ystraininglabsa.blob.core.windows.net/photos/" + $"{ToString()}.jpg";
        }
        public string GetThumb()
        {
            return "https://ystraininglabsa.blob.core.windows.net/thumbs/" + $"{ToString()}_th.jpg";
        }
    }
}

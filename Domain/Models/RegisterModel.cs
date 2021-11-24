using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class RegisterModel
    {
        public long Id { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        [JsonIgnore]
        [NotMapped]
        public List<string> Photos { get; set; }

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

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;

namespace YSTrainingWebjob.Models
{
    public class RegisterModel : TableEntity
    {

        public string Firstname { get; set; }
        public string Lastname { get; set; }

        public override string ToString()
        {
            return $"{Firstname}_{Lastname}";
        }
    }
}

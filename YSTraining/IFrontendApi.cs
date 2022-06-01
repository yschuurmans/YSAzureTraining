using Domain.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YSTraining
{
    interface IFrontendApi
    {
        [Get("/{user}")]
        Task<RegisterModel> Get([Query]string user);

        [Get("/")]
        Task<List<RegisterModel>> List();
    }
}

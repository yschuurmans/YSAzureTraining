using Domain.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YSTraining
{

    interface IBackendApi
    {
        [Post("/BackendRegistrations")]
        Task Register(RegisterModel model);
    }
}

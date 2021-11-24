using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace Domain
{
    public class RegistrationContext : DbContext 
    {
        public RegistrationContext(DbContextOptions<RegistrationContext> options) : base(options)
        {
        }

        public DbSet<RegisterModel> Registers { get; set; }
    }
}

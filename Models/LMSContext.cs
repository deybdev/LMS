using System;
using System.Data.Entity;

namespace LMS.Models
{
    public class LMSContext : DbContext
    {
        public LMSContext() : base("LMS_DB")
        {
        }

        public DbSet<User> Users { get; set; }
    }
}

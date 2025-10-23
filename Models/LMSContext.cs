using LMS.Models;
using System.Data.Entity;

public class LMSContext : DbContext
{
    public LMSContext() : base("LMS_DB") { }

    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
}

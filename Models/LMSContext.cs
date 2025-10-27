using LMS.Models;
using System.Data.Entity;

public class LMSContext : DbContext
{
    public LMSContext() : base("LMS_DB") { }

    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseUser> CourseUsers { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialFile> MaterialFiles { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
}

using LMS.Models;
using System.Data.Entity;

public class LMSContext : DbContext
{
    public LMSContext() : base("LMS_DB") { }

    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CurriculumCourse> CurriculumCourses { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialFile> MaterialFiles { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Program> Programs { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<StudentCourse> StudentCourses { get; set; }
    public DbSet<TeacherCourseSection> TeacherCourseSections { get; set; }
    
    // Classwork entities
    public DbSet<Classwork> Classworks { get; set; }
    public DbSet<ClassworkFile> ClassworkFiles { get; set; }
    public DbSet<ClassworkSubmission> ClassworkSubmissions { get; set; }
    public DbSet<SubmissionFile> SubmissionFiles { get; set; }
}

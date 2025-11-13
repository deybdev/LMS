namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AuditLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Timestamp = c.DateTime(nullable: false),
                        Category = c.String(),
                        Message = c.String(),
                        UserName = c.String(),
                        Role = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Courses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CourseTitle = c.String(),
                        CourseCode = c.String(),
                        Description = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        CourseUnit = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CourseUsers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CourseId = c.Int(nullable: false),
                        StudentId = c.Int(nullable: false),
                        DateAdded = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .Index(t => t.CourseId);
            
            CreateTable(
                "dbo.Materials",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CourseId = c.Int(nullable: false),
                        Title = c.String(),
                        Type = c.String(),
                        Description = c.String(),
                        UploadedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .Index(t => t.CourseId);
            
            CreateTable(
                "dbo.MaterialFiles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MaterialId = c.Int(nullable: false),
                        FileName = c.String(),
                        FilePath = c.String(),
                        SizeInMB = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Materials", t => t.MaterialId, cascadeDelete: true)
                .Index(t => t.MaterialId);
            
            CreateTable(
                "dbo.CurriculumCourses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProgramId = c.Int(nullable: false),
                        CourseId = c.Int(nullable: false),
                        YearLevel = c.Int(nullable: false),
                        Semester = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.Programs", t => t.ProgramId, cascadeDelete: true)
                .Index(t => t.ProgramId)
                .Index(t => t.CourseId);
            
            CreateTable(
                "dbo.Programs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DepartmentId = c.Int(nullable: false),
                        ProgramName = c.String(),
                        ProgramCode = c.String(),
                        ProgramDuration = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Departments", t => t.DepartmentId, cascadeDelete: true)
                .Index(t => t.DepartmentId);
            
            CreateTable(
                "dbo.Departments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DepartmentName = c.String(),
                        DepartmentCode = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Events",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false),
                        Type = c.String(nullable: false),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                        Description = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Sections",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProgramId = c.Int(nullable: false),
                        YearLevel = c.Int(nullable: false),
                        SectionName = c.String(nullable: false, maxLength: 50),
                        DateCreated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Programs", t => t.ProgramId, cascadeDelete: true)
                .Index(t => t.ProgramId);
            
            CreateTable(
                "dbo.StudentCourses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StudentId = c.Int(nullable: false),
                        CourseId = c.Int(nullable: false),
                        SectionId = c.Int(nullable: false),
                        DateEnrolled = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.Sections", t => t.SectionId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.StudentId, cascadeDelete: true)
                .Index(t => t.StudentId)
                .Index(t => t.CourseId)
                .Index(t => t.SectionId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserID = c.String(),
                        FirstName = c.String(),
                        LastName = c.String(),
                        Email = c.String(),
                        PhoneNumber = c.String(),
                        Role = c.String(),
                        Password = c.String(),
                        LastLogin = c.DateTime(),
                        DateCreated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StudentCourses", "StudentId", "dbo.Users");
            DropForeignKey("dbo.StudentCourses", "SectionId", "dbo.Sections");
            DropForeignKey("dbo.StudentCourses", "CourseId", "dbo.Courses");
            DropForeignKey("dbo.Sections", "ProgramId", "dbo.Programs");
            DropForeignKey("dbo.CurriculumCourses", "ProgramId", "dbo.Programs");
            DropForeignKey("dbo.Programs", "DepartmentId", "dbo.Departments");
            DropForeignKey("dbo.CurriculumCourses", "CourseId", "dbo.Courses");
            DropForeignKey("dbo.MaterialFiles", "MaterialId", "dbo.Materials");
            DropForeignKey("dbo.Materials", "CourseId", "dbo.Courses");
            DropForeignKey("dbo.CourseUsers", "CourseId", "dbo.Courses");
            DropIndex("dbo.StudentCourses", new[] { "SectionId" });
            DropIndex("dbo.StudentCourses", new[] { "CourseId" });
            DropIndex("dbo.StudentCourses", new[] { "StudentId" });
            DropIndex("dbo.Sections", new[] { "ProgramId" });
            DropIndex("dbo.Programs", new[] { "DepartmentId" });
            DropIndex("dbo.CurriculumCourses", new[] { "CourseId" });
            DropIndex("dbo.CurriculumCourses", new[] { "ProgramId" });
            DropIndex("dbo.MaterialFiles", new[] { "MaterialId" });
            DropIndex("dbo.Materials", new[] { "CourseId" });
            DropIndex("dbo.CourseUsers", new[] { "CourseId" });
            DropTable("dbo.Users");
            DropTable("dbo.StudentCourses");
            DropTable("dbo.Sections");
            DropTable("dbo.Events");
            DropTable("dbo.Departments");
            DropTable("dbo.Programs");
            DropTable("dbo.CurriculumCourses");
            DropTable("dbo.MaterialFiles");
            DropTable("dbo.Materials");
            DropTable("dbo.CourseUsers");
            DropTable("dbo.Courses");
            DropTable("dbo.AuditLogs");
        }
    }
}

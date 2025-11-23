namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddClassword : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ClassworkFiles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ClassworkId = c.Int(nullable: false),
                        FileName = c.String(nullable: false, maxLength: 255),
                        FilePath = c.String(nullable: false, maxLength: 500),
                        SizeInMB = c.Decimal(nullable: false, precision: 18, scale: 2),
                        UploadedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Classworks", t => t.ClassworkId, cascadeDelete: true)
                .Index(t => t.ClassworkId);
            
            CreateTable(
                "dbo.Classworks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TeacherCourseSectionId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 200),
                        ClassworkType = c.String(nullable: false, maxLength: 50),
                        Description = c.String(nullable: false),
                        Deadline = c.DateTime(nullable: false),
                        Points = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TeacherCourseSections", t => t.TeacherCourseSectionId, cascadeDelete: true)
                .Index(t => t.TeacherCourseSectionId);
            
            CreateTable(
                "dbo.ClassworkSubmissions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ClassworkId = c.Int(nullable: false),
                        StudentId = c.Int(nullable: false),
                        SubmissionText = c.String(),
                        SubmittedAt = c.DateTime(),
                        Grade = c.Decimal(precision: 18, scale: 2),
                        Feedback = c.String(),
                        GradedAt = c.DateTime(),
                        Status = c.String(maxLength: 20),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Classworks", t => t.ClassworkId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.StudentId)
                .Index(t => t.ClassworkId)
                .Index(t => t.StudentId);
            
            CreateTable(
                "dbo.SubmissionFiles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SubmissionId = c.Int(nullable: false),
                        FileName = c.String(nullable: false, maxLength: 255),
                        FilePath = c.String(nullable: false, maxLength: 500),
                        SizeInMB = c.Decimal(nullable: false, precision: 18, scale: 2),
                        UploadedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ClassworkSubmissions", t => t.SubmissionId, cascadeDelete: true)
                .Index(t => t.SubmissionId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Classworks", "TeacherCourseSectionId", "dbo.TeacherCourseSections");
            DropForeignKey("dbo.SubmissionFiles", "SubmissionId", "dbo.ClassworkSubmissions");
            DropForeignKey("dbo.ClassworkSubmissions", "StudentId", "dbo.Users");
            DropForeignKey("dbo.ClassworkSubmissions", "ClassworkId", "dbo.Classworks");
            DropForeignKey("dbo.ClassworkFiles", "ClassworkId", "dbo.Classworks");
            DropIndex("dbo.SubmissionFiles", new[] { "SubmissionId" });
            DropIndex("dbo.ClassworkSubmissions", new[] { "StudentId" });
            DropIndex("dbo.ClassworkSubmissions", new[] { "ClassworkId" });
            DropIndex("dbo.Classworks", new[] { "TeacherCourseSectionId" });
            DropIndex("dbo.ClassworkFiles", new[] { "ClassworkId" });
            DropTable("dbo.SubmissionFiles");
            DropTable("dbo.ClassworkSubmissions");
            DropTable("dbo.Classworks");
            DropTable("dbo.ClassworkFiles");
        }
    }
}

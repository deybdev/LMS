namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTeacherLibrary : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ClassworkCourseAssignments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ClassworkLibraryId = c.Int(nullable: false),
                        TeacherCourseSectionId = c.Int(nullable: false),
                        ClassworkId = c.Int(),
                        Deadline = c.DateTime(),
                        AssignedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Classworks", t => t.ClassworkId)
                .ForeignKey("dbo.TeacherClassworkLibraries", t => t.ClassworkLibraryId, cascadeDelete: true)
                .ForeignKey("dbo.TeacherCourseSections", t => t.TeacherCourseSectionId)
                .Index(t => t.ClassworkLibraryId)
                .Index(t => t.TeacherCourseSectionId)
                .Index(t => t.ClassworkId);
            
            CreateTable(
                "dbo.TeacherClassworkLibraries",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TeacherId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 200),
                        ClassworkType = c.String(nullable: false, maxLength: 50),
                        Description = c.String(),
                        Points = c.Int(nullable: false),
                        QuestionsJson = c.String(),
                        SubmissionMode = c.String(maxLength: 50),
                        Subject = c.String(maxLength: 100),
                        Tags = c.String(maxLength: 50),
                        DateCreated = c.DateTime(nullable: false),
                        LastModified = c.DateTime(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.TeacherId, cascadeDelete: true)
                .Index(t => t.TeacherId);
            
            CreateTable(
                "dbo.TeacherClassworkLibraryFiles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ClassworkLibraryId = c.Int(nullable: false),
                        FileName = c.String(nullable: false, maxLength: 255),
                        FilePath = c.String(nullable: false, maxLength: 500),
                        SizeInMB = c.Decimal(nullable: false, precision: 18, scale: 2),
                        UploadedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TeacherClassworkLibraries", t => t.ClassworkLibraryId)
                .Index(t => t.ClassworkLibraryId);
            
            CreateTable(
                "dbo.MaterialCourseAssignments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MaterialLibraryId = c.Int(nullable: false),
                        TeacherCourseSectionId = c.Int(nullable: false),
                        AssignedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TeacherMaterialLibraries", t => t.MaterialLibraryId, cascadeDelete: true)
                .ForeignKey("dbo.TeacherCourseSections", t => t.TeacherCourseSectionId)
                .Index(t => t.MaterialLibraryId)
                .Index(t => t.TeacherCourseSectionId);
            
            CreateTable(
                "dbo.TeacherMaterialLibraries",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TeacherId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 200),
                        Type = c.String(nullable: false, maxLength: 50),
                        Description = c.String(),
                        Subject = c.String(maxLength: 100),
                        Tags = c.String(maxLength: 50),
                        DateCreated = c.DateTime(nullable: false),
                        LastModified = c.DateTime(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.TeacherId)
                .Index(t => t.TeacherId);
            
            CreateTable(
                "dbo.TeacherMaterialLibraryFiles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MaterialLibraryId = c.Int(nullable: false),
                        FileName = c.String(nullable: false, maxLength: 255),
                        FilePath = c.String(nullable: false, maxLength: 500),
                        SizeInMB = c.Decimal(nullable: false, precision: 18, scale: 2),
                        UploadedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TeacherMaterialLibraries", t => t.MaterialLibraryId)
                .Index(t => t.MaterialLibraryId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MaterialCourseAssignments", "TeacherCourseSectionId", "dbo.TeacherCourseSections");
            DropForeignKey("dbo.TeacherMaterialLibraries", "TeacherId", "dbo.Users");
            DropForeignKey("dbo.TeacherMaterialLibraryFiles", "MaterialLibraryId", "dbo.TeacherMaterialLibraries");
            DropForeignKey("dbo.MaterialCourseAssignments", "MaterialLibraryId", "dbo.TeacherMaterialLibraries");
            DropForeignKey("dbo.ClassworkCourseAssignments", "TeacherCourseSectionId", "dbo.TeacherCourseSections");
            DropForeignKey("dbo.TeacherClassworkLibraries", "TeacherId", "dbo.Users");
            DropForeignKey("dbo.TeacherClassworkLibraryFiles", "ClassworkLibraryId", "dbo.TeacherClassworkLibraries");
            DropForeignKey("dbo.ClassworkCourseAssignments", "ClassworkLibraryId", "dbo.TeacherClassworkLibraries");
            DropForeignKey("dbo.ClassworkCourseAssignments", "ClassworkId", "dbo.Classworks");
            DropIndex("dbo.TeacherMaterialLibraryFiles", new[] { "MaterialLibraryId" });
            DropIndex("dbo.TeacherMaterialLibraries", new[] { "TeacherId" });
            DropIndex("dbo.MaterialCourseAssignments", new[] { "TeacherCourseSectionId" });
            DropIndex("dbo.MaterialCourseAssignments", new[] { "MaterialLibraryId" });
            DropIndex("dbo.TeacherClassworkLibraryFiles", new[] { "ClassworkLibraryId" });
            DropIndex("dbo.TeacherClassworkLibraries", new[] { "TeacherId" });
            DropIndex("dbo.ClassworkCourseAssignments", new[] { "ClassworkId" });
            DropIndex("dbo.ClassworkCourseAssignments", new[] { "TeacherCourseSectionId" });
            DropIndex("dbo.ClassworkCourseAssignments", new[] { "ClassworkLibraryId" });
            DropTable("dbo.TeacherMaterialLibraryFiles");
            DropTable("dbo.TeacherMaterialLibraries");
            DropTable("dbo.MaterialCourseAssignments");
            DropTable("dbo.TeacherClassworkLibraryFiles");
            DropTable("dbo.TeacherClassworkLibraries");
            DropTable("dbo.ClassworkCourseAssignments");
        }
    }
}

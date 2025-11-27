namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ajskdnaksd : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ClassworkCourseAssignments", "ClassworkId", "dbo.Classworks");
            DropForeignKey("dbo.ClassworkCourseAssignments", "ClassworkLibraryId", "dbo.TeacherClassworkLibraries");
            DropForeignKey("dbo.TeacherClassworkLibraryFiles", "ClassworkLibraryId", "dbo.TeacherClassworkLibraries");
            DropForeignKey("dbo.TeacherClassworkLibraries", "TeacherId", "dbo.Users");
            DropForeignKey("dbo.ClassworkCourseAssignments", "TeacherCourseSectionId", "dbo.TeacherCourseSections");
            DropForeignKey("dbo.MaterialCourseAssignments", "MaterialLibraryId", "dbo.TeacherMaterialLibraries");
            DropForeignKey("dbo.TeacherMaterialLibraryFiles", "MaterialLibraryId", "dbo.TeacherMaterialLibraries");
            DropForeignKey("dbo.TeacherMaterialLibraries", "TeacherId", "dbo.Users");
            DropForeignKey("dbo.MaterialCourseAssignments", "TeacherCourseSectionId", "dbo.TeacherCourseSections");
            DropIndex("dbo.ClassworkCourseAssignments", new[] { "ClassworkLibraryId" });
            DropIndex("dbo.ClassworkCourseAssignments", new[] { "TeacherCourseSectionId" });
            DropIndex("dbo.ClassworkCourseAssignments", new[] { "ClassworkId" });
            DropIndex("dbo.TeacherClassworkLibraries", new[] { "TeacherId" });
            DropIndex("dbo.TeacherClassworkLibraryFiles", new[] { "ClassworkLibraryId" });
            DropIndex("dbo.MaterialCourseAssignments", new[] { "MaterialLibraryId" });
            DropIndex("dbo.MaterialCourseAssignments", new[] { "TeacherCourseSectionId" });
            DropIndex("dbo.TeacherMaterialLibraries", new[] { "TeacherId" });
            DropIndex("dbo.TeacherMaterialLibraryFiles", new[] { "MaterialLibraryId" });
            DropTable("dbo.ClassworkCourseAssignments");
            DropTable("dbo.TeacherClassworkLibraries");
            DropTable("dbo.TeacherClassworkLibraryFiles");
            DropTable("dbo.MaterialCourseAssignments");
            DropTable("dbo.TeacherMaterialLibraries");
            DropTable("dbo.TeacherMaterialLibraryFiles");
        }
        
        public override void Down()
        {
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
                .PrimaryKey(t => t.Id);
            
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
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.MaterialCourseAssignments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MaterialLibraryId = c.Int(nullable: false),
                        TeacherCourseSectionId = c.Int(nullable: false),
                        AssignedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
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
                .PrimaryKey(t => t.Id);
            
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
                .PrimaryKey(t => t.Id);
            
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
                .PrimaryKey(t => t.Id);
            
            CreateIndex("dbo.TeacherMaterialLibraryFiles", "MaterialLibraryId");
            CreateIndex("dbo.TeacherMaterialLibraries", "TeacherId");
            CreateIndex("dbo.MaterialCourseAssignments", "TeacherCourseSectionId");
            CreateIndex("dbo.MaterialCourseAssignments", "MaterialLibraryId");
            CreateIndex("dbo.TeacherClassworkLibraryFiles", "ClassworkLibraryId");
            CreateIndex("dbo.TeacherClassworkLibraries", "TeacherId");
            CreateIndex("dbo.ClassworkCourseAssignments", "ClassworkId");
            CreateIndex("dbo.ClassworkCourseAssignments", "TeacherCourseSectionId");
            CreateIndex("dbo.ClassworkCourseAssignments", "ClassworkLibraryId");
            AddForeignKey("dbo.MaterialCourseAssignments", "TeacherCourseSectionId", "dbo.TeacherCourseSections", "Id", cascadeDelete: true);
            AddForeignKey("dbo.TeacherMaterialLibraries", "TeacherId", "dbo.Users", "Id", cascadeDelete: true);
            AddForeignKey("dbo.TeacherMaterialLibraryFiles", "MaterialLibraryId", "dbo.TeacherMaterialLibraries", "Id", cascadeDelete: true);
            AddForeignKey("dbo.MaterialCourseAssignments", "MaterialLibraryId", "dbo.TeacherMaterialLibraries", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ClassworkCourseAssignments", "TeacherCourseSectionId", "dbo.TeacherCourseSections", "Id", cascadeDelete: true);
            AddForeignKey("dbo.TeacherClassworkLibraries", "TeacherId", "dbo.Users", "Id", cascadeDelete: true);
            AddForeignKey("dbo.TeacherClassworkLibraryFiles", "ClassworkLibraryId", "dbo.TeacherClassworkLibraries", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ClassworkCourseAssignments", "ClassworkLibraryId", "dbo.TeacherClassworkLibraries", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ClassworkCourseAssignments", "ClassworkId", "dbo.Classworks", "Id");
        }
    }
}

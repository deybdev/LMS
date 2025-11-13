namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AssignProgUpd : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TeacherCourseSections",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TeacherId = c.Int(nullable: false),
                        CourseId = c.Int(nullable: false),
                        SectionId = c.Int(nullable: false),
                        Semester = c.Int(nullable: false),
                        DateAssigned = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.Sections", t => t.SectionId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.TeacherId, cascadeDelete: true)
                .Index(t => t.TeacherId)
                .Index(t => t.CourseId)
                .Index(t => t.SectionId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TeacherCourseSections", "TeacherId", "dbo.Users");
            DropForeignKey("dbo.TeacherCourseSections", "SectionId", "dbo.Sections");
            DropForeignKey("dbo.TeacherCourseSections", "CourseId", "dbo.Courses");
            DropIndex("dbo.TeacherCourseSections", new[] { "SectionId" });
            DropIndex("dbo.TeacherCourseSections", new[] { "CourseId" });
            DropIndex("dbo.TeacherCourseSections", new[] { "TeacherId" });
            DropTable("dbo.TeacherCourseSections");
        }
    }
}

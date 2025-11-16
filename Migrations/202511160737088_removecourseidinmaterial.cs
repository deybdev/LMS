namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class removecourseidinmaterial : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Materials", "CourseId", "dbo.Courses");
            DropIndex("dbo.Materials", new[] { "CourseId" });
            RenameColumn(table: "dbo.Materials", name: "CourseId", newName: "Course_Id");
            AddColumn("dbo.Materials", "TeacherCourseSectionId", c => c.Int(nullable: false));
            AlterColumn("dbo.Materials", "Course_Id", c => c.Int());
            CreateIndex("dbo.Materials", "TeacherCourseSectionId");
            CreateIndex("dbo.Materials", "Course_Id");
            AddForeignKey("dbo.Materials", "TeacherCourseSectionId", "dbo.TeacherCourseSections", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Materials", "Course_Id", "dbo.Courses", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Materials", "Course_Id", "dbo.Courses");
            DropForeignKey("dbo.Materials", "TeacherCourseSectionId", "dbo.TeacherCourseSections");
            DropIndex("dbo.Materials", new[] { "Course_Id" });
            DropIndex("dbo.Materials", new[] { "TeacherCourseSectionId" });
            AlterColumn("dbo.Materials", "Course_Id", c => c.Int(nullable: false));
            DropColumn("dbo.Materials", "TeacherCourseSectionId");
            RenameColumn(table: "dbo.Materials", name: "Course_Id", newName: "CourseId");
            CreateIndex("dbo.Materials", "CourseId");
            AddForeignKey("dbo.Materials", "CourseId", "dbo.Courses", "Id", cascadeDelete: true);
        }
    }
}

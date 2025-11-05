namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CourseUpdate : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Courses", "Teacher_Id", "dbo.Users");
            DropIndex("dbo.Courses", new[] { "Teacher_Id" });
            DropColumn("dbo.Courses", "Section");
            DropColumn("dbo.Courses", "AcademicYear");
            DropColumn("dbo.Courses", "Description");
            DropColumn("dbo.Courses", "Day");
            DropColumn("dbo.Courses", "StartTime");
            DropColumn("dbo.Courses", "EndTime");
            DropColumn("dbo.Courses", "Teacher_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Courses", "Teacher_Id", c => c.Int());
            AddColumn("dbo.Courses", "EndTime", c => c.String());
            AddColumn("dbo.Courses", "StartTime", c => c.String());
            AddColumn("dbo.Courses", "Day", c => c.String());
            AddColumn("dbo.Courses", "Description", c => c.String());
            AddColumn("dbo.Courses", "AcademicYear", c => c.String());
            AddColumn("dbo.Courses", "Section", c => c.String());
            CreateIndex("dbo.Courses", "Teacher_Id");
            AddForeignKey("dbo.Courses", "Teacher_Id", "dbo.Users", "Id");
        }
    }
}

namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StudentCoursesUpd : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StudentCourses", "TimeFrom", c => c.DateTime(nullable: false));
            AddColumn("dbo.StudentCourses", "TimeTo", c => c.DateTime(nullable: false));
            AddColumn("dbo.StudentCourses", "Status", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.StudentCourses", "Status");
            DropColumn("dbo.StudentCourses", "TimeTo");
            DropColumn("dbo.StudentCourses", "TimeFrom");
        }
    }
}

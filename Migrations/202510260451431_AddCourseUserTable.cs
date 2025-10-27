namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCourseUserTable : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CourseUsers", "DateAdded", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CourseUsers", "DateAdded");
        }
    }
}

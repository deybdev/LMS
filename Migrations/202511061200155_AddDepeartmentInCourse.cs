namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDepeartmentInCourse : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Courses", "Department", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Courses", "Department");
        }
    }
}

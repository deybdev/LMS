namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NullableTime : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.StudentCourses", "TimeFrom", c => c.DateTime());
            AlterColumn("dbo.StudentCourses", "TimeTo", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.StudentCourses", "TimeTo", c => c.DateTime(nullable: false));
            AlterColumn("dbo.StudentCourses", "TimeFrom", c => c.DateTime(nullable: false));
        }
    }
}

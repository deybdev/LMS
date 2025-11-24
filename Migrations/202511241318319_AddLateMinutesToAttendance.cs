namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLateMinutesToAttendance : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AttendanceRecords", "LateMinutes", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AttendanceRecords", "LateMinutes");
        }
    }
}

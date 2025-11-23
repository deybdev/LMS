namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateAnnouncementModel : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Announcements", "Title");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Announcements", "Title", c => c.String(nullable: false, maxLength: 500));
        }
    }
}

namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class asnasa : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Classworks", "IsScheduled", c => c.Boolean(nullable: false));
            AddColumn("dbo.Classworks", "ScheduledPublishDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Classworks", "ScheduledPublishDate");
            DropColumn("dbo.Classworks", "IsScheduled");
        }
    }
}

namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class asod : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Classworks", "IsManualEntry", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Classworks", "IsManualEntry");
        }
    }
}

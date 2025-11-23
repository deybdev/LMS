namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class udpdatabase : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Classworks", "Description", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Classworks", "Description", c => c.String(nullable: false));
        }
    }
}

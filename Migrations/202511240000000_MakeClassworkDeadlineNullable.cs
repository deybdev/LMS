namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MakeClassworkDeadlineNullable : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Classworks", "Deadline", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Classworks", "Deadline", c => c.DateTime(nullable: false));
        }
    }
}


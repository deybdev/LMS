namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveStatusProgram : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Programs", "ProgramName", c => c.String());
            AlterColumn("dbo.Programs", "ProgramCode", c => c.String());
            DropColumn("dbo.Programs", "IsActive");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Programs", "IsActive", c => c.Boolean(nullable: false));
            AlterColumn("dbo.Programs", "ProgramCode", c => c.String(nullable: false));
            AlterColumn("dbo.Programs", "ProgramName", c => c.String(nullable: false));
        }
    }
}

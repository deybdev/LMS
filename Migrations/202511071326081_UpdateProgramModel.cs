namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateProgramModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Programs", "ProgramDuration", c => c.Int(nullable: false));
            DropColumn("dbo.Programs", "DurationYears");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Programs", "DurationYears", c => c.Int(nullable: false));
            DropColumn("dbo.Programs", "ProgramDuration");
        }
    }
}

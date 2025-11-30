namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddResetPassword : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "ResetToken", c => c.String());
            AddColumn("dbo.Users", "ResetTokenExpiry", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "ResetTokenExpiry");
            DropColumn("dbo.Users", "ResetToken");
        }
    }
}

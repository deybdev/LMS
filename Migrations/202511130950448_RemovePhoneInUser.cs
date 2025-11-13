namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovePhoneInUser : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Users", "PhoneNumber");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Users", "PhoneNumber", c => c.String());
        }
    }
}

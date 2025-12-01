namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProfileDetailsToUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "DateOfBirth", c => c.DateTime());
            AddColumn("dbo.Users", "PhoneNumber", c => c.String());
            AddColumn("dbo.Users", "Address", c => c.String());
            AddColumn("dbo.Users", "DepartmentId", c => c.Int());
            AddColumn("dbo.Users", "EmergencyContactName", c => c.String());
            AddColumn("dbo.Users", "EmergencyContactPhone", c => c.String());
            AddColumn("dbo.Users", "EmergencyContactRelationship", c => c.String());
            CreateIndex("dbo.Users", "DepartmentId");
            AddForeignKey("dbo.Users", "DepartmentId", "dbo.Departments", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Users", "DepartmentId", "dbo.Departments");
            DropIndex("dbo.Users", new[] { "DepartmentId" });
            DropColumn("dbo.Users", "EmergencyContactRelationship");
            DropColumn("dbo.Users", "EmergencyContactPhone");
            DropColumn("dbo.Users", "EmergencyContactName");
            DropColumn("dbo.Users", "DepartmentId");
            DropColumn("dbo.Users", "Address");
            DropColumn("dbo.Users", "PhoneNumber");
            DropColumn("dbo.Users", "DateOfBirth");
        }
    }
}
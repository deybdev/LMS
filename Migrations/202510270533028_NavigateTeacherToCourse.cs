namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NavigateTeacherToCourse : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Courses", "TeacherId");
            AddForeignKey("dbo.Courses", "TeacherId", "dbo.Users", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Courses", "TeacherId", "dbo.Users");
            DropIndex("dbo.Courses", new[] { "TeacherId" });
        }
    }
}

namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdNanaman : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.CourseUsers", "CourseId", "dbo.Courses");
            DropIndex("dbo.CourseUsers", new[] { "CourseId" });
            DropTable("dbo.CourseUsers");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.CourseUsers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CourseId = c.Int(nullable: false),
                        StudentId = c.Int(nullable: false),
                        DateAdded = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateIndex("dbo.CourseUsers", "CourseId");
            AddForeignKey("dbo.CourseUsers", "CourseId", "dbo.Courses", "Id", cascadeDelete: true);
        }
    }
}

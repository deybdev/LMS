namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addansd : DbMigration
    {
        public override void Up()
        {
            // First, add the column as nullable
            AddColumn("dbo.Announcements", "CreatedByUserId", c => c.Int());
            
            // Update existing announcements to have a valid CreatedByUserId
            // Set to the teacher who owns the course section (or first admin/teacher if needed)
            Sql(@"
                UPDATE a
                SET a.CreatedByUserId = tcs.TeacherId
                FROM dbo.Announcements a
                INNER JOIN dbo.TeacherCourseSections tcs ON a.TeacherCourseSectionId = tcs.Id
                WHERE a.CreatedByUserId IS NULL
            ");
            
            // Make the column non-nullable
            AlterColumn("dbo.Announcements", "CreatedByUserId", c => c.Int(nullable: false));
            
            // Create index
            CreateIndex("dbo.Announcements", "CreatedByUserId");
            
            // Add foreign key with NO ACTION to prevent cascade conflicts
            AddForeignKey("dbo.Announcements", "CreatedByUserId", "dbo.Users", "Id", cascadeDelete: false);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Announcements", "CreatedByUserId", "dbo.Users");
            DropIndex("dbo.Announcements", new[] { "CreatedByUserId" });
            DropColumn("dbo.Announcements", "CreatedByUserId");
        }
    }
}

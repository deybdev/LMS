namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateCourseModelFixed : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Courses", "YearLevel", c => c.Int(nullable: false));
            AddColumn("dbo.Courses", "ProgramId", c => c.Int(nullable: false));
            AlterColumn("dbo.Courses", "Semester", c => c.Int(nullable: false));
            CreateIndex("dbo.Courses", "ProgramId");
            AddForeignKey("dbo.Courses", "ProgramId", "dbo.Programs", "Id", cascadeDelete: true);
            DropColumn("dbo.Courses", "Department");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Courses", "Department", c => c.String());
            DropForeignKey("dbo.Courses", "ProgramId", "dbo.Programs");
            DropIndex("dbo.Courses", new[] { "ProgramId" });
            AlterColumn("dbo.Courses", "Semester", c => c.String());
            DropColumn("dbo.Courses", "ProgramId");
            DropColumn("dbo.Courses", "YearLevel");
        }
    }
}

namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updult : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TeacherCourses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TeacherId = c.Int(nullable: false),
                        CurriculumCourseId = c.Int(nullable: false),
                        DateAssigned = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CurriculumCourses", t => t.CurriculumCourseId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.TeacherId, cascadeDelete: true)
                .Index(t => t.TeacherId)
                .Index(t => t.CurriculumCourseId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TeacherCourses", "TeacherId", "dbo.Users");
            DropForeignKey("dbo.TeacherCourses", "CurriculumCourseId", "dbo.CurriculumCourses");
            DropIndex("dbo.TeacherCourses", new[] { "CurriculumCourseId" });
            DropIndex("dbo.TeacherCourses", new[] { "TeacherId" });
            DropTable("dbo.TeacherCourses");
        }
    }
}

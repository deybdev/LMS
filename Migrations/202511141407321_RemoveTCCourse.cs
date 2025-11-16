namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveTCCourse : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.TeacherCourses", "CurriculumCourseId", "dbo.CurriculumCourses");
            DropForeignKey("dbo.TeacherCourses", "TeacherId", "dbo.Users");
            DropIndex("dbo.TeacherCourses", new[] { "TeacherId" });
            DropIndex("dbo.TeacherCourses", new[] { "CurriculumCourseId" });
            DropTable("dbo.TeacherCourses");
        }
        
        public override void Down()
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
                .PrimaryKey(t => t.Id);
            
            CreateIndex("dbo.TeacherCourses", "CurriculumCourseId");
            CreateIndex("dbo.TeacherCourses", "TeacherId");
            AddForeignKey("dbo.TeacherCourses", "TeacherId", "dbo.Users", "Id", cascadeDelete: true);
            AddForeignKey("dbo.TeacherCourses", "CurriculumCourseId", "dbo.CurriculumCourses", "Id", cascadeDelete: true);
        }
    }
}

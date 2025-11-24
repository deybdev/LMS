namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAttendance : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AttendanceRecords",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TeacherCourseSectionId = c.Int(nullable: false),
                        StudentId = c.Int(nullable: false),
                        AttendanceDate = c.DateTime(nullable: false, storeType: "date"),
                        Status = c.String(nullable: false, maxLength: 20),
                        MarkedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.StudentId, cascadeDelete: true)
                .ForeignKey("dbo.TeacherCourseSections", t => t.TeacherCourseSectionId)
                .Index(t => t.TeacherCourseSectionId)
                .Index(t => t.StudentId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AttendanceRecords", "TeacherCourseSectionId", "dbo.TeacherCourseSections");
            DropForeignKey("dbo.AttendanceRecords", "StudentId", "dbo.Users");
            DropIndex("dbo.AttendanceRecords", new[] { "StudentId" });
            DropIndex("dbo.AttendanceRecords", new[] { "TeacherCourseSectionId" });
            DropTable("dbo.AttendanceRecords");
        }
    }
}

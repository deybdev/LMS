namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MigrateAnnouncement : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AnnouncementComments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AnnouncementId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        Comment = c.String(nullable: false),
                        ParentCommentId = c.Int(),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Announcements", t => t.AnnouncementId, cascadeDelete: true)
                .ForeignKey("dbo.AnnouncementComments", t => t.ParentCommentId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.AnnouncementId)
                .Index(t => t.UserId)
                .Index(t => t.ParentCommentId);
            
            CreateTable(
                "dbo.Announcements",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TeacherCourseSectionId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 500),
                        Content = c.String(nullable: false),
                        PostedAt = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TeacherCourseSections", t => t.TeacherCourseSectionId)
                .Index(t => t.TeacherCourseSectionId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AnnouncementComments", "UserId", "dbo.Users");
            DropForeignKey("dbo.AnnouncementComments", "ParentCommentId", "dbo.AnnouncementComments");
            DropForeignKey("dbo.Announcements", "TeacherCourseSectionId", "dbo.TeacherCourseSections");
            DropForeignKey("dbo.AnnouncementComments", "AnnouncementId", "dbo.Announcements");
            DropIndex("dbo.Announcements", new[] { "TeacherCourseSectionId" });
            DropIndex("dbo.AnnouncementComments", new[] { "ParentCommentId" });
            DropIndex("dbo.AnnouncementComments", new[] { "UserId" });
            DropIndex("dbo.AnnouncementComments", new[] { "AnnouncementId" });
            DropTable("dbo.Announcements");
            DropTable("dbo.AnnouncementComments");
        }
    }
}

namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMaterialDB : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MaterialFiles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MaterialId = c.Int(nullable: false),
                        FileName = c.String(),
                        FilePath = c.String(),
                        SizeInMB = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Materials", t => t.MaterialId, cascadeDelete: true)
                .Index(t => t.MaterialId);
            
            CreateTable(
                "dbo.Materials",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CourseId = c.Int(nullable: false),
                        Title = c.String(),
                        Type = c.String(),
                        Description = c.String(),
                        UploadedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MaterialFiles", "MaterialId", "dbo.Materials");
            DropIndex("dbo.MaterialFiles", new[] { "MaterialId" });
            DropTable("dbo.Materials");
            DropTable("dbo.MaterialFiles");
        }
    }
}

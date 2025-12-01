namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ajksd : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MaterialComments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MaterialId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        Comment = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Materials", t => t.MaterialId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.MaterialId)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MaterialComments", "UserId", "dbo.Users");
            DropForeignKey("dbo.MaterialComments", "MaterialId", "dbo.Materials");
            DropIndex("dbo.MaterialComments", new[] { "UserId" });
            DropIndex("dbo.MaterialComments", new[] { "MaterialId" });
            DropTable("dbo.MaterialComments");
        }
    }
}

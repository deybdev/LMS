namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateDecimalInMaterialFIle : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.MaterialFiles", "SizeInMB", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.MaterialFiles", "SizeInMB", c => c.Double(nullable: false));
        }
    }
}

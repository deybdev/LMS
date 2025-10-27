namespace LMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class FixRelationships : DbMigration
    {
        public override void Up()
        {
            // Manually drop the FK constraint using SQL
            Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_MaterialFiles_Materials')
                    ALTER TABLE [dbo].[MaterialFiles] DROP CONSTRAINT [FK_MaterialFiles_Materials]
            ");

            // Drop other foreign keys (if they exist)
            Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.MaterialFiles_dbo.Materials_Material_Id')
                    ALTER TABLE [dbo].[MaterialFiles] DROP CONSTRAINT [FK_dbo.MaterialFiles_dbo.Materials_Material_Id]
            ");

            Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.MaterialFiles_dbo.Materials_Material_Id1')
                    ALTER TABLE [dbo].[MaterialFiles] DROP CONSTRAINT [FK_dbo.MaterialFiles_dbo.Materials_Material_Id1]
            ");

            // Drop indexes (with checks)
            Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MaterialId' AND object_id = OBJECT_ID('dbo.MaterialFiles'))
                    DROP INDEX [IX_MaterialId] ON [dbo].[MaterialFiles]
            ");

            Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Material_Id' AND object_id = OBJECT_ID('dbo.MaterialFiles'))
                    DROP INDEX [IX_Material_Id] ON [dbo].[MaterialFiles]
            ");

            Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Material_Id1' AND object_id = OBJECT_ID('dbo.MaterialFiles'))
                    DROP INDEX [IX_Material_Id1] ON [dbo].[MaterialFiles]
            ");

            // Drop the MaterialId column if it exists
            Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'MaterialId' AND object_id = OBJECT_ID('dbo.MaterialFiles'))
                    ALTER TABLE [dbo].[MaterialFiles] DROP COLUMN [MaterialId]
            ");

            // Drop the Material_Id column only if it exists
            Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'Material_Id' AND object_id = OBJECT_ID('dbo.MaterialFiles'))
                    ALTER TABLE [dbo].[MaterialFiles] DROP COLUMN [Material_Id]
            ");

            // Rename Material_Id1 to MaterialId or create new column
            Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'Material_Id1' AND object_id = OBJECT_ID('dbo.MaterialFiles'))
                BEGIN
                    EXEC sp_rename 'dbo.MaterialFiles.Material_Id1', 'MaterialId', 'COLUMN'
                END
                ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'MaterialId' AND object_id = OBJECT_ID('dbo.MaterialFiles'))
                BEGIN
                    -- Add MaterialId with default value
                    ALTER TABLE [dbo].[MaterialFiles] ADD [MaterialId] INT NOT NULL DEFAULT 0
                    
                    -- Find and drop the default constraint
                    DECLARE @ConstraintName NVARCHAR(200)
                    SELECT @ConstraintName = name 
                    FROM sys.default_constraints 
                    WHERE parent_object_id = OBJECT_ID('dbo.MaterialFiles') 
                    AND parent_column_id = (SELECT column_id FROM sys.columns WHERE name = 'MaterialId' AND object_id = OBJECT_ID('dbo.MaterialFiles'))
                    
                    IF @ConstraintName IS NOT NULL
                        EXEC('ALTER TABLE [dbo].[MaterialFiles] DROP CONSTRAINT [' + @ConstraintName + ']')
                END
            ");

            // Delete orphaned records (MaterialFiles records that don't have a matching Material)
            Sql(@"
                DELETE FROM [dbo].[MaterialFiles]
                WHERE MaterialId NOT IN (SELECT Id FROM [dbo].[Materials])
            ");

            // Make sure MaterialId column is non-nullable
            Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'MaterialId' AND object_id = OBJECT_ID('dbo.MaterialFiles'))
                    ALTER TABLE [dbo].[MaterialFiles] ALTER COLUMN [MaterialId] INT NOT NULL
            ");

            // Create new index
            CreateIndex("dbo.MaterialFiles", "MaterialId");

            // Add new foreign key with cascade delete
            AddForeignKey("dbo.MaterialFiles", "MaterialId", "dbo.Materials", "Id", cascadeDelete: true);
        }

        public override void Down()
        {
            // Reverse the process
            DropForeignKey("dbo.MaterialFiles", "MaterialId", "dbo.Materials");
            DropIndex("dbo.MaterialFiles", new[] { "MaterialId" });

            Sql("EXEC sp_rename 'dbo.MaterialFiles.MaterialId', 'Material_Id1', 'COLUMN'");

            AddColumn("dbo.MaterialFiles", "Material_Id", c => c.Int());
            AddColumn("dbo.MaterialFiles", "MaterialId", c => c.Int(nullable: false));

            CreateIndex("dbo.MaterialFiles", "MaterialId");
            CreateIndex("dbo.MaterialFiles", "Material_Id");
            CreateIndex("dbo.MaterialFiles", "Material_Id1");

            AddForeignKey("dbo.MaterialFiles", "MaterialId", "dbo.Materials", "Id");
            AddForeignKey("dbo.MaterialFiles", "Material_Id", "dbo.Materials", "Id");
            AddForeignKey("dbo.MaterialFiles", "Material_Id1", "dbo.Materials", "Id");
        }
    }
}
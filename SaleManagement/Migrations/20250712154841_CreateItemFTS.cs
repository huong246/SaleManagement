using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaleManagement.Migrations
{
    /// <inheritdoc />
    public partial class CreateItemFTS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"CREATE VIRTUAL TABLE ItemsFTS USING fts5(
            Name,
            Description,
            content='Items',
            content_rowid='Id'
        );"
            );

            // Tạo trigger để tự động cập nhật bảng FTS5 khi bảng Items thay đổi
            migrationBuilder.Sql(
                @"CREATE TRIGGER Items_after_insert AFTER INSERT ON Items
          BEGIN
            INSERT INTO ItemsFTS(rowid, Name, Description) VALUES (new.Id, new.Name, new.Description);
          END;"
            );

            migrationBuilder.Sql(
                @"CREATE TRIGGER Items_after_delete AFTER DELETE ON Items
          BEGIN
            INSERT INTO ItemsFTS(ItemsFTS, rowid, Name, Description) VALUES ('delete', old.Id, old.Name, old.Description);
          END;"
            );

            migrationBuilder.Sql(
                @"CREATE TRIGGER Items_after_update AFTER UPDATE ON Items
          BEGIN
            INSERT INTO ItemsFTS(ItemsFTS, rowid, Name, Description) VALUES ('delete', old.Id, old.Name, old.Description);
            INSERT INTO ItemsFTS(rowid, Name, Description) VALUES (new.Id, new.Name, new.Description);
          END;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Items_after_update;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Items_after_delete;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Items_after_insert;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ItemsFTS;");
        }
    }
}

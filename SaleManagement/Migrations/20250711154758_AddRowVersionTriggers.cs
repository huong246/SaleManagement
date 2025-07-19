using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaleManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Trigger cho bảng Items
            migrationBuilder.Sql(
                @"
                CREATE TRIGGER SetItemRowVersionOnInsert
                AFTER INSERT ON Items
                BEGIN
                    UPDATE Items
                    SET RowVersion = randomblob(8)
                    WHERE rowid = NEW.rowid;
                END;
                ");

            migrationBuilder.Sql(
                @"
                CREATE TRIGGER SetItemRowVersionOnUpdate
                AFTER UPDATE ON Items
                BEGIN
                    UPDATE Items
                    SET RowVersion = randomblob(8)
                    WHERE rowid = NEW.rowid;
                END;
                ");

            // Trigger cho bảng Vouchers
            migrationBuilder.Sql(
                @"
                CREATE TRIGGER SetVoucherRowVersionOnInsert
                AFTER INSERT ON Vouchers
                BEGIN
                    UPDATE Vouchers
                    SET RowVersion = randomblob(8)
                    WHERE rowid = NEW.rowid;
                END;
                ");

            migrationBuilder.Sql(
                @"
                CREATE TRIGGER SetVoucherRowVersionOnUpdate
                AFTER UPDATE ON Vouchers
                BEGIN
                    UPDATE Vouchers
                    SET RowVersion = randomblob(8)
                    WHERE rowid = NEW.rowid;
                END;
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SetItemRowVersionOnInsert;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SetItemRowVersionOnUpdate;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SetVoucherRowVersionOnInsert;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SetVoucherRowVersionOnUpdate;");
        }
    }
}
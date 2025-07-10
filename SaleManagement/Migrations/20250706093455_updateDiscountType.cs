using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaleManagement.Migrations
{
    /// <inheritdoc />
    public partial class updateDiscountType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiscountType",
                table: "Vouchers",
                newName: "TargetType");

            migrationBuilder.AddColumn<int>(
                name: "MethodType",
                table: "Vouchers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MethodType",
                table: "Vouchers");

            migrationBuilder.RenameColumn(
                name: "TargetType",
                table: "Vouchers",
                newName: "DiscountType");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncChain.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPricingAndImageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GiaNhap",
                table: "SanPham",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HinhAnhUrl",
                table: "SanPham",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MoTa",
                table: "SanPham",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiaNhap",
                table: "SanPham");

            migrationBuilder.DropColumn(
                name: "HinhAnhUrl",
                table: "SanPham");

            migrationBuilder.DropColumn(
                name: "MoTa",
                table: "SanPham");
        }
    }
}

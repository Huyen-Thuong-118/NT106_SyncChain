using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SyncChain.API.Data;

#nullable disable

namespace SyncChain.API.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260624150000_AddOrderDeliveryInformation")]
public partial class AddOrderDeliveryInformation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("TenNguoiNhan", "DonHang", maxLength: 150, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("SoDienThoai", "DonHang", maxLength: 30, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("EmailNguoiNhan", "DonHang", maxLength: 150, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("DiaChiGiaoHang", "DonHang", maxLength: 500, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("TinhThanh", "DonHang", maxLength: 100, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("PhuongXa", "DonHang", maxLength: 100, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("LoaiDichVu", "DonHang", maxLength: 30, nullable: false, defaultValue: "Tieu chuan");
        migrationBuilder.AddColumn<decimal>("TrongLuongKg", "DonHang", nullable: false, defaultValue: 1m);
        migrationBuilder.AddColumn<string>("GhiChu", "DonHang", maxLength: 500, nullable: false, defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("TenNguoiNhan", "DonHang");
        migrationBuilder.DropColumn("SoDienThoai", "DonHang");
        migrationBuilder.DropColumn("EmailNguoiNhan", "DonHang");
        migrationBuilder.DropColumn("DiaChiGiaoHang", "DonHang");
        migrationBuilder.DropColumn("TinhThanh", "DonHang");
        migrationBuilder.DropColumn("PhuongXa", "DonHang");
        migrationBuilder.DropColumn("LoaiDichVu", "DonHang");
        migrationBuilder.DropColumn("TrongLuongKg", "DonHang");
        migrationBuilder.DropColumn("GhiChu", "DonHang");
    }
}

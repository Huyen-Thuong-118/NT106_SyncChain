using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SyncChain.API.Data;

#nullable disable

namespace SyncChain.API.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260618100000_AddShippingManagement")]
public partial class AddShippingManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "VanChuyen",
            columns: table => new
            {
                MaVanChuyen = table.Column<int>(type: "integer", nullable: false)
                    .Annotation(
                        "Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MaDonHang = table.Column<int>(type: "integer", nullable: false),
                DonViVanChuyen = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MaVanDon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                PhiVanChuyen = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TrangThaiGiaoHang = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NgayGiaoDuKien = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                NgayGiaoThucTe = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VanChuyen", x => x.MaVanChuyen);
                table.CheckConstraint(
                    "CK_VanChuyen_PhiVanChuyen_NonNegative",
                    "\"PhiVanChuyen\" >= 0");
                table.ForeignKey(
                    name: "FK_VanChuyen_DonHang_MaDonHang",
                    column: x => x.MaDonHang,
                    principalTable: "DonHang",
                    principalColumn: "MaDonHang",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "LichSuVanChuyen",
            columns: table => new
            {
                MaLichSu = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation(
                        "Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MaVanChuyen = table.Column<int>(type: "integer", nullable: false),
                TrangThaiCu = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                TrangThaiMoi = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ThoiGian = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                MaNguoiDung = table.Column<int>(type: "integer", nullable: true),
                GhiChu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LichSuVanChuyen", x => x.MaLichSu);
                table.ForeignKey(
                    name: "FK_LichSuVanChuyen_VanChuyen_MaVanChuyen",
                    column: x => x.MaVanChuyen,
                    principalTable: "VanChuyen",
                    principalColumn: "MaVanChuyen",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_VanChuyen_MaDonHang",
            table: "VanChuyen",
            column: "MaDonHang",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_VanChuyen_MaVanDon",
            table: "VanChuyen",
            column: "MaVanDon",
            unique: true,
            filter: "\"MaVanDon\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_LichSuVanChuyen_MaVanChuyen",
            table: "LichSuVanChuyen",
            column: "MaVanChuyen");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LichSuVanChuyen");
        migrationBuilder.DropTable(name: "VanChuyen");
    }
}

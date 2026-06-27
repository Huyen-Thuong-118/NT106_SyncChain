using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SyncChain.API.Data;

#nullable disable

namespace SyncChain.API.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260618090000_AddOrderAuditLog")]
public partial class AddOrderAuditLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLog",
            columns: table => new
            {
                MaAudit = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation(
                        "Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                HanhDong = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LoaiDoiTuong = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MaDoiTuong = table.Column<int>(type: "integer", nullable: false),
                MaNguoiDung = table.Column<int>(type: "integer", nullable: true),
                ThoiGian = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DuLieu = table.Column<string>(type: "jsonb", nullable: false),
                TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLog", x => x.MaAudit);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLog_LoaiDoiTuong_MaDoiTuong_ThoiGian",
            table: "AuditLog",
            columns: new[] { "LoaiDoiTuong", "MaDoiTuong", "ThoiGian" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditLog");
    }
}

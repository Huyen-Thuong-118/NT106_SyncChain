using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SyncChain.API.Data;

#nullable disable

namespace SyncChain.API.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260618110000_ExpandAuditLog")]
public partial class ExpandAuditLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "DuLieu",
            table: "AuditLog",
            newName: "Metadata");

        migrationBuilder.Sql("""
            ALTER TABLE "AuditLog"
            ALTER COLUMN "MaDoiTuong" TYPE character varying(100)
            USING "MaDoiTuong"::text;
            ALTER TABLE "AuditLog"
            ALTER COLUMN "MaDoiTuong" DROP NOT NULL;
            """);

        migrationBuilder.AddColumn<string>(
            name: "DuLieuTruoc", table: "AuditLog", type: "jsonb",
            nullable: false, defaultValueSql: "'{}'::jsonb");
        migrationBuilder.AddColumn<string>(
            name: "DuLieuSau", table: "AuditLog", type: "jsonb",
            nullable: false, defaultValueSql: "'{}'::jsonb");
        migrationBuilder.AddColumn<string>(
            name: "TenDangNhap", table: "AuditLog", type: "character varying(150)",
            maxLength: 150, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "VaiTro", table: "AuditLog", type: "character varying(50)",
            maxLength: 50, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "TrangThaiKetQua", table: "AuditLog", type: "character varying(20)",
            maxLength: 20, nullable: false, defaultValue: "SUCCESS");
        migrationBuilder.AddColumn<string>(
            name: "IpAddress", table: "AuditLog", type: "character varying(64)",
            maxLength: 64, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "UserAgent", table: "AuditLog", type: "character varying(500)",
            maxLength: 500, nullable: false, defaultValue: "");

        migrationBuilder.Sql("""
            UPDATE "AuditLog" a
            SET "TenDangNhap" = COALESCE(u."TenDangNhap", ''),
                "VaiTro" = CASE u."MaVaiTro"
                    WHEN 1 THEN 'customer' WHEN 2 THEN 'staff'
                    WHEN 3 THEN 'manager' WHEN 4 THEN 'admin' ELSE '' END
            FROM "NguoiDung" u
            WHERE a."MaNguoiDung" = u."MaNguoiDung";

            UPDATE "AuditLog"
            SET "HanhDong" = CASE "HanhDong"
                WHEN 'CREATE_ORDER' THEN 'CREATE'
                WHEN 'CREATE_SHIPPING' THEN 'CREATE'
                WHEN 'UPDATE_SHIPPING' THEN 'UPDATE'
                WHEN 'UPDATE_SHIPPING_STATUS' THEN 'SHIPPING_STATUS_CHANGE'
                ELSE "HanhDong" END;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_AuditLog_MaNguoiDung_ThoiGian",
            table: "AuditLog",
            columns: new[] { "MaNguoiDung", "ThoiGian" });
        migrationBuilder.CreateIndex(
            name: "IX_AuditLog_HanhDong_ThoiGian",
            table: "AuditLog",
            columns: new[] { "HanhDong", "ThoiGian" });
        migrationBuilder.CreateIndex(
            name: "IX_AuditLog_TraceId",
            table: "AuditLog",
            column: "TraceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_AuditLog_MaNguoiDung_ThoiGian", table: "AuditLog");
        migrationBuilder.DropIndex(name: "IX_AuditLog_HanhDong_ThoiGian", table: "AuditLog");
        migrationBuilder.DropIndex(name: "IX_AuditLog_TraceId", table: "AuditLog");

        migrationBuilder.DropColumn(name: "DuLieuTruoc", table: "AuditLog");
        migrationBuilder.DropColumn(name: "DuLieuSau", table: "AuditLog");
        migrationBuilder.DropColumn(name: "TenDangNhap", table: "AuditLog");
        migrationBuilder.DropColumn(name: "VaiTro", table: "AuditLog");
        migrationBuilder.DropColumn(name: "TrangThaiKetQua", table: "AuditLog");
        migrationBuilder.DropColumn(name: "IpAddress", table: "AuditLog");
        migrationBuilder.DropColumn(name: "UserAgent", table: "AuditLog");

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM "AuditLog"
                    WHERE "MaDoiTuong" IS NULL
                       OR "MaDoiTuong" !~ '^[0-9]+$') THEN
                    RAISE EXCEPTION 'Cannot downgrade AuditLog: non-numeric entity IDs exist';
                END IF;
            END $$;
            ALTER TABLE "AuditLog"
            ALTER COLUMN "MaDoiTuong" TYPE integer
            USING NULLIF("MaDoiTuong", '')::integer;
            ALTER TABLE "AuditLog"
            ALTER COLUMN "MaDoiTuong" SET NOT NULL;
            """);

        migrationBuilder.RenameColumn(
            name: "Metadata",
            table: "AuditLog",
            newName: "DuLieu");
    }
}

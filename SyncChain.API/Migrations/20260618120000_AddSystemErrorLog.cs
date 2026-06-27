using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SyncChain.API.Data;

#nullable disable

namespace SyncChain.API.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260618120000_AddSystemErrorLog")]
public partial class AddSystemErrorLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SystemErrorLog",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                RequestPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                HttpMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                StatusCode = table.Column<int>(type: "integer", nullable: true),
                ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                ExceptionType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                StackTrace = table.Column<string>(type: "text", nullable: true),
                DetailsJson = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                UserId = table.Column<int>(type: "integer", nullable: true),
                Username = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SystemErrorLog", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SystemErrorLog_TraceId",
            table: "SystemErrorLog",
            column: "TraceId");
        migrationBuilder.CreateIndex(
            name: "IX_SystemErrorLog_CreatedAt",
            table: "SystemErrorLog",
            column: "CreatedAt");
        migrationBuilder.CreateIndex(
            name: "IX_SystemErrorLog_ErrorCode",
            table: "SystemErrorLog",
            column: "ErrorCode");
        migrationBuilder.CreateIndex(
            name: "IX_SystemErrorLog_StatusCode",
            table: "SystemErrorLog",
            column: "StatusCode");
        migrationBuilder.CreateIndex(
            name: "IX_SystemErrorLog_UserId",
            table: "SystemErrorLog",
            column: "UserId");
        migrationBuilder.CreateIndex(
            name: "IX_SystemErrorLog_RequestPath",
            table: "SystemErrorLog",
            column: "RequestPath");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SystemErrorLog");
    }
}

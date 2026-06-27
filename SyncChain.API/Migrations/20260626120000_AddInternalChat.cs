using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SyncChain.API.Migrations
{
    public partial class AddInternalChat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatConversation",
                columns: table => new
                {
                    MaCuocTroChuyen = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaNguoiDung1 = table.Column<int>(type: "integer", nullable: false),
                    MaNguoiDung2 = table.Column<int>(type: "integer", nullable: false),
                    LaNhom = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TenNhom = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, defaultValue: ""),
                    AnhDaiDien = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    MaNguoiTao = table.Column<int>(type: "integer", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CapNhatLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatConversation", x => x.MaCuocTroChuyen);
                });

            migrationBuilder.CreateTable(
                name: "ChatParticipant",
                columns: table => new
                {
                    MaThanhVien = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaCuocTroChuyen = table.Column<int>(type: "integer", nullable: false),
                    MaNguoiDung = table.Column<int>(type: "integer", nullable: false),
                    ThamGiaLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatParticipant", x => x.MaThanhVien);
                    table.ForeignKey(
                        name: "FK_ChatParticipant_ChatConversation_MaCuocTroChuyen",
                        column: x => x.MaCuocTroChuyen,
                        principalTable: "ChatConversation",
                        principalColumn: "MaCuocTroChuyen",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessage",
                columns: table => new
                {
                    MaTinNhan = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaCuocTroChuyen = table.Column<int>(type: "integer", nullable: false),
                    MaNguoiGui = table.Column<int>(type: "integer", nullable: false),
                    MaNguoiNhan = table.Column<int>(type: "integer", nullable: false),
                    NoiDung = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LoaiTinNhan = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "text"),
                    TenFile = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: ""),
                    DuongDanFile = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    TrangThaiCuocGoi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    ThoiLuongCuocGoiGiay = table.Column<int>(type: "integer", nullable: true),
                    DaGhim = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DaThuHoi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CamXuc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    ThoiGianGui = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ThoiGianDoc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => x.MaTinNhan);
                    table.ForeignKey(
                        name: "FK_ChatMessage_ChatConversation_MaCuocTroChuyen",
                        column: x => x.MaCuocTroChuyen,
                        principalTable: "ChatConversation",
                        principalColumn: "MaCuocTroChuyen",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversation_CapNhatLuc",
                table: "ChatConversation",
                column: "CapNhatLuc");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversation_MaNguoiDung1_MaNguoiDung2",
                table: "ChatConversation",
                columns: new[] { "MaNguoiDung1", "MaNguoiDung2" },
                unique: true,
                filter: "\"LaNhom\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_MaCuocTroChuyen_ThoiGianGui",
                table: "ChatMessage",
                columns: new[] { "MaCuocTroChuyen", "ThoiGianGui" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_MaNguoiNhan_ThoiGianDoc",
                table: "ChatMessage",
                columns: new[] { "MaNguoiNhan", "ThoiGianDoc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipant_MaCuocTroChuyen_MaNguoiDung",
                table: "ChatParticipant",
                columns: new[] { "MaCuocTroChuyen", "MaNguoiDung" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "ChatPoll",
                columns: table => new
                {
                    MaThamDo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaTinNhan = table.Column<long>(type: "bigint", nullable: false),
                    CauHoi = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ChoPhepNhieuLuaChon = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ChoPhepThemLuaChon = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DaKhoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    KetThucLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatPoll", x => x.MaThamDo);
                    table.ForeignKey(
                        name: "FK_ChatPoll_ChatMessage_MaTinNhan",
                        column: x => x.MaTinNhan,
                        principalTable: "ChatMessage",
                        principalColumn: "MaTinNhan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatPollOption",
                columns: table => new
                {
                    MaLuaChon = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaThamDo = table.Column<int>(type: "integer", nullable: false),
                    NoiDung = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatPollOption", x => x.MaLuaChon);
                    table.ForeignKey(
                        name: "FK_ChatPollOption_ChatPoll_MaThamDo",
                        column: x => x.MaThamDo,
                        principalTable: "ChatPoll",
                        principalColumn: "MaThamDo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatPollVote",
                columns: table => new
                {
                    MaBinhChon = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaLuaChon = table.Column<int>(type: "integer", nullable: false),
                    MaNguoiDung = table.Column<int>(type: "integer", nullable: false),
                    BinhChonLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatPollVote", x => x.MaBinhChon);
                    table.ForeignKey(
                        name: "FK_ChatPollVote_ChatPollOption_MaLuaChon",
                        column: x => x.MaLuaChon,
                        principalTable: "ChatPollOption",
                        principalColumn: "MaLuaChon",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatPoll_MaTinNhan",
                table: "ChatPoll",
                column: "MaTinNhan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatPollOption_MaThamDo",
                table: "ChatPollOption",
                column: "MaThamDo");

            migrationBuilder.CreateIndex(
                name: "IX_ChatPollVote_MaLuaChon_MaNguoiDung",
                table: "ChatPollVote",
                columns: new[] { "MaLuaChon", "MaNguoiDung" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChatPollVote");
            migrationBuilder.DropTable(name: "ChatPollOption");
            migrationBuilder.DropTable(name: "ChatPoll");
            migrationBuilder.DropTable(name: "ChatMessage");
            migrationBuilder.DropTable(name: "ChatParticipant");
            migrationBuilder.DropTable(name: "ChatConversation");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SyncChain.API.Models;

namespace SyncChain.API.Data;
public class AppDbContext : DbContext
{
    // CÃ¡c báº£ng tÃ i khoáº£n vÃ  phÃ¢n quyá»n.
    public DbSet<NguoiDung> NguoiDung { get; set; }
    public DbSet<PhanQuyen> PhanQuyen { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // CÃ¡c báº£ng sáº£n pháº©m, Ä‘Æ¡n hÃ ng vÃ  giao dá»‹ch kho.
    public DbSet<SanPham> SanPham { get; set; }
    public DbSet<DanhMucSanPham> DanhMucSanPham { get; set; }

    public DbSet<DonHang> DonHang { get; set; }
    public DbSet<ChiTietDonHang> ChiTietDonHang { get; set; }
    public DbSet<GiaoDichKho> GiaoDichKho { get; set; }
    public DbSet<AuditLog> AuditLog { get; set; }
    public DbSet<VanChuyen> VanChuyen { get; set; }
    public DbSet<LichSuVanChuyen> LichSuVanChuyen { get; set; }
    public DbSet<PhieuNhapKho> PhieuNhapKho { get; set; }
    public DbSet<ChiTietPhieuNhap> ChiTietPhieuNhap { get; set; }
    public DbSet<PhieuXuatKho> PhieuXuatKho { get; set; }
    public DbSet<ChiTietPhieuXuat> ChiTietPhieuXuat { get; set; }
    public DbSet<SystemErrorLog> SystemErrorLog { get; set; }
    public DbSet<ChatConversation> ChatConversations { get; set; }
    public DbSet<ChatMessageEntity> ChatMessages { get; set; }
    public DbSet<ChatParticipant> ChatParticipants { get; set; }
    public DbSet<ChatPoll> ChatPolls { get; set; }
    public DbSet<ChatPollOption> ChatPollOptions { get; set; }
    public DbSet<ChatPollVote> ChatPollVotes { get; set; }

    // Địa chỉ giao hàng của khách hàng (module khách hàng).
    public DbSet<DiaChi> DiaChi { get; set; }

    // Thông báo cho khách hàng (module khách hàng).
    public DbSet<ThongBao> ThongBao { get; set; }

    // Giỏ hàng khách hàng (module khách hàng).
    public DbSet<GioHang> GioHang { get; set; }
    public DbSet<ChiTietGioHang> ChiTietGioHang { get; set; }

    // Thanh toán đơn hàng (module khách hàng).
    public DbSet<ThanhToan> ThanhToan { get; set; }

    // Cáº¥u hÃ¬nh quan há»‡ giá»¯a Ä‘Æ¡n hÃ ng, sáº£n pháº©m vÃ  lá»‹ch sá»­ kho.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DonHang>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        modelBuilder.Entity<DonHang>()
            .Property(x => x.ConcurrencyVersion)
            .IsConcurrencyToken();

        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => new { x.MaNguoiDung, x.ThoiGian });
        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => new { x.LoaiDoiTuong, x.MaDoiTuong, x.ThoiGian });
        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => new { x.HanhDong, x.ThoiGian });
        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => x.TraceId);
        modelBuilder.Entity<AuditLog>().Property(x => x.DuLieuTruoc).HasColumnType("jsonb");
        modelBuilder.Entity<AuditLog>().Property(x => x.DuLieuSau).HasColumnType("jsonb");
        modelBuilder.Entity<AuditLog>().Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<SystemErrorLog>()
            .HasIndex(x => x.TraceId);
        modelBuilder.Entity<SystemErrorLog>()
            .HasIndex(x => x.CreatedAt);
        modelBuilder.Entity<SystemErrorLog>()
            .HasIndex(x => x.ErrorCode);
        modelBuilder.Entity<SystemErrorLog>()
            .HasIndex(x => x.StatusCode);
        modelBuilder.Entity<SystemErrorLog>()
            .HasIndex(x => x.UserId);
        modelBuilder.Entity<SystemErrorLog>()
            .HasIndex(x => x.RequestPath);
        modelBuilder.Entity<SystemErrorLog>()
            .Property(x => x.DetailsJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<ChatConversation>()
            .ToTable("ChatConversation");
        modelBuilder.Entity<ChatConversation>()
            .HasIndex(x => new { x.MaNguoiDung1, x.MaNguoiDung2 })
            .IsUnique()
            .HasFilter("\"LaNhom\" = FALSE");
        modelBuilder.Entity<ChatConversation>()
            .HasIndex(x => x.CapNhatLuc);

        modelBuilder.Entity<ChatParticipant>()
            .ToTable("ChatParticipant");
        modelBuilder.Entity<ChatParticipant>()
            .HasIndex(x => new { x.MaCuocTroChuyen, x.MaNguoiDung })
            .IsUnique();
        modelBuilder.Entity<ChatParticipant>()
            .HasOne(x => x.CuocTroChuyen)
            .WithMany(x => x.ThanhViens)
            .HasForeignKey(x => x.MaCuocTroChuyen)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMessageEntity>()
            .ToTable("ChatMessage");
        modelBuilder.Entity<ChatMessageEntity>()
            .HasIndex(x => new { x.MaCuocTroChuyen, x.ThoiGianGui });
        modelBuilder.Entity<ChatMessageEntity>()
            .HasIndex(x => new { x.MaNguoiNhan, x.ThoiGianDoc });
        modelBuilder.Entity<ChatMessageEntity>()
            .HasOne(x => x.CuocTroChuyen)
            .WithMany(x => x.TinNhans)
            .HasForeignKey(x => x.MaCuocTroChuyen)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatPoll>()
            .ToTable("ChatPoll");
        modelBuilder.Entity<ChatPoll>()
            .HasIndex(x => x.MaTinNhan)
            .IsUnique();
        modelBuilder.Entity<ChatPoll>()
            .HasOne(x => x.TinNhan)
            .WithMany()
            .HasForeignKey(x => x.MaTinNhan)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatPollOption>()
            .ToTable("ChatPollOption");
        modelBuilder.Entity<ChatPollOption>()
            .HasOne(x => x.ThamDo)
            .WithMany(x => x.LuaChons)
            .HasForeignKey(x => x.MaThamDo)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatPollVote>()
            .ToTable("ChatPollVote");
        modelBuilder.Entity<ChatPollVote>()
            .HasIndex(x => new { x.MaLuaChon, x.MaNguoiDung })
            .IsUnique();
        modelBuilder.Entity<ChatPollVote>()
            .HasOne(x => x.LuaChon)
            .WithMany(x => x.BinhChons)
            .HasForeignKey(x => x.MaLuaChon)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VanChuyen>()
            .HasIndex(x => x.MaDonHang)
            .IsUnique();

        modelBuilder.Entity<VanChuyen>()
            .HasIndex(x => x.MaVanDon)
            .IsUnique()
            .HasFilter("\"MaVanDon\" IS NOT NULL");

        modelBuilder.Entity<VanChuyen>()
            .Property(x => x.PhiVanChuyen)
            .HasPrecision(18, 2);

        modelBuilder.Entity<VanChuyen>()
            .Property(x => x.ConcurrencyVersion)
            .IsConcurrencyToken();

        modelBuilder.Entity<VanChuyen>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_VanChuyen_PhiVanChuyen_NonNegative",
                "\"PhiVanChuyen\" >= 0"));

        modelBuilder.Entity<VanChuyen>()
            .HasOne(x => x.DonHang)
            .WithOne(x => x.VanChuyen)
            .HasForeignKey<VanChuyen>(x => x.MaDonHang)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LichSuVanChuyen>()
            .HasOne(x => x.VanChuyen)
            .WithMany(x => x.LichSu)
            .HasForeignKey(x => x.MaVanChuyen)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChiTietDonHang>()
            .HasOne(c => c.DonHang)
            .WithMany(o => o.ChiTietDonHang)
            .HasForeignKey(c => c.MaDonHang);

        modelBuilder.Entity<ChiTietDonHang>()
            .HasOne(c => c.SanPham)
            .WithMany()
            .HasForeignKey(c => c.MaSanPham);

        modelBuilder.Entity<GiaoDichKho>()
            .HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.MaSanPham)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GiaoDichKho>()
            .HasOne(x => x.PhieuNhapKho)
            .WithMany()
            .HasForeignKey(x => x.MaPhieuNhap)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GiaoDichKho>()
            .HasOne(x => x.PhieuXuatKho)
            .WithMany()
            .HasForeignKey(x => x.MaPhieuXuat)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GiaoDichKho>()
            .HasOne(x => x.DonHang)
            .WithMany()
            .HasForeignKey(x => x.MaDonHang)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PhieuNhapKho>()
            .HasIndex(x => x.SoPhieu)
            .IsUnique();

        modelBuilder.Entity<ChiTietPhieuNhap>()
            .HasIndex(x => new { x.MaPhieuNhap, x.MaSanPham })
            .IsUnique();

        modelBuilder.Entity<ChiTietPhieuNhap>()
            .HasOne(x => x.PhieuNhapKho)
            .WithMany(x => x.ChiTietPhieuNhap)
            .HasForeignKey(x => x.MaPhieuNhap)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChiTietPhieuNhap>()
            .HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.MaSanPham)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PhieuXuatKho>()
            .HasIndex(x => x.SoPhieu)
            .IsUnique();

        modelBuilder.Entity<ChiTietPhieuXuat>()
            .HasIndex(x => new { x.MaPhieuXuat, x.MaSanPham })
            .IsUnique();

        modelBuilder.Entity<ChiTietPhieuXuat>()
            .HasOne(x => x.PhieuXuatKho)
            .WithMany(x => x.ChiTietPhieuXuat)
            .HasForeignKey(x => x.MaPhieuXuat)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChiTietPhieuXuat>()
            .HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.MaSanPham)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DanhMucSanPham>()
            .HasIndex(x => x.TenDanhMuc)
            .IsUnique();

        modelBuilder.Entity<SanPham>()
            .HasOne(x => x.DanhMuc)
            .WithMany(x => x.SanPhams)
            .HasForeignKey(x => x.MaDanhMuc)
            .OnDelete(DeleteBehavior.SetNull);

        // Giỏ hàng: cấu hình FK tường minh để tránh EF tạo shadow FK
        // (property MaGioHang/MaSanPham không khớp convention mặc định của EF).
        modelBuilder.Entity<ChiTietGioHang>()
            .HasOne(x => x.GioHang)
            .WithMany(x => x.ChiTietGioHang)
            .HasForeignKey(x => x.MaGioHang)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChiTietGioHang>()
            .HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.MaSanPham)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using SyncChain.API.Models;

namespace SyncChain.API.Data;
public class AppDbContext : DbContext
{
    // Các bảng tài khoản và phân quyền.
    public DbSet<NguoiDung> NguoiDung { get; set; }
    public DbSet<PhanQuyen> PhanQuyen { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // Các bảng sản phẩm, đơn hàng và giao dịch kho.
    public DbSet<SanPham> SanPham { get; set; }

    public DbSet<DonHang> DonHang { get; set; }
    public DbSet<ChiTietDonHang> ChiTietDonHang { get; set; }
    public DbSet<GiaoDichKho> GiaoDichKho { get; set; }

    // Cấu hình quan hệ giữa đơn hàng, sản phẩm và lịch sử kho.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            .HasForeignKey(x => x.MaSanPham);
    }
}

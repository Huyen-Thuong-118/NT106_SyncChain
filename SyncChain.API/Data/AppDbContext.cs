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

        // ─── Đồng bộ tên vật lý với schema PostgreSQL của Node (database/TaoBang.sql) ───
        // Node tạo bảng KHÔNG đặt nháy kép, nên Postgres tự hạ tên bảng/cột về chữ thường
        // (vd: DonHang -> donhang, TenSanPham -> tensanpham). Map toàn bộ về chữ thường
        // để EF đọc/ghi đúng các bảng mà backend Node tạo ra, dùng chung 1 DB.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()!.ToLowerInvariant());
            foreach (var property in entity.GetProperties())
                property.SetColumnName(property.Name.ToLowerInvariant());
        }

        // Các cột mà TaoBang.sql đặt tên khác với property C#.
        // Giữ nguyên tên property (để không đổi JSON key client đang dùng), chỉ đổi tên cột DB.
        modelBuilder.Entity<DonHang>().Property(x => x.MaNguoiDung).HasColumnName("makhachhang");
        modelBuilder.Entity<DonHang>().Property(x => x.TrangThai).HasColumnName("trangthaidon");
        modelBuilder.Entity<NguoiDung>().Property(x => x.IsActive).HasColumnName("kichhoat");
    }
}

using SyncChain.API.Data;
using SyncChain.API.DTOs.Address;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class AddressService
{
    private readonly AppDbContext _db;

    public AddressService(AppDbContext db) => _db = db;

    public List<object> GetAll(int userId) =>
        _db.DiaChi
            .Where(x => x.MaNguoiDung == userId)
            .OrderByDescending(x => x.LaMacDinh)
            .ThenByDescending(x => x.NgayTao)
            .Select(x => (object)new
            {
                x.MaDiaChi,
                x.TenNguoiNhan,
                x.SoDienThoai,
                x.TinhThanh,
                x.QuanHuyen,
                x.PhuongXa,
                x.DiaChiChiTiet,
                x.LaMacDinh,
                DiaChi = $"{x.DiaChiChiTiet}, {x.PhuongXa}, {x.QuanHuyen}, {x.TinhThanh}"
            })
            .ToList();

    public object Create(int userId, CreateAddressDTO dto)
    {
        Validate(dto.TenNguoiNhan, dto.SoDienThoai, dto.TinhThanh,
                 dto.QuanHuyen, dto.PhuongXa, dto.DiaChiChiTiet);

        if (dto.LaMacDinh)
            ClearDefault(userId);

        var address = new DiaChi
        {
            MaNguoiDung = userId,
            TenNguoiNhan = dto.TenNguoiNhan.Trim(),
            SoDienThoai = dto.SoDienThoai.Trim(),
            TinhThanh = dto.TinhThanh.Trim(),
            QuanHuyen = dto.QuanHuyen.Trim(),
            PhuongXa = dto.PhuongXa.Trim(),
            DiaChiChiTiet = dto.DiaChiChiTiet.Trim(),
            LaMacDinh = dto.LaMacDinh
        };

        _db.DiaChi.Add(address);
        _db.SaveChanges();
        return new { address.MaDiaChi, message = "Thêm địa chỉ thành công" };
    }

    public object Update(int userId, int id, UpdateAddressDTO dto)
    {
        var address = _db.DiaChi.FirstOrDefault(x => x.MaDiaChi == id && x.MaNguoiDung == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy địa chỉ");

        Validate(dto.TenNguoiNhan, dto.SoDienThoai, dto.TinhThanh,
                 dto.QuanHuyen, dto.PhuongXa, dto.DiaChiChiTiet);

        if (dto.LaMacDinh && !address.LaMacDinh)
            ClearDefault(userId);

        address.TenNguoiNhan = dto.TenNguoiNhan.Trim();
        address.SoDienThoai = dto.SoDienThoai.Trim();
        address.TinhThanh = dto.TinhThanh.Trim();
        address.QuanHuyen = dto.QuanHuyen.Trim();
        address.PhuongXa = dto.PhuongXa.Trim();
        address.DiaChiChiTiet = dto.DiaChiChiTiet.Trim();
        address.LaMacDinh = dto.LaMacDinh;

        _db.SaveChanges();
        return new { message = "Cập nhật địa chỉ thành công" };
    }

    public void Delete(int userId, int id)
    {
        var address = _db.DiaChi.FirstOrDefault(x => x.MaDiaChi == id && x.MaNguoiDung == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy địa chỉ");
        _db.DiaChi.Remove(address);
        _db.SaveChanges();
    }

    public object SetDefault(int userId, int id)
    {
        var address = _db.DiaChi.FirstOrDefault(x => x.MaDiaChi == id && x.MaNguoiDung == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy địa chỉ");
        ClearDefault(userId);
        address.LaMacDinh = true;
        _db.SaveChanges();
        return new { message = "Đã đặt làm địa chỉ mặc định" };
    }

    private void ClearDefault(int userId)
    {
        var defaults = _db.DiaChi.Where(x => x.MaNguoiDung == userId && x.LaMacDinh).ToList();
        defaults.ForEach(x => x.LaMacDinh = false);
    }

    private static void Validate(string ten, string phone, string tinh, string quan, string phuong, string chiTiet)
    {
        if (string.IsNullOrWhiteSpace(ten)) throw new Exception("Vui lòng nhập tên người nhận");
        if (string.IsNullOrWhiteSpace(phone)) throw new Exception("Vui lòng nhập số điện thoại");
        if (string.IsNullOrWhiteSpace(tinh)) throw new Exception("Vui lòng chọn tỉnh/thành phố");
        if (string.IsNullOrWhiteSpace(quan)) throw new Exception("Vui lòng chọn quận/huyện");
        if (string.IsNullOrWhiteSpace(phuong)) throw new Exception("Vui lòng chọn phường/xã");
        if (string.IsNullOrWhiteSpace(chiTiet)) throw new Exception("Vui lòng nhập địa chỉ chi tiết");
    }
}

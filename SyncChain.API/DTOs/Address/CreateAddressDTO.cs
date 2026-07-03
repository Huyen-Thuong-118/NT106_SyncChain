namespace SyncChain.API.DTOs.Address;

public class CreateAddressDTO
{
    public string TenNguoiNhan { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string TinhThanh { get; set; } = "";
    public string QuanHuyen { get; set; } = "";
    public string PhuongXa { get; set; } = "";
    public string DiaChiChiTiet { get; set; } = "";
    public bool LaMacDinh { get; set; } = false;
}

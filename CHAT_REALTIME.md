# Chat noi bo va goi realtime

Tinh nang chat noi bo chi danh cho tai khoan `staff`, `manager`, `admin`.
Hai may muon nhan tin/goi nhau phai cung ket noi toi mot backend `SyncChain.API` va mot PostgreSQL chung.
Neu moi may tu chay backend rieng bang `localhost` thi du lieu chat se tach nhau va khong thay tin nhan cua nhau.

## Chay backend

1. Cai PostgreSQL va tao database, vi du `syncchain`.
2. Sao chep `.env.example` thanh `.env`, sua `DATABASE_URL`.
3. Chay migration neu dung EF tooling:
   ```powershell
   dotnet ef database update --project SyncChain.API
   ```
   Khi backend khoi dong, app cung tao cac bang chat neu chua co bang.
4. Chay API:
   ```powershell
   dotnet run --project SyncChain.API
   ```

## Cau hinh desktop app tren may khac

Mac dinh desktop ket noi `http://localhost:5292/`.
De tro den backend tren may/server khac, dat bien moi truong truoc khi chay app:

```powershell
$env:SYNCCHAIN_API_URL = "http://IP_MAY_CHAY_API:5292/"
dotnet run --project app/SyncChain.Desktop/SyncChain.Desktop.csproj
```

Mo hai app desktop, dang nhap bang hai tai khoan noi bo khac nhau, vao Tin nhan, chon nhan vien va gui tin.
Tin nhan duoc luu vao PostgreSQL va day realtime qua SignalR hub `/hubs/chat`.

## Tinh nang chat da co

- Moi vao hop thu chi hien cac cuoc tro chuyen da co tin nhan hoac lich su goi.
- Tim tai khoan noi bo bang o tim kiem, bam ket qua de bat dau nhan tin/goi.
- Gui tin nhan text, icon nhanh va file dinh kem dang metadata duong dan local.
- Tao nhom bang ten nhom va danh sach ID nhan vien.
- Doi ten nhom.
- Ghim/bo ghim tin nhan bang cach bam vao bong tin nhan.
- Mo khung thong tin chat de xem ten, avatar chu cai, tim tin nhan, tin da ghim va file/icon da gui.
- Khung thong tin chi hien file va hinh anh, khong dua icon vao danh sach file/phuong tien.
- Khi bam vao o tim kiem, cot trai chuyen sang che do tim tai khoan; bam Back de quay ve hop thu.
- Gui hinh anh se hien preview anh truc tiep trong khung tro chuyen neu may hien tai truy cap duoc duong dan file.
- Hien trang thai da doc cho tin nhan gui di khi nguoi nhan da doc.
- Luu lich su cuoc goi vao chat nhu mot call card: goi, nho cuoc goi, bi tu choi, ban, ket thuc va thoi luong neu co.
- Tin nhan nhom co the tao tham do y kien voi cau hoi, nhieu lua chon, tuy chon mot/nhieu lua chon, thoi gian ket thuc, cho phep them lua chon va khoa binh chon.

## Goi am thanh

Nut `GOI` hien da co realtime call signaling:

- Goi den.
- Nhan cuoc goi.
- Tu choi.
- Ket thuc.
- Bao ban khi nguoi nhan dang co cuoc goi khac.
- Kenh `CallSignal` san sang de gan WebRTC SDP/ICE payload.

Phan truyen am thanh that can them WebRTC hoac audio transport rieng cho .NET MAUI Windows.
Trong lan implement nay, backend va desktop da co signaling realtime hoan chinh, nhung chua co media audio pipeline vi project chua co thu vien WebRTC/audio capture phu hop san trong workspace.

## Test nhanh Chat API

```powershell
.\scripts\test-chat.ps1 `
  -StaffEmail staff1@example.com -StaffPassword 123456 `
  -OtherStaffEmail staff2@example.com -OtherStaffPassword 123456 `
  -CustomerEmail customer@example.com -CustomerPassword 123456
```

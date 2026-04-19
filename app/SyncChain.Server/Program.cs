using Microsoft.Data.Sqlite;
using SyncChain.Core;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var databaseDirectory = Path.Combine(rootPath, "database");
Directory.CreateDirectory(databaseDirectory);

var dbPath = Path.Combine(databaseDirectory, "SyncChain.db");
var schemaPath = Path.Combine(databaseDirectory, "TaoBang.sql");
var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = dbPath,
    Mode = SqliteOpenMode.ReadWriteCreate,
    Pooling = true
}.ToString();

var repository = new SyncChainRepository(connectionString, schemaPath);
await repository.InitializeAsync();

var port = 5050;
var server = new SyncChainTcpServer(IPAddress.Any, port, repository);

Console.WriteLine($"SyncChain server started on tcp://127.0.0.1:{port}");
Console.WriteLine($"Database: {dbPath}");
Console.WriteLine("Seed accounts: admin/admin123, distributor/dist123, customer/cust123, factory/factory123");

await server.RunAsync(CancellationToken.None);

internal sealed class SyncChainTcpServer(IPAddress address, int port, SyncChainRepository repository)
{
    private readonly TcpListener _listener = new(address, port);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync(cancellationToken);
            _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var _ = client;
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

        try
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var request = JsonSerializer.Deserialize<RequestEnvelope>(line, ProtocolSerializer.JsonOptions)
                ?? throw new InvalidOperationException("Request envelope is invalid.");

            var response = await DispatchAsync(request);
            await writer.WriteLineAsync(response);
        }
        catch (Exception ex)
        {
            await writer.WriteLineAsync(ProtocolSerializer.SerializeResponse<object>(false, ex.Message, errorCode: "server_error"));
        }
    }

    private async Task<string> DispatchAsync(RequestEnvelope request)
    {
        return request.Command switch
        {
            NetworkCommands.Login => await HandleAsync<LoginRequest, LoginResponse>(request.Payload, repository.LoginAsync, "Đăng nhập thành công."),
            NetworkCommands.GetDashboard => await HandleAsync<AuthorizedRequest, DashboardSnapshot>(request.Payload, repository.GetDashboardAsync, "Đã tải dashboard."),
            NetworkCommands.GetProducts => await HandleAsync<AuthorizedRequest, ProductsResponse>(request.Payload, repository.GetProductsAsync, "Đã tải danh sách sản phẩm."),
            NetworkCommands.GetOrders => await HandleAsync<AuthorizedRequest, OrdersResponse>(request.Payload, repository.GetOrdersAsync, "Đã tải đơn hàng."),
            NetworkCommands.CreateOrder => await HandleAsync<CreateOrderRequest, CreateOrderResponse>(request.Payload, repository.CreateOrderAsync, "Tạo đơn hàng thành công."),
            NetworkCommands.UpdateOrderStatus => await HandleAsync<UpdateOrderStatusRequest, TrackingResponse>(request.Payload, repository.UpdateOrderStatusAsync, "Đã cập nhật trạng thái đơn hàng."),
            NetworkCommands.GetTracking => await HandleAsync<OrderTrackingRequest, TrackingResponse>(request.Payload, repository.GetTrackingAsync, "Đã tải lịch sử theo dõi."),
            NetworkCommands.AddProduct => await HandleAsync<AddProductRequest, ProductDto>(request.Payload, repository.AddProductAsync, "Đã thêm sản phẩm."),
            NetworkCommands.DeleteProduct => await HandleAsync<DeleteProductRequest, DeleteResult>(request.Payload, repository.DeleteProductAsync, "Đã xóa sản phẩm."),
            _ => ProtocolSerializer.SerializeResponse<object>(false, $"Unsupported command: {request.Command}", errorCode: "unsupported_command")
        };
    }

    private static async Task<string> HandleAsync<TRequest, TResponse>(
        JsonElement payload,
        Func<TRequest, Task<TResponse>> handler,
        string successMessage)
    {
        var request = ProtocolSerializer.DeserializePayload<TRequest>(payload);
        var response = await handler(request);
        return ProtocolSerializer.SerializeResponse(true, successMessage, response);
    }
}

internal sealed record OrderTrackingRequest(string SessionToken, long OrderId);

internal sealed record DeleteResult(int ProductId, bool Removed);

internal sealed class SyncChainRepository(string connectionString, string schemaPath)
{
    private readonly string _connectionString = connectionString;
    private readonly string _schemaPath = schemaPath;
    private readonly Dictionary<string, SessionContext> _sessions = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _inventoryLock = new(1, 1);

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var schemaSql = await File.ReadAllTextAsync(_schemaPath);
        var extraSchema = """
            CREATE TABLE IF NOT EXISTS HoatDongNguoiDung (
                MaHoatDong INTEGER PRIMARY KEY AUTOINCREMENT,
                MaNguoiDung INTEGER NOT NULL,
                HanhDong TEXT NOT NULL,
                ChiTiet TEXT NOT NULL,
                ThoiGian DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung)
            );

            CREATE TABLE IF NOT EXISTS TheoDoiDonHang (
                MaTheoDoi INTEGER PRIMARY KEY AUTOINCREMENT,
                MaDonHang INTEGER NOT NULL,
                TrangThai TEXT NOT NULL,
                GhiChu TEXT NOT NULL,
                CapNhatBoi TEXT NOT NULL,
                ThoiGian DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (MaDonHang) REFERENCES DonHang(MaDonHang)
            );

            CREATE TABLE IF NOT EXISTS ThanhToan (
                MaThanhToan INTEGER PRIMARY KEY AUTOINCREMENT,
                MaDonHang INTEGER NOT NULL,
                PhuongThuc TEXT NOT NULL,
                TrangThai TEXT NOT NULL,
                MaGiaoDich TEXT NOT NULL,
                ThoiGian DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (MaDonHang) REFERENCES DonHang(MaDonHang)
            );

            CREATE TABLE IF NOT EXISTS EmailOutbox (
                MaEmail INTEGER PRIMARY KEY AUTOINCREMENT,
                MaNguoiDung INTEGER NOT NULL,
                TieuDe TEXT NOT NULL,
                NoiDung TEXT NOT NULL,
                TrangThai TEXT NOT NULL DEFAULT 'Queued',
                ThoiGian DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung)
            );
            """;

        var command = connection.CreateCommand();
        command.CommandText = $"{schemaSql}\n{extraSchema}";
        await command.ExecuteNonQueryAsync();

        await SeedAsync(connection);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT nd.MaNguoiDung, nd.TenDangNhap, pq.TenVaiTro, nd.MatKhauHash
            FROM NguoiDung nd
            LEFT JOIN PhanQuyen pq ON pq.MaVaiTro = nd.MaVaiTro
            WHERE nd.TenDangNhap = $username
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$username", request.Username);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("Sai tài khoản hoặc mật khẩu.");
        }

        var passwordHash = reader.GetString(3);
        if (!string.Equals(passwordHash, HashPassword(request.Password), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Sai tài khoản hoặc mật khẩu.");
        }

        var session = new SessionContext(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? Roles.Customer : reader.GetString(2),
            Guid.NewGuid().ToString("N"));

        _sessions[session.Token] = session;
        await InsertActivityAsync(connection, session.UserId, "LOGIN", $"Người dùng {session.Username} đăng nhập.");

        return new LoginResponse(session, $"Xin chào {session.Username}. Vai trò hiện tại: {session.Role}.");
    }

    public async Task<DashboardSnapshot> GetDashboardAsync(AuthorizedRequest request)
    {
        var session = RequireSession(request.SessionToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var productCount = await ExecuteScalarIntAsync(connection, "SELECT COUNT(*) FROM SanPham;");
        var pendingOrders = await ExecuteScalarIntAsync(connection, "SELECT COUNT(*) FROM DonHang WHERE TrangThaiDon IN ('Da dat hang', 'Cho nha may xu ly');");
        var shippingOrders = await ExecuteScalarIntAsync(connection, "SELECT COUNT(*) FROM DonHang WHERE TrangThaiDon = 'Dang van chuyen';");
        var lowStockCount = await ExecuteScalarIntAsync(connection, "SELECT COUNT(*) FROM SanPham WHERE SoLuongTon <= MucTonThap;");
        var revenueToday = await ExecuteScalarDecimalAsync(connection, "SELECT COALESCE(SUM(TongTien), 0) FROM DonHang WHERE DATE(NgayTao) = DATE('now', 'localtime') AND TrangThaiDon IN ('Da thanh toan', 'Dang van chuyen', 'Da giao hang', 'Da hoan tat');");

        var alerts = new List<InventoryAlertDto>();
        var alertCommand = connection.CreateCommand();
        alertCommand.CommandText = """
            SELECT MaSanPham, TenSanPham, SoLuongTon, MucTonThap
            FROM SanPham
            WHERE SoLuongTon <= MucTonThap
            ORDER BY SoLuongTon ASC;
            """;

        await using (var alertReader = await alertCommand.ExecuteReaderAsync())
        {
            while (await alertReader.ReadAsync())
            {
                var qty = alertReader.GetInt32(2);
                var min = alertReader.GetInt32(3);
                alerts.Add(new InventoryAlertDto(
                    alertReader.GetInt32(0),
                    alertReader.GetString(1),
                    qty,
                    min,
                    qty == 0 ? "Critical" : qty < min ? "High" : "Medium"));
            }
        }

        var activities = new List<ActivityLogDto>();
        var activityCommand = connection.CreateCommand();
        activityCommand.CommandText = """
            SELECT nd.TenDangNhap, hd.HanhDong, hd.ChiTiet, hd.ThoiGian
            FROM HoatDongNguoiDung hd
            INNER JOIN NguoiDung nd ON nd.MaNguoiDung = hd.MaNguoiDung
            ORDER BY hd.MaHoatDong DESC
            LIMIT 8;
            """;

        await using (var activityReader = await activityCommand.ExecuteReaderAsync())
        {
            while (await activityReader.ReadAsync())
            {
                activities.Add(new ActivityLogDto(
                    activityReader.GetString(0),
                    activityReader.GetString(1),
                    activityReader.GetString(2),
                    activityReader.GetDateTime(3)));
            }
        }

        return new DashboardSnapshot(session.Username, session.Role, productCount, pendingOrders, shippingOrders, lowStockCount, revenueToday, alerts, activities);
    }

    public async Task<ProductsResponse> GetProductsAsync(AuthorizedRequest request)
    {
        RequireSession(request.SessionToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var products = new List<ProductDto>();
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT MaSanPham, TenSanPham, GiaBan, SoLuongTon, MucTonThap, TrangThai
            FROM SanPham
            ORDER BY MaSanPham DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            products.Add(new ProductDto(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetDecimal(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetString(5)));
        }

        return new ProductsResponse(products);
    }

    public async Task<OrdersResponse> GetOrdersAsync(AuthorizedRequest request)
    {
        var session = RequireSession(request.SessionToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var orders = new List<OrderDto>();
        var command = connection.CreateCommand();
        command.CommandText = session.Role == Roles.Customer
            ? """
                SELECT dh.MaDonHang, nd.TenDangNhap, ct.MaSanPham, sp.TenSanPham, ct.SoLuong, ct.DonGia, dh.TongTien,
                       COALESCE(tt.PhuongThuc, 'COD'), dh.TrangThaiDon, printf('SC-%06d', dh.MaDonHang), dh.NgayTao
                FROM DonHang dh
                INNER JOIN NguoiDung nd ON nd.MaNguoiDung = dh.MaKhachHang
                INNER JOIN ChiTietDonHang ct ON ct.MaDonHang = dh.MaDonHang
                INNER JOIN SanPham sp ON sp.MaSanPham = ct.MaSanPham
                LEFT JOIN ThanhToan tt ON tt.MaDonHang = dh.MaDonHang
                WHERE dh.MaKhachHang = $userId
                ORDER BY dh.MaDonHang DESC;
                """
            : """
                SELECT dh.MaDonHang, nd.TenDangNhap, ct.MaSanPham, sp.TenSanPham, ct.SoLuong, ct.DonGia, dh.TongTien,
                       COALESCE(tt.PhuongThuc, 'COD'), dh.TrangThaiDon, printf('SC-%06d', dh.MaDonHang), dh.NgayTao
                FROM DonHang dh
                INNER JOIN NguoiDung nd ON nd.MaNguoiDung = dh.MaKhachHang
                INNER JOIN ChiTietDonHang ct ON ct.MaDonHang = dh.MaDonHang
                INNER JOIN SanPham sp ON sp.MaSanPham = ct.MaSanPham
                LEFT JOIN ThanhToan tt ON tt.MaDonHang = dh.MaDonHang
                ORDER BY dh.MaDonHang DESC;
                """;
        if (session.Role == Roles.Customer)
        {
            command.Parameters.AddWithValue("$userId", session.UserId);
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            orders.Add(new OrderDto(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetDecimal(5),
                reader.GetDecimal(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetDateTime(10)));
        }

        return new OrdersResponse(orders);
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        var session = RequireSession(request.SessionToken);
        await _inventoryLock.WaitAsync();
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

            var productCommand = connection.CreateCommand();
            productCommand.Transaction = transaction;
            productCommand.CommandText = """
                SELECT TenSanPham, GiaBan, SoLuongTon
                FROM SanPham
                WHERE MaSanPham = $productId;
                """;
            productCommand.Parameters.AddWithValue("$productId", request.ProductId);

            string productName;
            decimal price;
            int stock;

            await using (var reader = await productCommand.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException("Sản phẩm không tồn tại.");
                }

                productName = reader.GetString(0);
                price = reader.GetDecimal(1);
                stock = reader.GetInt32(2);
            }

            if (stock < request.Quantity)
            {
                throw new InvalidOperationException("Tồn kho không đủ để xử lý đơn hàng.");
            }

            var updateInventory = connection.CreateCommand();
            updateInventory.Transaction = transaction;
            updateInventory.CommandText = """
                UPDATE SanPham
                SET SoLuongTon = SoLuongTon - $quantity
                WHERE MaSanPham = $productId AND SoLuongTon >= $quantity;
                """;
            updateInventory.Parameters.AddWithValue("$quantity", request.Quantity);
            updateInventory.Parameters.AddWithValue("$productId", request.ProductId);

            var changedRows = await updateInventory.ExecuteNonQueryAsync();
            if (changedRows == 0)
            {
                throw new InvalidOperationException("Có xung đột tồn kho. Vui lòng tải lại và thử lại.");
            }

            var totalPrice = price * request.Quantity;
            var initialStatus = request.PaymentMethod == PaymentMethods.Online ? OrderStatuses.Paid : OrderStatuses.Placed;

            var insertOrder = connection.CreateCommand();
            insertOrder.Transaction = transaction;
            insertOrder.CommandText = """
                INSERT INTO DonHang (MaKhachHang, TongTien, TrangThaiDon)
                VALUES ($customerId, $total, $status);
                SELECT last_insert_rowid();
                """;
            insertOrder.Parameters.AddWithValue("$customerId", session.UserId);
            insertOrder.Parameters.AddWithValue("$total", totalPrice);
            insertOrder.Parameters.AddWithValue("$status", initialStatus);
            var orderId = (long)(await insertOrder.ExecuteScalarAsync() ?? 0L);

            var insertDetail = connection.CreateCommand();
            insertDetail.Transaction = transaction;
            insertDetail.CommandText = """
                INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, SoLuong, DonGia)
                VALUES ($orderId, $productId, $quantity, $price);
                """;
            insertDetail.Parameters.AddWithValue("$orderId", orderId);
            insertDetail.Parameters.AddWithValue("$productId", request.ProductId);
            insertDetail.Parameters.AddWithValue("$quantity", request.Quantity);
            insertDetail.Parameters.AddWithValue("$price", price);
            await insertDetail.ExecuteNonQueryAsync();

            var transactionId = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId}";
            var insertPayment = connection.CreateCommand();
            insertPayment.Transaction = transaction;
            insertPayment.CommandText = """
                INSERT INTO ThanhToan (MaDonHang, PhuongThuc, TrangThai, MaGiaoDich)
                VALUES ($orderId, $method, $status, $transactionId);
                """;
            insertPayment.Parameters.AddWithValue("$orderId", orderId);
            insertPayment.Parameters.AddWithValue("$method", request.PaymentMethod);
            insertPayment.Parameters.AddWithValue("$status", request.PaymentMethod == PaymentMethods.Online ? "Confirmed" : "Pending COD");
            insertPayment.Parameters.AddWithValue("$transactionId", transactionId);
            await insertPayment.ExecuteNonQueryAsync();

            await InsertTrackingAsync(connection, transaction, orderId, initialStatus, $"Đơn hàng cho {productName} đã được tạo.", session.Username);
            await InsertActivityAsync(connection, transaction, session.UserId, "CREATE_ORDER", $"Tạo đơn #{orderId} - {productName} x{request.Quantity}.");
            await QueueEmailAsync(connection, transaction, session.UserId, "Xác nhận đơn hàng", $"Đơn hàng #{orderId} đã được tạo với trạng thái {initialStatus}.");

            await transaction.CommitAsync();

            return new CreateOrderResponse(orderId, $"SC-{orderId:D6}", initialStatus, $"Đã khóa tồn kho và tạo đơn cho {productName}.");
        }
        finally
        {
            _inventoryLock.Release();
        }
    }

    public async Task<TrackingResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest request)
    {
        var session = RequireSession(request.SessionToken);
        if (session.Role is not (Roles.Admin or Roles.Distributor or Roles.Factory))
        {
            throw new InvalidOperationException("Bạn không có quyền cập nhật trạng thái đơn hàng.");
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

        var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = transaction;
        updateCommand.CommandText = """
            UPDATE DonHang
            SET TrangThaiDon = $status
            WHERE MaDonHang = $orderId;
            """;
        updateCommand.Parameters.AddWithValue("$status", request.OrderStatus);
        updateCommand.Parameters.AddWithValue("$orderId", request.OrderId);

        if (await updateCommand.ExecuteNonQueryAsync() == 0)
        {
            throw new InvalidOperationException("Không tìm thấy đơn hàng.");
        }

        await InsertTrackingAsync(connection, transaction, request.OrderId, request.OrderStatus, request.Note, session.Username);
        await InsertActivityAsync(connection, transaction, session.UserId, "UPDATE_ORDER", $"Cập nhật đơn #{request.OrderId} -> {request.OrderStatus}.");
        await transaction.CommitAsync();

        return await GetTrackingAsync(new OrderTrackingRequest(request.SessionToken, request.OrderId));
    }

    public async Task<TrackingResponse> GetTrackingAsync(OrderTrackingRequest request)
    {
        RequireSession(request.SessionToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var events = new List<TrackingEventDto>();
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT MaDonHang, TrangThai, GhiChu, ThoiGian, CapNhatBoi
            FROM TheoDoiDonHang
            WHERE MaDonHang = $orderId
            ORDER BY MaTheoDoi DESC;
            """;
        command.Parameters.AddWithValue("$orderId", request.OrderId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            events.Add(new TrackingEventDto(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDateTime(3),
                reader.GetString(4)));
        }

        return new TrackingResponse(events);
    }

    public async Task<ProductDto> AddProductAsync(AddProductRequest request)
    {
        var session = RequireSession(request.SessionToken);
        if (session.Role is not (Roles.Admin or Roles.Distributor))
        {
            throw new InvalidOperationException("Chỉ Admin hoặc Nhà phân phối mới được thêm sản phẩm.");
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO SanPham (TenSanPham, GiaBan, SoLuongTon, MucTonThap, TrangThai)
            VALUES ($name, $price, $quantity, $minimumStock, 'Hoat dong');
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$name", request.ProductName);
        command.Parameters.AddWithValue("$price", request.Price);
        command.Parameters.AddWithValue("$quantity", request.Quantity);
        command.Parameters.AddWithValue("$minimumStock", request.MinimumStock);

        var productId = Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
        await InsertActivityAsync(connection, transaction, session.UserId, "ADD_PRODUCT", $"Thêm sản phẩm {request.ProductName}.");
        await transaction.CommitAsync();

        return new ProductDto(productId, request.ProductName, request.Price, request.Quantity, request.MinimumStock, "Hoat dong");
    }

    public async Task<DeleteResult> DeleteProductAsync(DeleteProductRequest request)
    {
        var session = RequireSession(request.SessionToken);
        if (session.Role is not (Roles.Admin or Roles.Distributor))
        {
            throw new InvalidOperationException("Chỉ Admin hoặc Nhà phân phối mới được xóa sản phẩm.");
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM SanPham WHERE MaSanPham = $productId;";
        command.Parameters.AddWithValue("$productId", request.ProductId);
        var rows = await command.ExecuteNonQueryAsync();

        await InsertActivityAsync(connection, transaction, session.UserId, "DELETE_PRODUCT", $"Xóa sản phẩm #{request.ProductId}.");
        await transaction.CommitAsync();

        return new DeleteResult(request.ProductId, rows > 0);
    }

    private SessionContext RequireSession(string token)
    {
        if (_sessions.TryGetValue(token, out var session))
        {
            return session;
        }

        throw new InvalidOperationException("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
    }

    private async Task SeedAsync(SqliteConnection connection)
    {
        foreach (var role in new[] { Roles.Admin, Roles.Distributor, Roles.Customer, Roles.Factory })
        {
            var roleCommand = connection.CreateCommand();
            roleCommand.CommandText = "INSERT OR IGNORE INTO PhanQuyen (TenVaiTro) VALUES ($role);";
            roleCommand.Parameters.AddWithValue("$role", role);
            await roleCommand.ExecuteNonQueryAsync();
        }

        var users = new[]
        {
            ("admin", "admin123", "admin@syncchain.local", Roles.Admin),
            ("distributor", "dist123", "distributor@syncchain.local", Roles.Distributor),
            ("customer", "cust123", "customer@syncchain.local", Roles.Customer),
            ("factory", "factory123", "factory@syncchain.local", Roles.Factory)
        };

        foreach (var user in users)
        {
            var userCommand = connection.CreateCommand();
            userCommand.CommandText = """
                INSERT OR IGNORE INTO NguoiDung (TenDangNhap, MatKhauHash, Email, MaVaiTro)
                VALUES (
                    $username,
                    $passwordHash,
                    $email,
                    (SELECT MaVaiTro FROM PhanQuyen WHERE TenVaiTro = $role LIMIT 1)
                );
                """;
            userCommand.Parameters.AddWithValue("$username", user.Item1);
            userCommand.Parameters.AddWithValue("$passwordHash", HashPassword(user.Item2));
            userCommand.Parameters.AddWithValue("$email", user.Item3);
            userCommand.Parameters.AddWithValue("$role", user.Item4);
            await userCommand.ExecuteNonQueryAsync();
        }

        if (await ExecuteScalarIntAsync(connection, "SELECT COUNT(*) FROM SanPham;") == 0)
        {
            var products = new[]
            {
                ("Ban phim co Logitech", 500000m, 12, 4),
                ("Tai nghe kho van", 350000m, 25, 10),
                ("Camera kiem kho mini", 1800000m, 6, 3),
                ("May quet ma vach", 950000m, 9, 4)
            };

            foreach (var product in products)
            {
                var productCommand = connection.CreateCommand();
                productCommand.CommandText = """
                    INSERT INTO SanPham (TenSanPham, GiaBan, SoLuongTon, MucTonThap, TrangThai)
                    VALUES ($name, $price, $quantity, $minimumStock, 'Hoat dong');
                    """;
                productCommand.Parameters.AddWithValue("$name", product.Item1);
                productCommand.Parameters.AddWithValue("$price", product.Item2);
                productCommand.Parameters.AddWithValue("$quantity", product.Item3);
                productCommand.Parameters.AddWithValue("$minimumStock", product.Item4);
                await productCommand.ExecuteNonQueryAsync();
            }
        }
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static async Task InsertTrackingAsync(SqliteConnection connection, SqliteTransaction transaction, long orderId, string status, string note, string updatedBy)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO TheoDoiDonHang (MaDonHang, TrangThai, GhiChu, CapNhatBoi)
            VALUES ($orderId, $status, $note, $updatedBy);
            """;
        command.Parameters.AddWithValue("$orderId", orderId);
        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$note", note);
        command.Parameters.AddWithValue("$updatedBy", updatedBy);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertActivityAsync(SqliteConnection connection, int userId, string action, string detail)
    {
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO HoatDongNguoiDung (MaNguoiDung, HanhDong, ChiTiet)
            VALUES ($userId, $action, $detail);
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$action", action);
        command.Parameters.AddWithValue("$detail", detail);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertActivityAsync(SqliteConnection connection, SqliteTransaction transaction, int userId, string action, string detail)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO HoatDongNguoiDung (MaNguoiDung, HanhDong, ChiTiet)
            VALUES ($userId, $action, $detail);
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$action", action);
        command.Parameters.AddWithValue("$detail", detail);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task QueueEmailAsync(SqliteConnection connection, SqliteTransaction transaction, int userId, string subject, string body)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO EmailOutbox (MaNguoiDung, TieuDe, NoiDung, TrangThai)
            VALUES ($userId, $subject, $body, 'Queued');
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$subject", subject);
        command.Parameters.AddWithValue("$body", body);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> ExecuteScalarIntAsync(SqliteConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
    }

    private static async Task<decimal> ExecuteScalarDecimalAsync(SqliteConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToDecimal(await command.ExecuteScalarAsync() ?? 0m);
    }
}

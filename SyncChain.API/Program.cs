using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.Services;
using SyncChain.API.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using SyncChain.API.Models;
using System.Security.Claims;


// Schema dùng 'timestamp without time zone' và toàn bộ code ghi thời gian bằng DateTime.Now
// (Kind=Local). Npgsql 6+ mặc định từ chối ghi DateTime Local → lỗi "only UTC is supported".
// Bật legacy timestamp behavior để ghi/đọc DateTime local hoạt động bình thường.
// Phải đặt trước khi khởi tạo bất kỳ NpgsqlDataSource nào.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Đăng ký service xử lý nghiệp vụ xác thực.
builder.Services.AddScoped<AuthService>();

// Cấu hình PostgreSQL làm database chính (dùng chung 1 DB với backend Node).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Cấu hình Swagger và nút nhập Bearer token.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "SyncChain API", 
        Version = "v1" 
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});


var jwtSettings = builder.Configuration.GetSection("Jwt");

// Cấu hình xác thực JWT cho toàn bộ API.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})


.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
        )
    };
    // Cho phép SignalR gửi token qua query string ?access_token=
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) &&
                ctx.Request.Path.StartsWithSegments("/hubs"))
            {
                ctx.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // Các policy phân quyền theo vai trò người dùng.
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("StaffOnly", policy =>
        policy.RequireRole("staff", "manager", "admin"));

    options.AddPolicy("ProductWrite", policy =>
        policy.RequireRole("manager", "admin"));

    options.AddPolicy("OrderWrite", policy =>
        policy.RequireRole("customer", "staff", "manager", "admin"));

    options.AddPolicy("OrderManage", policy =>
        policy.RequireRole("staff", "manager", "admin"));
});

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<VnPayService>();
builder.Services.AddHttpClient<MoMoService>();

builder.Services.AddSignalR();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("SignalRCors", policy =>
        policy.WithOrigins("http://localhost:5292", "http://localhost:5000", "http://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Rate limiting: login và forgot-password
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("otp", o =>
    {
        o.PermitLimit = 3;
        o.Window = TimeSpan.FromMinutes(5);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

var app = builder.Build();


// Khởi tạo database cục bộ và bổ sung schema còn thiếu khi chạy bản cũ.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // EnsureCreated() tự tạo toàn bộ schema (kể cả GiaoDichKho và các cột
    // mới của SanPham) theo đúng cú pháp PostgreSQL từ model EF.
    db.Database.EnsureCreated();

    // Seed lại các role mặc định để phân quyền luôn đúng.
    var roles = new[]
    {
        new PhanQuyen { MaVaiTro = 1, TenVaiTro = "customer" },
        new PhanQuyen { MaVaiTro = 2, TenVaiTro = "staff" },
        new PhanQuyen { MaVaiTro = 3, TenVaiTro = "manager" },
        new PhanQuyen { MaVaiTro = 4, TenVaiTro = "admin" }
    };

    foreach (var role in roles)
    {
        var existingRole = db.PhanQuyen.FirstOrDefault(x => x.MaVaiTro == role.MaVaiTro);
        if (existingRole == null)
        {
            db.PhanQuyen.Add(role);
        }
        else if (existingRole.TenVaiTro != role.TenVaiTro)
        {
            existingRole.TenVaiTro = role.TenVaiTro;
        }
    }

    // Tạo hoặc sửa tài khoản admin mặc định.
    var admin = db.NguoiDung.FirstOrDefault(x => x.Email == "admin@gmail.com");

    if (admin == null)
    {
        db.NguoiDung.Add(new NguoiDung
        {
            Email = "admin@gmail.com",
            TenDangNhap = "admin",
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            MaVaiTro = 4
        });
    }
    else
    {
        admin.MaVaiTro = 4; 
    }

    db.SaveChanges();

    // Tạo bảng mới và thêm cột cho schema đã tồn tại (Neon cloud DB)
    try
    {
        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS thanhtoan (
    mathanhtoan SERIAL PRIMARY KEY,
    madonhang INTEGER NOT NULL REFERENCES donhang(madonhang),
    phuongthuc TEXT NOT NULL,
    trangthaithanhtoan TEXT NOT NULL DEFAULT 'Pending',
    sotien NUMERIC(15,2) NOT NULL,
    ngaytao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ngaycapnhat TIMESTAMP,
    magiaodich TEXT,
    dulieucallback TEXT
);");
        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS thongbao (
    mathongbao SERIAL PRIMARY KEY,
    manguoidung INTEGER NOT NULL REFERENCES nguoidung(manguoidung),
    loaithongbao TEXT NOT NULL,
    tieude TEXT NOT NULL,
    noidung TEXT NOT NULL,
    dadoc BOOLEAN NOT NULL DEFAULT FALSE,
    ngaytao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    madonhang INTEGER REFERENCES donhang(madonhang)
);");
        db.Database.ExecuteSqlRaw(
            "ALTER TABLE donhang ADD COLUMN IF NOT EXISTS phuongthucthanhtoan TEXT DEFAULT 'COD';");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Schema patch warning: {msg}", ex.Message);
    }
}



app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();

app.UseCors("SignalRCors");

// Bật xác thực và phân quyền trước khi map controller.
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<OrderHub>("/hubs/order");
app.MapControllers();

app.Run();

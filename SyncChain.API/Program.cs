using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SyncChain.API.Configuration;
using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.ExceptionHandling;
using SyncChain.API.Hubs;
using SyncChain.API.Models;
using SyncChain.API.Services;
using System.Text;

EnvFileLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            var systemErrorLog = context.HttpContext.RequestServices.GetService<ISystemErrorLogService>();
            if (systemErrorLog != null)
            {
                systemErrorLog.LogAsync(new SystemErrorLogEntry(
                    "VALIDATION_ERROR",
                    "Du lieu request khong hop le.",
                    StatusCodes.Status400BadRequest,
                    details)).GetAwaiter().GetResult();
            }

            return new BadRequestObjectResult(new ApiErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "Du lieu request khong hop le.",
                Details = details,
                TraceId = context.HttpContext.TraceIdentifier
            });
        };
    });
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditContextAccessor, AuditContextAccessor>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<ISystemErrorLogService, SystemErrorLogService>();
builder.Services.AddScoped<AuthService>();

var databaseUrl = builder.Configuration["DATABASE_URL"];
if (string.IsNullOrWhiteSpace(databaseUrl))
{
    throw new InvalidOperationException(
        "Chua cau hinh DATABASE_URL. Hay sao chep .env.example thanh .env va cap nhat thong tin PostgreSQL.");
}

var connectionString = EnvFileLoader.ToPostgreSqlConnectionString(databaseUrl);
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
});

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
        Description = "Nhap: Bearer {token}"
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
            Array.Empty<string>()
        }
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt");

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
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/chat"))
                context.Token = accessToken;

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("InternalOnly", policy =>
        policy.RequireRole("staff", "manager", "admin"));

    options.AddPolicy("StaffOrAbove", policy =>
        policy.RequireRole("staff", "manager", "admin"));

    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("manager", "admin"));

    options.AddPolicy("ProductRead", policy =>
        policy.RequireRole("customer", "staff", "manager", "admin"));

    options.AddPolicy("ProductWrite", policy =>
        policy.RequireRole("manager", "admin"));

    options.AddPolicy("InventoryRead", policy =>
        policy.RequireRole("manager", "admin"));

    options.AddPolicy("InventoryWrite", policy =>
        policy.RequireRole("manager", "admin"));

    options.AddPolicy("InventoryApprove", policy =>
        policy.RequireRole("manager", "admin"));

    options.AddPolicy("OrderWrite", policy =>
        policy.RequireRole("customer", "staff", "manager", "admin"));

    options.AddPolicy("OrderManage", policy =>
        policy.RequireRole("staff", "manager", "admin"));

    options.AddPolicy("AuditRead", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("SystemErrorLogRead", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("ReportView", policy =>
        policy.RequireRole("manager", "admin"));

    options.AddPolicy("RevenueView", policy =>
        policy.RequireRole("manager", "admin"));
});

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<WarehouseReceiptService>();
builder.Services.AddScoped<WarehouseIssueService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ShippingService>();
builder.Services.AddScoped<DeliveryEstimateService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddHostedService<ShippingAutoCompletionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("""
        ALTER TABLE "DonHang" ADD COLUMN IF NOT EXISTS "TenNguoiNhan" character varying(150) NOT NULL DEFAULT '';
        ALTER TABLE "DonHang" ADD COLUMN IF NOT EXISTS "SoDienThoai" character varying(30) NOT NULL DEFAULT '';
        ALTER TABLE "DonHang" ADD COLUMN IF NOT EXISTS "EmailNguoiNhan" character varying(150) NOT NULL DEFAULT '';
        ALTER TABLE "DonHang" ADD COLUMN IF NOT EXISTS "DiaChiGiaoHang" character varying(500) NOT NULL DEFAULT '';
        ALTER TABLE "DonHang" ADD COLUMN IF NOT EXISTS "TinhThanh" character varying(100) NOT NULL DEFAULT '';
        ALTER TABLE "DonHang" ADD COLUMN IF NOT EXISTS "PhuongXa" character varying(100) NOT NULL DEFAULT '';
        ALTER TABLE "DonHang" ADD COLUMN IF NOT EXISTS "LoaiDichVu" character varying(30) NOT NULL DEFAULT 'Tieu chuan';
        ALTER TABLE "DonHang" ADD COLUMN IF NOT EXISTS "TrongLuongKg" numeric NOT NULL DEFAULT 1;
        ALTER TABLE "DonHang" ADD COLUMN IF NOT EXISTS "GhiChu" character varying(500) NOT NULL DEFAULT '';

        CREATE TABLE IF NOT EXISTS "ChatConversation" (
            "MaCuocTroChuyen" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "MaNguoiDung1" integer NOT NULL,
            "MaNguoiDung2" integer NOT NULL,
            "LaNhom" boolean NOT NULL DEFAULT FALSE,
            "TenNhom" character varying(150) NOT NULL DEFAULT '',
            "AnhDaiDien" character varying(500) NOT NULL DEFAULT '',
            "MaNguoiTao" integer NULL,
            "NgayTao" timestamp with time zone NOT NULL,
            "CapNhatLuc" timestamp with time zone NOT NULL
        );
        ALTER TABLE "ChatConversation" ADD COLUMN IF NOT EXISTS "LaNhom" boolean NOT NULL DEFAULT FALSE;
        ALTER TABLE "ChatConversation" ADD COLUMN IF NOT EXISTS "TenNhom" character varying(150) NOT NULL DEFAULT '';
        ALTER TABLE "ChatConversation" ADD COLUMN IF NOT EXISTS "AnhDaiDien" character varying(500) NOT NULL DEFAULT '';
        ALTER TABLE "ChatConversation" ADD COLUMN IF NOT EXISTS "MaNguoiTao" integer NULL;
        DROP INDEX IF EXISTS "IX_ChatConversation_MaNguoiDung1_MaNguoiDung2";
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ChatConversation_MaNguoiDung1_MaNguoiDung2"
            ON "ChatConversation" ("MaNguoiDung1", "MaNguoiDung2")
            WHERE "LaNhom" = FALSE;
        CREATE INDEX IF NOT EXISTS "IX_ChatConversation_CapNhatLuc"
            ON "ChatConversation" ("CapNhatLuc");

        CREATE TABLE IF NOT EXISTS "ChatParticipant" (
            "MaThanhVien" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "MaCuocTroChuyen" integer NOT NULL REFERENCES "ChatConversation" ("MaCuocTroChuyen") ON DELETE CASCADE,
            "MaNguoiDung" integer NOT NULL,
            "ThamGiaLuc" timestamp with time zone NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ChatParticipant_MaCuocTroChuyen_MaNguoiDung"
            ON "ChatParticipant" ("MaCuocTroChuyen", "MaNguoiDung");

        CREATE TABLE IF NOT EXISTS "ChatMessage" (
            "MaTinNhan" bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "MaCuocTroChuyen" integer NOT NULL REFERENCES "ChatConversation" ("MaCuocTroChuyen") ON DELETE CASCADE,
            "MaNguoiGui" integer NOT NULL,
            "MaNguoiNhan" integer NOT NULL,
            "NoiDung" character varying(2000) NOT NULL,
            "LoaiTinNhan" character varying(30) NOT NULL DEFAULT 'text',
            "TenFile" character varying(255) NOT NULL DEFAULT '',
            "DuongDanFile" character varying(500) NOT NULL DEFAULT '',
            "TrangThaiCuocGoi" character varying(50) NOT NULL DEFAULT '',
            "ThoiLuongCuocGoiGiay" integer NULL,
            "DaGhim" boolean NOT NULL DEFAULT FALSE,
            "DaThuHoi" boolean NOT NULL DEFAULT FALSE,
            "CamXuc" character varying(20) NOT NULL DEFAULT '',
            "ThoiGianGui" timestamp with time zone NOT NULL,
            "ThoiGianDoc" timestamp with time zone NULL
        );
        ALTER TABLE "ChatMessage" ADD COLUMN IF NOT EXISTS "LoaiTinNhan" character varying(30) NOT NULL DEFAULT 'text';
        ALTER TABLE "ChatMessage" ADD COLUMN IF NOT EXISTS "TenFile" character varying(255) NOT NULL DEFAULT '';
        ALTER TABLE "ChatMessage" ADD COLUMN IF NOT EXISTS "DuongDanFile" character varying(500) NOT NULL DEFAULT '';
        ALTER TABLE "ChatMessage" ADD COLUMN IF NOT EXISTS "TrangThaiCuocGoi" character varying(50) NOT NULL DEFAULT '';
        ALTER TABLE "ChatMessage" ADD COLUMN IF NOT EXISTS "ThoiLuongCuocGoiGiay" integer NULL;
        ALTER TABLE "ChatMessage" ADD COLUMN IF NOT EXISTS "DaGhim" boolean NOT NULL DEFAULT FALSE;
        ALTER TABLE "ChatMessage" ADD COLUMN IF NOT EXISTS "DaThuHoi" boolean NOT NULL DEFAULT FALSE;
        ALTER TABLE "ChatMessage" ADD COLUMN IF NOT EXISTS "CamXuc" character varying(20) NOT NULL DEFAULT '';
        CREATE INDEX IF NOT EXISTS "IX_ChatMessage_MaCuocTroChuyen_ThoiGianGui"
            ON "ChatMessage" ("MaCuocTroChuyen", "ThoiGianGui");
        CREATE INDEX IF NOT EXISTS "IX_ChatMessage_MaNguoiNhan_ThoiGianDoc"
            ON "ChatMessage" ("MaNguoiNhan", "ThoiGianDoc");

        CREATE TABLE IF NOT EXISTS "ChatPoll" (
            "MaThamDo" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "MaTinNhan" bigint NOT NULL REFERENCES "ChatMessage" ("MaTinNhan") ON DELETE CASCADE,
            "CauHoi" character varying(300) NOT NULL,
            "ChoPhepNhieuLuaChon" boolean NOT NULL DEFAULT FALSE,
            "ChoPhepThemLuaChon" boolean NOT NULL DEFAULT FALSE,
            "AnKetQuaKhiChuaBinhChon" boolean NOT NULL DEFAULT FALSE,
            "AnNguoiBinhChon" boolean NOT NULL DEFAULT FALSE,
            "DaKhoa" boolean NOT NULL DEFAULT FALSE,
            "KetThucLuc" timestamp with time zone NULL
        );
        ALTER TABLE "ChatPoll" ADD COLUMN IF NOT EXISTS "AnKetQuaKhiChuaBinhChon" boolean NOT NULL DEFAULT FALSE;
        ALTER TABLE "ChatPoll" ADD COLUMN IF NOT EXISTS "AnNguoiBinhChon" boolean NOT NULL DEFAULT FALSE;
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ChatPoll_MaTinNhan"
            ON "ChatPoll" ("MaTinNhan");

        CREATE TABLE IF NOT EXISTS "ChatPollOption" (
            "MaLuaChon" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "MaThamDo" integer NOT NULL REFERENCES "ChatPoll" ("MaThamDo") ON DELETE CASCADE,
            "NoiDung" character varying(200) NOT NULL
        );

        CREATE TABLE IF NOT EXISTS "ChatPollVote" (
            "MaBinhChon" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "MaLuaChon" integer NOT NULL REFERENCES "ChatPollOption" ("MaLuaChon") ON DELETE CASCADE,
            "MaNguoiDung" integer NOT NULL,
            "BinhChonLuc" timestamp with time zone NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ChatPollVote_MaLuaChon_MaNguoiDung"
            ON "ChatPollVote" ("MaLuaChon", "MaNguoiDung");
        """);

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
            db.PhanQuyen.Add(role);
        else
            existingRole.TenVaiTro = role.TenVaiTro;
    }

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
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (feature?.Error == null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "Da xay ra loi he thong.",
                Details = new Dictionary<string, object?>(),
                TraceId = context.TraceIdentifier
            });
            return;
        }

        var handler = context.RequestServices.GetRequiredService<ApiExceptionHandler>();
        await handler.TryHandleAsync(context, feature.Error, context.RequestAborted);
    });
});
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    if (response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
    {
        var forbidden = response.StatusCode == StatusCodes.Status403Forbidden;
        var code = forbidden ? "FORBIDDEN" : "UNAUTHORIZED";
        var message = forbidden ? "Ban khong co quyen thuc hien thao tac nay." : "Yeu cau chua duoc xac thuc.";
        var systemErrorLog = statusContext.HttpContext.RequestServices.GetService<ISystemErrorLogService>();
        if (systemErrorLog != null)
        {
            await systemErrorLog.LogAsync(new SystemErrorLogEntry(
                code,
                message,
                response.StatusCode,
                new Dictionary<string, object?>
                {
                    ["path"] = statusContext.HttpContext.Request.Path.Value,
                    ["method"] = statusContext.HttpContext.Request.Method
                }));
        }

        await response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Code = code,
            Message = message,
            Details = new Dictionary<string, object?>(),
            TraceId = statusContext.HttpContext.TraceIdentifier
        });
    }
});
app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

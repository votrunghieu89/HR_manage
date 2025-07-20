// File: Program.cs
// Mục đích: Cấu hình đầy đủ các dịch vụ và pipeline cho ứng dụng,
// bao gồm DALs, Identity, JWT Auth (với Issuer/Audience validation),
// Swagger JWT support, Middleware, và Role Seeding.

using Integration_System.DAL;
using Integration_System.Middleware;
using Integration_System.Model; // Đảm bảo Model namespace được thêm nếu cần
using Integration_System.Services;
using Integration_System.Settings;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using Integration_System.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Integration_System.Constants;
using Microsoft.OpenApi.Models; // <<< Thêm using cho Swagger Models

var builder = WebApplication.CreateBuilder(args);

// --- Đăng ký Services ---

// 1. Controller và Swagger/OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Cấu hình SwaggerGen để hỗ trợ JWT Authorization UI
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Integration System API", Version = "v1" });
    // Định nghĩa Security Scheme cho Bearer Token (JWT)
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid JWT token prefixed with 'Bearer ' (e.g., 'Bearer eyJhbGciOi...')",
        Name = "Authorization",
        Type = SecuritySchemeType.Http, // Sử dụng HTTP authentication
        BearerFormat = "JWT",           // Định dạng là JWT
        Scheme = "Bearer"               // Scheme là Bearer
    });
    // Yêu cầu Security Scheme này cho các endpoint cần bảo vệ
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer" // Phải khớp với tên trong AddSecurityDefinition
                }
            },
            new string[]{} // Không cần scopes cụ thể
        }
    });
});


// 2. Data Access Layer (DAL)
builder.Services.AddScoped<EmployeeDAL>();
builder.Services.AddScoped<SalaryDAL>();
builder.Services.AddScoped<PositionDAL>();
builder.Services.AddScoped<DepartmentDAL>();
builder.Services.AddScoped<AttendanceDAL>();
builder.Services.AddScoped<AuthDAL>(); // DAL cho Authentication
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://*:{port}");
}
// 3. CORS (Cross-Origin Resource Sharing)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Cho phép frontend dev server
                  .AllowAnyHeader()
                  .AllowAnyMethod();
            // Trong production, nên chỉ định rõ các Origin được phép thay vì "*"
        });
});

// 4. Redis và Notification Service
try
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var configuration = ConfigurationOptions.Parse("localhost:6379", true);
        configuration.AbortOnConnectFail = false; // Cho phép thử lại kết nối nếu thất bại ban đầu
        return ConnectionMultiplexer.Connect(configuration);
    });
    builder.Services.AddSingleton<NotificationSalaryService>(); // Service tương tác Redis
}
catch (RedisConnectionException ex)
{
    // Ghi log lỗi nghiêm trọng khi không kết nối được Redis
    Console.WriteLine($"FATAL: Could not connect to Redis. Notifications may not work. Error: {ex.Message}");
    // Cân nhắc có nên dừng ứng dụng hay không tùy thuộc vào tầm quan trọng của Redis
}

// 5. Middleware tùy chỉnh
builder.Services.AddScoped<NotificationSalaryMDW>(); // Middleware kiểm tra lương/ngày kỷ niệm

// 6. Google Auth và Email Service (Dùng để gửi mail)
builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddTransient<IGmailService, GmailService>();

// 7. DbContext cho Authentication (IntegrationAuthDB)

builder.Services.AddScoped<IAuthService, AuthService>();
//var authConnectionString = builder.Configuration.GetConnectionString("AuthDbConnection");
//if (string.IsNullOrEmpty(authConnectionString))
//{
//    throw new InvalidOperationException("Connection string 'AuthDbConnection' not found in appsettings.json.");
//}
//builder.Services.AddDbContext<AuthDbContext>(options =>
//    options.UseSqlServer(authConnectionString));


// 9. Cấu hình JWT Authentication
builder.Services.AddAuthentication(options =>
{
    // Đặt scheme mặc định là Bearer (JWT)
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => // Cấu hình cụ thể cho Bearer scheme
{
    options.SaveToken = true; // Lưu token vào HttpContext sau khi xác thực thành công
    options.RequireHttpsMetadata = false; // Đặt thành TRUE trong môi trường Production
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        // === Cấu hình Validation ===
        ValidateIssuer = true, // <<< BẬT KIỂM TRA ISSUER (Người phát hành)
        ValidateAudience = true, // <<< BẬT KIỂM TRA AUDIENCE (Đối tượng nhận)
        ValidateLifetime = true, // Kiểm tra token còn hạn hay không (Rất quan trọng!)
        ValidateIssuerSigningKey = true, // Kiểm tra chữ ký của token (Bắt buộc!)

        // === Giá trị hợp lệ (Đọc từ appsettings.json) ===
        ValidIssuer = builder.Configuration["Jwt:Issuer"], // Phải khớp giá trị Issuer trong appsettings
        ValidAudience = builder.Configuration["Jwt:Audience"], // Phải khớp giá trị Audience trong appsettings
        // Key bí mật dùng để ký và xác thực token (Phải khớp 100% với appsettings)
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured in appsettings.json.")))
    };
});

// --- Xây dựng WebApplication ---
var app = builder.Build();

// --- Cấu hình HTTP Request Pipeline (Thứ tự Middleware rất quan trọng!) ---

// 1. Môi trường Development: Bật Swagger và trang lỗi chi tiết
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Tạo file JSON mô tả API
    app.UseSwaggerUI(); // Tạo trang UI Swagger từ file JSON
    // app.UseDeveloperExceptionPage(); // Có thể dùng nếu cần trang lỗi dev mặc định
}
else // Môi trường Production
{
    // Thêm middleware xử lý lỗi tập trung (ví dụ: trang lỗi tùy chỉnh)
    // app.UseExceptionHandler("/Error");
    // Bắt buộc dùng HTTPS trong production
    app.UseHsts();
}

// 2. Chuyển hướng HTTP sang HTTPS
app.UseHttpsRedirection();

// 3. Sử dụng CORS (Phải đặt trước UseAuthentication/UseAuthorization nếu chúng cần thông tin CORS)
app.UseCors("AllowSpecificOrigin");

// 4. Sử dụng Authentication (Xác định người dùng là ai dựa vào token)
// Phải đặt TRƯỚC UseAuthorization
app.UseAuthentication();

// 5. Sử dụng Authorization (Kiểm tra xem người dùng đã xác thực có quyền làm gì)
// Phải đặt SAU UseAuthentication
app.UseAuthorization();

// 6. Ánh xạ các request tới Controllers
app.MapControllers();

// 7. Seed Roles vào Database (Thực hiện một lần khi khởi động)
// Đặt sau các cấu hình pipeline cần thiết nhưng trước app.Run()
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roleNames = {
            UserRoles.Admin,
            UserRoles.Hr,
            UserRoles.PayrollManagement,
            UserRoles.Employee
        };

        logger.LogInformation("Starting role seeding process...");
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                }
                else
                {
                    foreach (var error in roleResult.Errors)
                    {
                        logger.LogError("Error creating role '{RoleName}': {ErrorCode} - {ErrorDescription}", roleName, error.Code, error.Description);
                    }
                }
            }
            else
            {
                logger.LogDebug("Role '{RoleName}' already exists. Skipping creation.", roleName); // Dùng Debug level cho log này
            }
        }
        logger.LogInformation("Role seeding process finished.");
    }
    catch (Exception ex)
    {
        // Ghi log lỗi nếu quá trình seeding gặp vấn đề (ví dụ: không kết nối được DB)
        logger.LogError(ex, "An error occurred while seeding the database roles.");
        // Có thể throw lại lỗi nếu việc seed role là bắt buộc để ứng dụng chạy đúng
        // throw;
    }
}

// --- Chạy ứng dụng ---
app.Run();
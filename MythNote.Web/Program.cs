using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MythNote;
using MythNote.Web;
using MythNote.Web.DTOs;
using MythNote.Web.Git;
using MythNote.Web.Models;
using MythNote.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy(); });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

// 配置数据
var databaseEngine = builder.Configuration.GetValue<string>("Database:Engine");
var databaseConnection = builder.Configuration.GetValue<string>("Database:Connection");
var connectionStr = builder.Configuration.GetConnectionString(databaseConnection);


if (databaseEngine == "Mysql")
{
    builder.Services.AddDbContext<AppDbContext>(options => options
        .UseMySql(connectionStr,
            new MySqlServerVersion(new Version(8, 0, 21)))
        .EnableSensitiveDataLogging()
    );
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionStr));
}


builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IUploadService, UploadService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // --- 新增拦截逻辑 ---
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                // 跳过默认的 401 逻辑
                context.HandleResponse();

                // 设置状态码为 200
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";

                // 自定义返回结果
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = 401,
                    message = "未授权访问或 Token 已过期",
                    data = (object)null
                });

                await context.Response.WriteAsync(result);
            }
        };
    });

builder.Services.AddAuthorization();


// 1. 注册 Git 管理器为单例
builder.Services.AddSingleton<GitSyncManager>();

// 2. 注册后台托管服务
builder.Services.AddHostedService<GitSyncWorker>();
builder.Services.AddScoped<SessionUser>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DatabaseInitializer.Initialize(context, builder.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();


app.UseAuthentication();
app.UseAuthorization();


app.Use(async (context, next) =>
{
    // 1. 获取用户 ID
    var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
    {
        var user = context.RequestServices.GetRequiredService<SessionUser>();
        user.Id = userId;
    }

    // 3. 务必调用 next，否则请求会在这里中断
    await next(context);
});


app.MapControllers();

app.Run();
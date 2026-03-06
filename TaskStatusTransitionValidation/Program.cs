using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using TaskStatusTransitionValidation.Infrastructure;
using TaskStatusTransitionValidation.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================
// Controllers / JSON
// ============================
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(opt =>
    {
        opt.InvalidModelStateResponseFactory = ctx =>
        {
            var pd = new ValidationProblemDetails(ctx.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Type = "https://httpstatuses.com/400"
            };
            return new BadRequestObjectResult(pd);
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// ============================
// JWT Authentication
// ============================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        var key = jwt["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is missing.");

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwt["Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// ============================
// Current User
// ============================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ============================
// EF Core
// ============================
// Azure SQL / SQL Server では一時的な接続失敗があり得るため、
// 接続リトライを有効化して起動時の不安定さを下げる。
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");

    opt.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

// ============================
// Repositories
// ============================
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IProjectRepository, EfProjectRepository>();
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();

// ============================
// Services
// ============================
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ============================
// Status Transition
// ============================
builder.Services.AddSingleton<ITaskStatusTransitionPolicy, TaskStatusTransitionPolicy>();

// ============================
// CORS
// ============================
var origins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("frontend", p =>
    {
        // 設定が空ならCORS許可しない（安全側）
        if (origins.Length > 0)
        {
            p.WithOrigins(origins)
             .AllowAnyHeader()
             .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// ============================
// Middleware
// ============================
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

// ============================
// Endpoints
// ============================
// ここでは DB 非依存の Health を返す。
// 「アプリが起動しているか」と「DB が生きているか」をまず分離して確認しやすくするため。
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Text(
@"TaskStatusTransitionValidation API

- Swagger UI: /swagger
- Health: /health
", "text/plain"));

app.MapControllers();

// ============================
// DB Migration / Seed
// ============================
// Azure 安定化のため、Migration と Seed は「毎回自動実行」ではなく
// 明示フラグで必要時のみ実行する。
// 
// 想定運用:
// - 通常運用時:
//     ENABLE_DB_MIGRATION = false
//     ENABLE_DB_SEED      = false
//
// - 初回DB作成・初回データ投入時のみ:
//     ENABLE_DB_MIGRATION = true
//     ENABLE_DB_SEED      = true
//
// 重要:
// - Seed は初回だけ必要でも、毎回起動時に自動で走らせない。
// - 以前は DbSeederHostedService を AddHostedService で常時登録していたが、
//   それだと「アプリ起動のたびに DB 接続が発生」し、Azure 上で 500.30 の原因候補になる。
// - そのため、必要時だけ手動フラグで明示実行する形に変更している。

var enableMigration = builder.Configuration.GetValue<bool>("ENABLE_DB_MIGRATION");
var enableSeed = builder.Configuration.GetValue<bool>("ENABLE_DB_SEED");

if (enableMigration || enableSeed)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("StartupDbInit");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        if (enableMigration)
        {
            logger.LogInformation("Database migration started.");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migration completed.");
        }

        if (enableSeed)
        {
            logger.LogInformation("Database seed started.");

            // 以前の AddHostedService<DbSeederHostedService>() は無効化し、
            // 初回だけここで明示的に実行する。
            //
            // もし既に Users が1件以上あるなら、Seeder側の実装により何もしない想定。
            var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<DbSeederHostedService>>();
            var seeder = new DbSeederHostedService(scope.ServiceProvider, seederLogger);
            await seeder.StartAsync(CancellationToken.None);

            logger.LogInformation("Database seed completed.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed during startup.");
        throw;
    }
}

app.Run();
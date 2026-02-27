// ============================
// Program.cs
// ============================
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using TaskStatusTransitionValidation.Infrastructure;
using TaskStatusTransitionValidation.Services;

var builder = WebApplication.CreateBuilder(args);

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

// JWT認証
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        var key = jwt["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is missing.");

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


// CurrentUser
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ===== EF Core =====
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

// ===== Repositories（EF版へ差し替え）=====
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IProjectRepository, EfProjectRepository>();
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();

// ===== Services =====
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Status Transition
builder.Services.AddSingleton<ITaskStatusTransitionPolicy, TaskStatusTransitionPolicy>();

// Seeder（任意：初回起動時だけデモデータ投入）
builder.Services.AddHostedService<DbSeederHostedService>();

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

// 例外ハンドリング
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

app.MapControllers();



// ★置き換え：フラグがtrueのときだけMigrate
var enableMigration = builder.Configuration.GetValue<bool>("ENABLE_DB_MIGRATION");
if (enableMigration)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();


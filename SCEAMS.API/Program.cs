using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SCEAMS.Application.Interfaces;
using SCEAMS.Application.Services;
using SCEAMS.Api.BackgroundServices;
using SCEAMS.Domain.Entities;
using SCEAMS.Infrastructure.Authentication;
using SCEAMS.Infrastructure.AI;
using SCEAMS.Infrastructure.Data;
using SCEAMS.Infrastructure.Data.Seed;
using SCEAMS.Infrastructure.Health;
using SCEAMS.Infrastructure.GrpcClients;
using SCEAMS.Infrastructure.Repositories;
using SCEAMS.Infrastructure.Security;
using SCEAMS.Infrastructure.UnitOfWork;
using SCEAMS.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration
        .AddJsonFile(
            "appsettings.Local.json",
            optional: true,
            reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args);
}

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. " +
        "Use appsettings.Local.json, .NET User Secrets, or an environment variable.");

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Jwt configuration is missing.");
jwtOptions.Validate();

builder.Services.AddControllers(options =>
    {
        options.ReturnHttpNotAcceptable = true;
    })
    .AddXmlSerializerFormatters()
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .Expand()
        .Count()
        .SetMaxTop(50));
builder.Services.AddProblemDetails();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "Request chứa dữ liệu không hợp lệ.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description =
                "Enter a JWT access token using: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }
            ] = []
        });
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "email",
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await ProblemDetailsWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    "Yêu cầu xác thực hợp lệ để truy cập tài nguyên này.");
            },
            OnForbidden = async context =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ProblemDetailsWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    "Bạn không có quyền truy cập tài nguyên này.");
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddDbContext<SceamsDbContext>(options =>
    // Các workflow đăng ký, điểm danh và feedback dùng transaction thủ công
    // để bảo vệ capacity/concurrency. Không bật execution strategy retry ở
    // đây vì EF Core không cho chạy user-initiated transaction bên trong
    // strategy mặc định nếu chưa bọc toàn bộ workflow bằng ExecuteAsync.
    options.UseSqlServer(connectionString));

builder.Services.AddScoped(
    typeof(IGenericRepository<>),
    typeof(GenericRepository<>));
builder.Services.AddScoped<
    IClubCategoryRepository,
    ClubCategoryRepository>();
builder.Services.AddScoped<IClubRepository, ClubRepository>();
builder.Services.AddScoped<IVenueRepository, VenueRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<
    IRegistrationRepository,
    RegistrationRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<
    IClubCategoryService,
    ClubCategoryService>();
builder.Services.AddScoped<IClubService, ClubService>();
builder.Services.AddScoped<IClubMembershipService, ClubMembershipService>();
builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventFaqRetrievalService, EventFaqRetrievalService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IChatHistoryService, ChatHistoryService>();
builder.Services.AddScoped<IChatRateLimiter, ChatRateLimiter>();
builder.Services.Configure<AiProviderOptions>(
    builder.Configuration.GetSection(AiProviderOptions.SectionName));
builder.Services.AddHttpClient<IAiProvider, HttpAiProvider>(
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<AiProviderOptions>>()
            .Value;
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 120));
    });
builder.Services.AddScoped<IEventStatusSyncService, EventStatusSyncService>();
builder.Services.Configure<EventReminderOptions>(
    builder.Configuration.GetSection(EventReminderOptions.SectionName));
builder.Services.AddScoped<INotificationReminderStore, NotificationReminderStore>();
builder.Services.AddScoped<IEventReminderService, EventReminderService>();
builder.Services.AddSingleton<EventReminderBackgroundService>();
builder.Services.AddHostedService(sp =>
    sp.GetRequiredService<EventReminderBackgroundService>());
builder.Services.Configure<NotificationGrpcOptions>(
    builder.Configuration.GetSection(NotificationGrpcOptions.SectionName));
builder.Services.AddSingleton<INotificationLogStore, NotificationLogStore>();
builder.Services.AddScoped<INotificationClientService, NotificationClientService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddHostedService<EventStatusSyncBackgroundService>();
builder.Services.AddScoped<IAccessTokenService, JwtAccessTokenService>();

builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<
    IDatabaseHealthService,
    DatabaseHealthService>();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<SeedDataOptions>(
    builder.Configuration.GetSection(SeedDataOptions.SectionName));
builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.UseStatusCodePages(async statusContext =>
{
    var httpContext = statusContext.HttpContext;
    if (!httpContext.Request.Path.StartsWithSegments("/api") ||
        httpContext.Response.StatusCode is not (
            StatusCodes.Status400BadRequest or
            StatusCodes.Status401Unauthorized or
            StatusCodes.Status403Forbidden or
            StatusCodes.Status404NotFound or
            StatusCodes.Status409Conflict or
            StatusCodes.Status406NotAcceptable or
            StatusCodes.Status500InternalServerError))
    {
        return;
    }

    var statusCode = httpContext.Response.StatusCode;
    var (title, detail) = statusCode switch
    {
        StatusCodes.Status400BadRequest => ("Bad request", "Request không hợp lệ."),
        StatusCodes.Status401Unauthorized => ("Unauthorized", "Yêu cầu xác thực hợp lệ."),
        StatusCodes.Status403Forbidden => ("Forbidden", "Bạn không có quyền truy cập."),
        StatusCodes.Status404NotFound => ("Not found", "Không tìm thấy tài nguyên."),
        StatusCodes.Status406NotAcceptable => ("Not acceptable", "Định dạng response không được hỗ trợ."),
        StatusCodes.Status409Conflict => ("Conflict", "Yêu cầu xung đột với trạng thái hiện tại."),
        _ => ("Internal server error", "Đã xảy ra lỗi ngoài dự kiến.")
    };
    await ProblemDetailsWriter.WriteAsync(httpContext, statusCode, title, detail);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Development seed can only run in the Development environment.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var databaseSeeder = scope.ServiceProvider
        .GetRequiredService<DatabaseSeeder>();

    await databaseSeeder.SeedAsync();
    return;
}

app.Run();

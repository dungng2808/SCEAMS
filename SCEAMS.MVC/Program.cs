using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using SCEAMS.MVC.Handlers;
using SCEAMS.MVC.Options;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.Services.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".SCEAMS.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromMinutes(65);
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".SCEAMS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddSingleton<RefreshTokenCoordinator>();

builder.Services
    .AddOptions<ApiSettings>()
    .Bind(builder.Configuration.GetSection(ApiSettings.SectionName))
    .Validate(
        settings => Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _),
        "ApiSettings:BaseUrl must be an absolute URL.")
    .Validate(
        settings => settings.TimeoutSeconds > 0,
        "ApiSettings:TimeoutSeconds must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddHttpClient<IHealthApiClient, HealthApiClient>(
    (serviceProvider, client) =>
    {
        var settings = serviceProvider
            .GetRequiredService<IOptions<ApiSettings>>()
            .Value;

        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<
    IClubCategoryApiClient,
    ClubCategoryApiClient>(
    (serviceProvider, client) =>
    {
        var settings = serviceProvider
            .GetRequiredService<IOptions<ApiSettings>>()
            .Value;

        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(
    (serviceProvider, client) =>
    {
        var settings = serviceProvider
            .GetRequiredService<IOptions<ApiSettings>>()
            .Value;

        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    });
builder.Services.AddHttpClient<IUserApiClient, UserApiClient>(
    (serviceProvider, client) =>
    {
        var settings = serviceProvider
            .GetRequiredService<IOptions<ApiSettings>>()
            .Value;

        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

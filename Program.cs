using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using StockPortal.Data;
using StockPortal.Middleware;
using StockPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();

// Session support (for testing identity switching)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Localization configuration
builder.Services.AddLocalization();
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("ar")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "StockPortal.DomainAuth";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

// Multi-provider Database setup (SQLite by default for zero-config run, or MySQL/MariaDB)
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
var mySqlConn = builder.Configuration.GetConnectionString("MySqlConnection") 
                ?? "server=localhost;port=3306;database=stock;user=root;password=;CharSet=utf8mb4;";
var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection") 
                 ?? builder.Configuration.GetConnectionString("DefaultConnection") 
                 ?? "Data Source=stockportal.db";

if (dbProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<StockDbContext>(options =>
        options.UseMySql(mySqlConn, new MySqlServerVersion(new Version(8, 0, 36)))
    );
}
else
{
    builder.Services.AddDbContext<StockDbContext>(options =>
        options.UseSqlite(sqliteConn)
    );
}

var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<StockDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Request Localization Middleware
app.UseRequestLocalization();

app.UseSession();
app.UseAuthentication();
app.UseDomainAuthentication(); // Automatically signs in domain user without password!
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

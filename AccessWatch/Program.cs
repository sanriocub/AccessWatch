using Amazon;
using Amazon.SimpleNotificationService;
using AccessWatch.Services;
using AccessWatch.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register HttpClient for AWS API Gateway communication
builder.Services.AddHttpClient();

// Register EF Core with the connection string from appsettings.json
// (locally this points at your local DB; on AWS it points at RDS)
builder.Services.AddDbContext<AccessWatchDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AccessWatchDb")));

var awsRegion = builder.Configuration["AWS:Region"] ?? "us-east-1";
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
    new AmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(awsRegion)));
builder.Services.AddSingleton<ISnsNotificationService, SnsNotificationService>();

// Cookie authentication for register/login/logout
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed the first Platform Administrator account, if one doesn't exist yet.
// Runs at startup (not via migration HasData) since password hashing needs
// a random salt each time -- doing it here avoids the non-deterministic model warning.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccessWatchDbContext>();
    db.Database.Migrate();

    bool adminExists = db.Users.Any(u => u.Role == UserRole.PlatformAdministrator);

    if (!adminExists)
    {
        var hasher = new PasswordHasher<User>();

        var admin = new User
        {
            Name = "System Admin",
            Email = "admin@accesswatch.com",
            Role = UserRole.PlatformAdministrator,
            CreatedAt = DateTime.UtcNow
        };

        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

        db.Users.Add(admin);
        db.SaveChanges();
    }
}

app.Run();
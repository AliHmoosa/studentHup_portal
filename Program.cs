using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentHub.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddScoped<StudentHub.Services.IDashboardService, StudentHub.Services.DemoDashboardService>();
builder.Services.AddDbContext<StudentHubDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("StudentHubDb")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => { options.User.RequireUniqueEmail = true; options.Password.RequiredLength = 8; options.Password.RequireDigit = true; options.Password.RequireUppercase = true; options.Password.RequireLowercase = true; options.Password.RequireNonAlphanumeric = false; }).AddEntityFrameworkStores<StudentHubDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options => { options.LoginPath = "/Account/Login"; options.AccessDeniedPath = "/Account/AccessDenied"; options.Cookie.Name = "__Host-StudentHub"; options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Lax; options.SlidingExpiration = true; options.ExpireTimeSpan = TimeSpan.FromHours(8); });

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();

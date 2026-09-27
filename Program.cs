using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentHub.Data;
using StudentHub.Services;

var builder = WebApplication.CreateBuilder(args);

// SERVICES

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IAcademicTermService, AcademicTermService>();

builder.Services.AddLocalization(options =>
    options.ResourcesPath = "Resources");

builder.Services.AddScoped<
    StudentHub.Services.IDashboardService,
    StudentHub.Services.DemoDashboardService>();

builder.Services.AddScoped<
    IEmailService,
    SmtpEmailService>();

builder.Services.AddScoped<
    ICourseService,
    CourseService>();

builder.Services.AddDbContext<StudentHubDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("StudentHubDb")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Password reset token validity.
        options.Tokens.PasswordResetTokenProvider =
            TokenOptions.DefaultProvider;
    })
    .AddEntityFrameworkStores<StudentHubDbContext>()
    .AddDefaultTokenProviders();


// Google login

builder.Services
    .AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId =
            builder.Configuration[
                "Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException(
                "Google ClientId is not configured.");

        options.ClientSecret =
            builder.Configuration[
                "Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException(
                "Google ClientSecret is not configured.");

        options.CallbackPath =
            "/signin-google";
    });


// COOKIE

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath =
        "/Account/Login";

    options.AccessDeniedPath =
        "/Account/AccessDenied";

    options.Cookie.Name =
        "StudentHub.Auth";

    options.Cookie.HttpOnly = true;

    options.Cookie.SameSite =
        SameSiteMode.Lax;

    options.SlidingExpiration = true;

    options.ExpireTimeSpan =
        TimeSpan.FromHours(8);
});


// APP

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(
    app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScentedCandleWebsite.Data;
using ScentedCandleWebsite.Models;
using ScentedCandleWebsite.Models.Binders;
using ScentedCandleWebsite.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Register the decimal model binder
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
});



// Register Email Service



// Configure decimal model binding


// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity with roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false; // Set to true for production
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure authentication with Google and Facebook
//builder.Services.AddAuthentication()
//    .AddGoogle(options =>
//    {
//        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
//        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
//    })
//    .AddFacebook(options =>
//    {
//        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "";
//        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "";
//    });

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Register email service
builder.Services.AddTransient<IEmailService, EmailService>();

// Add authorization policies with role requirements
builder.Services.AddAuthorization(options =>
{
    // Admin only policy
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Customer only policy
    options.AddPolicy("CustomerOnly", policy =>
        policy.RequireRole("Customer"));

    // Admin or Customer policy (authenticated users)
    options.AddPolicy("AuthenticatedUsers", policy =>
        policy.RequireRole("Admin", "Customer"));

    // Admin can access everything, customers have limited access
    options.AddPolicy("ProductManagement", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("OrderManagement", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("ViewOrders", policy =>
        policy.RequireRole("Admin", "Customer"));
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

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.Initialize(services);
        Console.WriteLine("Database seeded successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding database: {ex.Message}");
    }
}

app.Run();
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;
using BarideWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
});
builder.Services.AddDbContext<BarideDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 1;
    options.Password.RequiredUniqueChars = 1;
})
.AddEntityFrameworkStores<BarideDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();

// Add TenantId claim to the user principal on sign-in
builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, TenantClaimsPrincipalFactory>();

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<BarideDbContext>();
    db.Database.EnsureCreated();

    // Seed roles
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in new[] { "Admin", "TenantAdmin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed default tenant
    var defaultTenantId = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    if (!db.Tenants.Any(t => t.TenantId == defaultTenantId))
    {
        // Tenant is seeded via HasData, but ensure it exists
    }

    // Seed default admin user
    var userManager = services.GetRequiredService<UserManager<AppUser>>();

    var adminUser = await userManager.FindByNameAsync("admin");
    if (adminUser == null)
    {
        adminUser = new AppUser
        {
            UserName = "admin",
            FullName = "المدير",
            EmailConfirmed = true,
            TenantId = null // Global admin, no tenant
        };
        await userManager.CreateAsync(adminUser, "admin");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
    else if (adminUser.TenantId != null)
    {
        // Ensure the global admin has no tenant
        adminUser.TenantId = null;
        await userManager.UpdateAsync(adminUser);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "Dynamic-Web-TWAIN-19.3.2", "dist", "dist")),
    RequestPath = "/Dynamic-Web-TWAIN-19.3.2/dist/dist",
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

// Custom claims principal factory that adds TenantId as a claim
public class TenantClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    private readonly BarideDbContext _context;

    public TenantClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        Microsoft.Extensions.Options.IOptions<IdentityOptions> options,
        BarideDbContext context)
        : base(userManager, roleManager, options)
    {
        _context = context;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.TenantId.HasValue)
        {
            identity.AddClaim(new Claim("TenantId", user.TenantId.Value.ToString()));

            var tenant = await _context.Tenants.FindAsync(user.TenantId.Value);
            if (tenant != null)
            {
                identity.AddClaim(new Claim("TenantName", tenant.Name));
            }
        }

        return identity;
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
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
    options.Conventions.AllowAnonymousToPage("/Correspondances/SharedView");
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
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});
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

// User search API for autocomplete
app.MapGet("/api/users/search", async (string? q, UserManager<AppUser> userManager, HttpContext httpContext) =>
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        return Results.Json(Array.Empty<object>());

    var currentUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var tenantClaim = httpContext.User.FindFirst("TenantId");
    Guid? tenantId = tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tid) ? tid : null;

    var query = userManager.Users
        .Where(u => u.Id != currentUserId)
        .Where(u => u.FullName.Contains(q) || u.UserName!.Contains(q));

    if (tenantId.HasValue)
        query = query.Where(u => u.TenantId == tenantId);

    var users = await query.Take(10)
        .Select(u => new { u.Id, u.FullName, u.UserName })
        .ToListAsync();

    return Results.Json(users);
}).RequireAuthorization();

// Contact search API for autocomplete
app.MapGet("/api/contacts/search", async (string? q, BarideDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        return Results.Json(Array.Empty<object>());

    var contacts = await db.Contacts
        .Where(c => c.Name.Contains(q) || c.Email.Contains(q))
        .Take(10)
        .Select(c => new { c.ContactId, c.Name, c.Email, c.Phone })
        .ToListAsync();

    return Results.Json(contacts);
}).RequireAuthorization();

// Server-side AG Grid data source
app.MapGet("/api/correspondances", async (
    int startRow,
    int endRow,
    int? type,
    string? search,
    Guid? catId,
    string? exped,
    string? objet,
    DateTime? dateFrom,
    DateTime? dateTo,
    string? sortField,
    string? sortDir,
    BarideDbContext db,
    HttpContext httpContext) =>
{
    IQueryable<Corresp> query = db.Correspondances.Include(c => c.Categ);

    if (!string.IsNullOrWhiteSpace(search))
    {
        query = query.Where(c =>
            c.Num.Contains(search) ||
            c.NumInterne.Contains(search) ||
            c.Expediteur.Contains(search) ||
            c.Objet.Contains(search) ||
            (c.Observation != null && c.Observation.Contains(search)));
    }
    else if (type.HasValue)
    {
        var tp = (TypeCorresp)type.Value;
        query = query.Where(c => c.Type == tp);
    }

    if (catId.HasValue)
        query = query.Where(c => c.CatId == catId.Value);
    if (!string.IsNullOrWhiteSpace(exped))
        query = query.Where(c => c.Expediteur.Contains(exped));
    if (!string.IsNullOrWhiteSpace(objet))
        query = query.Where(c => c.Objet.Contains(objet));
    if (dateFrom.HasValue)
        query = query.Where(c => c.DateCorresp >= dateFrom.Value);
    if (dateTo.HasValue)
        query = query.Where(c => c.DateCorresp <= dateTo.Value.AddDays(1));

    var totalCount = await query.CountAsync();

    // Sorting
    query = sortField switch
    {
        "num" => sortDir == "asc" ? query.OrderBy(c => c.Num) : query.OrderByDescending(c => c.Num),
        "numInterne" => sortDir == "asc" ? query.OrderBy(c => c.NumInterne) : query.OrderByDescending(c => c.NumInterne),
        "dateCorresp" => sortDir == "asc" ? query.OrderBy(c => c.DateCorresp) : query.OrderByDescending(c => c.DateCorresp),
        "dateArrivDepart" => sortDir == "asc" ? query.OrderBy(c => c.DateArrivDepart) : query.OrderByDescending(c => c.DateArrivDepart),
        "expediteur" => sortDir == "asc" ? query.OrderBy(c => c.Expediteur) : query.OrderByDescending(c => c.Expediteur),
        "objet" => sortDir == "asc" ? query.OrderBy(c => c.Objet) : query.OrderByDescending(c => c.Objet),
        "observation" => sortDir == "asc" ? query.OrderBy(c => c.Observation) : query.OrderByDescending(c => c.Observation),
        "categorie" => sortDir == "asc" ? query.OrderBy(c => c.Categ!.Designation) : query.OrderByDescending(c => c.Categ!.Designation),
        _ => query.OrderByDescending(c => c.DateArrivDepart)
    };

    var pageSize = endRow - startRow;
    var rows = await query.Skip(startRow).Take(pageSize)
        .Select(c => new
        {
            cid = c.Cid,
            num = c.Num,
            numInterne = c.NumInterne,
            dateCorresp = c.DateCorresp.ToString("yyyy-MM-dd"),
            dateArrivDepart = c.DateArrivDepart.ToString("yyyy-MM-dd"),
            expediteur = c.Expediteur,
            objet = c.Objet,
            observation = c.Observation ?? "",
            categorie = c.Categ != null ? c.Categ.Designation : "",
            type = (int?)c.Type
        })
        .ToListAsync();

    // Get transfert counts for this page
    var cids = rows.Select(r => r.cid).ToList();
    var transfertCounts = await db.Transferts
        .Where(t => cids.Contains(t.Cid))
        .GroupBy(t => t.Cid)
        .Select(g => new { g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.Key, x => x.Count);

    var result = rows.Select(r => new
    {
        r.cid,
        r.num,
        r.numInterne,
        r.dateCorresp,
        r.dateArrivDepart,
        r.expediteur,
        r.objet,
        r.observation,
        r.categorie,
        typeLabel = r.type switch
        {
            0 => "وارد داخلي",
            1 => "وارد خارجي",
            2 => "صادر داخلي",
            3 => "صادر خارجي",
            4 => "متفرقات",
            _ => ""
        },
        transfertCount = transfertCounts.GetValueOrDefault(r.cid, 0)
    });

    return Results.Json(new { rows = result, totalCount });
}).RequireAuthorization();

app.Run("http://localhost:5226");

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

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Models;
using BarideWeb.Services;

namespace BarideWeb.Data
{
    public class BarideDbContext : IdentityDbContext<AppUser>
    {
        private readonly ITenantService? _tenantService;

        public BarideDbContext(DbContextOptions<BarideDbContext> options, ITenantService? tenantService = null)
            : base(options)
        {
            _tenantService = tenantService;
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Categorie> Categories { get; set; }
        public DbSet<Corresp> Correspondances { get; set; }
        public DbSet<Doc> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed default tenant
            var defaultTenantId = Guid.Parse("b0000000-0000-0000-0000-000000000001");
            modelBuilder.Entity<Tenant>().HasData(
                new Tenant { TenantId = defaultTenantId, Name = "المؤسسة الرئيسية" }
            );

            modelBuilder.Entity<Categorie>().HasData(
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-000000000001"), Designation = "المراسلات", TenantId = defaultTenantId },
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-000000000002"), Designation = "المحاضر", TenantId = defaultTenantId },
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-000000000003"), Designation = "التقارير", TenantId = defaultTenantId },
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-000000000004"), Designation = "عروض الحال", TenantId = defaultTenantId },
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-000000000005"), Designation = "سندات الطلب", TenantId = defaultTenantId },
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-000000000006"), Designation = "الدعوات", TenantId = defaultTenantId },
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-000000000007"), Designation = "الاعلانات", TenantId = defaultTenantId },
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-000000000008"), Designation = "المذكرات", TenantId = defaultTenantId },
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-000000000009"), Designation = "الشهادات", TenantId = defaultTenantId },
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-00000000000a"), Designation = "المقررات", TenantId = defaultTenantId },
                new Categorie { CatId = Guid.Parse("a0000000-0000-0000-0000-00000000000b"), Designation = "التهاني", TenantId = defaultTenantId }
            );

            // Global query filters for multi-tenancy
            modelBuilder.Entity<Categorie>().HasQueryFilter(c => _tenantService == null || _tenantService.GetCurrentTenantId() == null || c.TenantId == _tenantService.GetCurrentTenantId());
            modelBuilder.Entity<Corresp>().HasQueryFilter(c => _tenantService == null || _tenantService.GetCurrentTenantId() == null || c.TenantId == _tenantService.GetCurrentTenantId());
        }

        public override int SaveChanges()
        {
            SetTenantId();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetTenantId();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void SetTenantId()
        {
            var tenantId = _tenantService?.GetCurrentTenantId();
            if (tenantId == null) return;

            foreach (var entry in ChangeTracker.Entries<Categorie>().Where(e => e.State == EntityState.Added && e.Entity.TenantId == null))
            {
                entry.Entity.TenantId = tenantId;
            }

            foreach (var entry in ChangeTracker.Entries<Corresp>().Where(e => e.State == EntityState.Added && e.Entity.TenantId == null))
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }
}

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
        public DbSet<AppParameter> Parameters { get; set; }
        public DbSet<Transfert> Transferts { get; set; }
        public DbSet<Contact> Contacts { get; set; }

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

            modelBuilder.Entity<AppParameter>().HasData(
                new AppParameter
                {
                    ParamId = Guid.Parse("c0000000-0000-0000-0000-000000000001"),
                    Key = "CorrespViewMode",
                    Value = "0",
                    Description = "طريقة عرض المراسلة: 0=في صفحة جديدة، 1=في نافذة منبثقة، 2=في نفس الصفحة",
                    TenantId = defaultTenantId
                },
                new AppParameter
                {
                    ParamId = Guid.Parse("c0000000-0000-0000-0000-000000000010"),
                    Key = "ScannerDpi",
                    Value = "200",
                    Description = "الدقة الافتراضية للماسح الضوئي (DPI): 100, 150, 200, 300, 600",
                    TenantId = defaultTenantId
                },
                new AppParameter
                {
                    ParamId = Guid.Parse("c0000000-0000-0000-0000-000000000011"),
                    Key = "ScannerPixelMode",
                    Value = "Grayscale",
                    Description = "وضع اللون الافتراضي للماسح الضوئي: Color, Grayscale",
                    TenantId = defaultTenantId
                },
                new AppParameter
                {
                    ParamId = Guid.Parse("c0000000-0000-0000-0000-000000000012"),
                    Key = "ScannerImageFormat",
                    Value = "PDF",
                    Description = "صيغة الصورة الافتراضية للماسح الضوئي: JPG, PNG, PDF",
                    TenantId = defaultTenantId
                }
            );

            // Global query filters for multi-tenancy
            modelBuilder.Entity<Categorie>().HasQueryFilter(c => _tenantService == null || _tenantService.GetCurrentTenantId() == null || c.TenantId == _tenantService.GetCurrentTenantId());
            modelBuilder.Entity<Corresp>().HasQueryFilter(c => _tenantService == null || _tenantService.GetCurrentTenantId() == null || c.TenantId == _tenantService.GetCurrentTenantId());
            modelBuilder.Entity<AppParameter>().HasQueryFilter(p => _tenantService == null || _tenantService.GetCurrentTenantId() == null || p.TenantId == _tenantService.GetCurrentTenantId());
            modelBuilder.Entity<Transfert>().HasQueryFilter(t => _tenantService == null || _tenantService.GetCurrentTenantId() == null || t.TenantId == _tenantService.GetCurrentTenantId());
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

            foreach (var entry in ChangeTracker.Entries<AppParameter>().Where(e => e.State == EntityState.Added && e.Entity.TenantId == null))
            {
                entry.Entity.TenantId = tenantId;
            }

            foreach (var entry in ChangeTracker.Entries<Transfert>().Where(e => e.State == EntityState.Added && e.Entity.TenantId == null))
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }
}

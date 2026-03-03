using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BarideWeb.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = "";

        [ForeignKey("Tenant")]
        public Guid? TenantId { get; set; }

        public Tenant? Tenant { get; set; }
    }
}

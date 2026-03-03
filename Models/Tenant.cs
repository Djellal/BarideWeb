using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarideWeb.Models
{
    public class Tenant
    {
        public Tenant()
        {
            TenantId = Guid.NewGuid();
            Name = "مؤسسة جديدة";
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid TenantId { get; set; }

        [MaxLength(300)]
        [Display(Name = "اسم المؤسسة")]
        public string Name { get; set; }

        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<Categorie> Categories { get; set; } = new List<Categorie>();
        public ICollection<Corresp> Correspondances { get; set; } = new List<Corresp>();

        public override string ToString() => Name;
    }
}

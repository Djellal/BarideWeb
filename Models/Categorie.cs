using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarideWeb.Models
{
    public class Categorie : ITenantEntity
    {
        public Categorie()
        {
            CatId = Guid.NewGuid();
            Designation = "تصنيف جديد";
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid CatId { get; set; }

        [MaxLength(300)]
        [Display(Name = "التصنيف")]
        public string Designation { get; set; }

        [ForeignKey("Tenant")]
        public Guid? TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public ICollection<Corresp> Corresps { get; set; } = new List<Corresp>();

        public override string ToString() => Designation;
    }
}

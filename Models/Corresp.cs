using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarideWeb.Models
{
    public class Corresp
    {
        public Corresp()
        {
            Cid = Guid.NewGuid();
            Expediteur = "-";
            Objet = "-";
            DateCorresp = DateTime.Now;
            DateArrivDepart = DateTime.Now;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Cid { get; set; }

        [MaxLength(100)]
        [Display(Name = "الرقم")]
        public string Num { get; set; } = "";

        [MaxLength(100)]
        [Display(Name = "الرقم الداخلي")]
        public string NumInterne { get; set; } = "";

        [Display(Name = "تاريخ الوصول/الارسال")]
        [DataType(DataType.Date)]
        public DateTime DateArrivDepart { get; set; }

        [Display(Name = "تاريخ المراسلة")]
        [DataType(DataType.Date)]
        public DateTime DateCorresp { get; set; }

        [Display(Name = "المرسل")]
        public string Expediteur { get; set; }

        [Display(Name = "ملاحظة")]
        public string? Observation { get; set; }

        [MaxLength(300)]
        [Display(Name = "الموضوع")]
        public string Objet { get; set; }

        public Guid CorrespRep { get; set; }

        [Display(Name = "النوع")]
        public TypeCorresp? Type { get; set; }

        [ForeignKey("Categ")]
        [Display(Name = "الصنف")]
        public Guid CatId { get; set; }

        public Categorie? Categ { get; set; }

        [ForeignKey("Tenant")]
        public Guid? TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public ICollection<Doc> Docs { get; set; } = new List<Doc>();
    }
}

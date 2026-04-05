using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarideWeb.Models
{
    public class AppParameter : ITenantEntity
    {
        public AppParameter()
        {
            ParamId = Guid.NewGuid();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid ParamId { get; set; }

        [MaxLength(100)]
        [Display(Name = "المفتاح")]
        public string Key { get; set; } = "";

        [MaxLength(300)]
        [Display(Name = "القيمة")]
        public string Value { get; set; } = "";

        [Display(Name = "القيمة الطويلة")]
        public string? LongValue { get; set; }

        [MaxLength(300)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [ForeignKey("Tenant")]
        public Guid? TenantId { get; set; }

        public Tenant? Tenant { get; set; }
    }
}

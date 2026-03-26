using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarideWeb.Models
{
    public class Transfert : ITenantEntity, IAuditable
    {
        public Transfert()
        {
            TransfertId = Guid.NewGuid();
            DateTransfert = DateTime.UtcNow;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid TransfertId { get; set; }

        [Display(Name = "تاريخ التحويل")]
        [DataType(DataType.Date)]
        public DateTime DateTransfert { get; set; }

        [Display(Name = "ملاحظة")]
        public string? Note { get; set; }

        [Display(Name = "تمت المعاينة")]
        public bool IsViewed { get; set; }

        [ForeignKey("Corresp")]
        public Guid Cid { get; set; }

        public Corresp? Corresp { get; set; }

        [ForeignKey("Sender")]
        public string? SenderId { get; set; }

        public AppUser? Sender { get; set; }

        [ForeignKey("Receiver")]
        public string? ReceiverId { get; set; }

        public AppUser? Receiver { get; set; }

        [ForeignKey("Tenant")]
        public Guid? TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

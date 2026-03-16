using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarideWeb.Models
{
    public class Contact
    {
        public Contact()
        {
            ContactId = Guid.NewGuid();
            Name = "";
            Email = "";
            Phone = "";
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid ContactId { get; set; }

        [MaxLength(200)]
        [Display(Name = "الاسم")]
        public string Name { get; set; }

        [MaxLength(200)]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; }

        [MaxLength(50)]
        [Display(Name = "الهاتف")]
        public string Phone { get; set; }
    }
}

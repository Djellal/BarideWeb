using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarideWeb.Models
{
    public class Doc
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid DocId { get; set; } = Guid.NewGuid();

        [MaxLength(300)]
        [Display(Name = "الوصف")]
        public string? Designation { get; set; }

        [MaxLength(300)]
        public string? Chemin { get; set; }

        [Display(Name = "الملف")]
        public string? FileName { get; set; }

        [ForeignKey("Corresp")]
        public Guid Cid { get; set; }

        public Corresp? Corresp { get; set; }
    }
}

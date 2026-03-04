namespace BarideWeb.Models
{
    public class CorrespViewData
    {
        public Corresp Corresp { get; set; } = null!;
        public List<Doc> Documents { get; set; } = new();
        public string TypeLabel { get; set; } = "";
        public string ExpedLabel { get; set; } = "المرسل";
        public bool IsModal { get; set; }
    }
}

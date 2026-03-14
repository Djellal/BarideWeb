using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Pages.Correspondances
{
    public class ViewModel : PageModel
    {
        private readonly BarideDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ViewModel(BarideDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Corresp Corresp { get; set; } = null!;
        public List<Doc> Documents { get; set; } = new();
        public List<AppUser> Users { get; set; } = new();
        public string TypeLabel { get; set; } = "";
        public string ExpedLabel { get; set; } = "المرسل";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var corresp = await _context.Correspondances
                .Include(c => c.Categ)
                .FirstOrDefaultAsync(c => c.Cid == id);

            if (corresp == null) return NotFound();

            Corresp = corresp;
            Documents = await _context.Documents.Where(d => d.Cid == id).ToListAsync();
            await LoadUsers();

            TypeLabel = Corresp.Type switch
            {
                TypeCorresp.Entrant_Interne => "وارد داخلي",
                TypeCorresp.Entrant_Externe => "وارد خارجي",
                TypeCorresp.Sotant_Interne => "صادر داخلي",
                TypeCorresp.Sortant_Externe => "صادر خارجي",
                TypeCorresp.Divers => "متفرقات",
                _ => ""
            };

            ExpedLabel = Corresp.Type == TypeCorresp.Entrant_Interne || Corresp.Type == TypeCorresp.Entrant_Externe
                ? "المرسل" : "المرسل إليه";

            return Page();
        }

        public async Task<IActionResult> OnPostTransferAsync(Guid cid, string receiverId, string? note)
        {
            var corresp = await _context.Correspondances.FindAsync(cid);
            if (corresp == null) return NotFound();

            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var transfert = new Transfert
            {
                Cid = cid,
                SenderId = senderId,
                ReceiverId = receiverId,
                Note = note,
                DateTransfert = DateTime.UtcNow
            };

            _context.Transferts.Add(transfert);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = cid });
        }

        private async Task LoadUsers()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tenantClaim = User.FindFirst("TenantId");
            Guid? tenantId = tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tid) ? tid : null;

            if (tenantId.HasValue)
            {
                Users = await _userManager.Users
                    .Where(u => u.TenantId == tenantId && u.Id != currentUserId)
                    .ToListAsync();
            }
            else
            {
                Users = await _userManager.Users
                    .Where(u => u.Id != currentUserId)
                    .ToListAsync();
            }
        }
    }
}

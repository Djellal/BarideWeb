using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;
using System.Security.Claims;

namespace BarideWeb.Pages.Transferts
{
    public class IndexModel : PageModel
    {
        private readonly BarideDbContext _context;

        public IndexModel(BarideDbContext context)
        {
            _context = context;
        }

        public List<Transfert> Transferts { get; set; } = new();
        public Guid? FilterCid { get; set; }

        public async Task OnGetAsync(Guid? cid = null)
        {
            FilterCid = cid;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = _context.Transferts
                .Include(t => t.Corresp)
                .Include(t => t.Sender)
                .Include(t => t.Receiver)
                .Where(t => t.ReceiverId == userId || t.SenderId == userId);

            if (cid.HasValue)
                query = query.Where(t => t.Cid == cid.Value);

            Transferts = await query
                .OrderByDescending(t => t.DateTransfert)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostMarkViewedAsync(Guid id)
        {
            var transfert = await _context.Transferts.FindAsync(id);
            if (transfert != null)
            {
                transfert.IsViewed = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("TenantAdmin"))
                return Forbid();

            var transfert = await _context.Transferts.FindAsync(id);
            if (transfert != null)
            {
                _context.Transferts.Remove(transfert);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(Guid id, string? note)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("TenantAdmin"))
                return Forbid();

            var transfert = await _context.Transferts.FindAsync(id);
            if (transfert != null)
            {
                transfert.Note = note;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}

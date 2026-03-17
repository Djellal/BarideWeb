using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;
using BarideWeb.Services;

namespace BarideWeb.Pages.Correspondances
{
    public class ViewModel : PageModel
    {
        private readonly BarideDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;

        public ViewModel(BarideDbContext context, UserManager<AppUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
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

        public async Task<IActionResult> OnPostTransferAsync(Guid cid, List<string> receiverIds, string? note)
        {
            var corresp = await _context.Correspondances.FindAsync(cid);
            if (corresp == null) return NotFound();

            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach (var receiverId in receiverIds)
            {
                _context.Transferts.Add(new Transfert
                {
                    Cid = cid,
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Note = note,
                    DateTransfert = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = cid });
        }

        public async Task<IActionResult> OnPostShareAsync(Guid cid, string? contactsJson)
        {
            var corresp = await _context.Correspondances.FindAsync(cid);
            if (corresp == null) return NotFound();

            if (string.IsNullOrWhiteSpace(contactsJson))
                return RedirectToPage(new { id = cid });

            var contacts = JsonSerializer.Deserialize<List<ShareContactDto>>(contactsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (contacts == null || contacts.Count == 0)
                return RedirectToPage(new { id = cid });

            var viewUrl = $"{Request.Scheme}://{Request.Host}/Correspondances/SharedView/{cid}";
            var subject = $"مراسلة رقم {corresp.Num} - {corresp.Objet}";
            var dateStr = corresp.DateCorresp.ToString("yyyy/MM/dd");
            var body = $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background-color:#f4f6f8;font-family:Arial,Tahoma,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f6f8;padding:30px 0;'>
    <tr><td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);'>
        <!-- Header -->
        <tr>
          <td style='background:linear-gradient(135deg,#1a73e8,#0d47a1);padding:24px 32px;text-align:center;'>
            <h1 style='margin:0;color:#ffffff;font-size:22px;font-weight:bold;'>📬 بريد جامعة سطيف 1</h1>
          </td>
        </tr>
        <!-- Body -->
        <tr>
          <td style='padding:32px;'>
            <h2 style='margin:0 0 20px;color:#1a73e8;font-size:18px;border-bottom:2px solid #e8eaed;padding-bottom:12px;'>
              مراسلة رقم {corresp.Num}
            </h2>
            <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:24px;'>
              <tr>
                <td style='padding:10px 12px;background-color:#f8f9fa;border-right:3px solid #1a73e8;font-size:14px;color:#5f6368;width:120px;'>الموضوع</td>
                <td style='padding:10px 12px;background-color:#f8f9fa;font-size:14px;color:#202124;font-weight:bold;'>{corresp.Objet}</td>
              </tr>
              <tr>
                <td style='padding:10px 12px;font-size:14px;color:#5f6368;border-right:3px solid #1a73e8;width:120px;'>المرسل</td>
                <td style='padding:10px 12px;font-size:14px;color:#202124;'>{corresp.Expediteur}</td>
              </tr>
              <tr>
                <td style='padding:10px 12px;background-color:#f8f9fa;border-right:3px solid #1a73e8;font-size:14px;color:#5f6368;width:120px;'>التاريخ</td>
                <td style='padding:10px 12px;background-color:#f8f9fa;font-size:14px;color:#202124;'>{dateStr}</td>
              </tr>
            </table>
            <table width='100%' cellpadding='0' cellspacing='0'>
              <tr><td align='center'>
                <a href='{viewUrl}' style='display:inline-block;background-color:#1a73e8;color:#ffffff;text-decoration:none;padding:12px 32px;border-radius:6px;font-size:15px;font-weight:bold;'>
                  عرض المراسلة ←
                </a>
              </td></tr>
            </table>
          </td>
        </tr>
        <!-- Footer -->
        <tr>
          <td style='background-color:#f8f9fa;padding:16px 32px;text-align:center;border-top:1px solid #e8eaed;'>
            <p style='margin:0;font-size:12px;color:#9aa0a6;'>
              تم إرسال هذا البريد تلقائيًا من نظام بريد جامعة فرحات عباس سطيف 1
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            var failedEmails = new List<string>();

            foreach (var dto in contacts)
            {
                if (string.IsNullOrWhiteSpace(dto.Email)) continue;

                // Find or create contact
                var existing = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.Email == dto.Email);

                if (existing == null)
                {
                    var newContact = new Contact
                    {
                        Name = dto.Name ?? dto.Email,
                        Email = dto.Email,
                        Phone = dto.Phone ?? ""
                    };
                    _context.Contacts.Add(newContact);
                }

                try
                {
                    await _emailService.SendEmailAsync(dto.Email, dto.Name ?? dto.Email, subject, body);
                }
                catch (Exception ex)
                {
                    failedEmails.Add($"{dto.Email}: {ex.InnerException?.Message ?? ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            if (failedEmails.Count > 0)
            {
                return new JsonResult(new { success = false, errors = failedEmails });
            }

            return new JsonResult(new { success = true });
        }

        private class ShareContactDto
        {
            public string? Name { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
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

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
            var dateArrivStr = corresp.DateArrivDepart.ToString("yyyy/MM/dd");
            var typeLabel = corresp.Type switch
            {
                TypeCorresp.Entrant_Interne => "وارد داخلي",
                TypeCorresp.Entrant_Externe => "وارد خارجي",
                TypeCorresp.Sotant_Interne => "صادر داخلي",
                TypeCorresp.Sortant_Externe => "صادر خارجي",
                TypeCorresp.Divers => "متفرقات",
                _ => ""
            };
            var expedLabel = corresp.Type == TypeCorresp.Entrant_Interne || corresp.Type == TypeCorresp.Entrant_Externe
                ? "المرسل" : "المرسل إليه";
            var observationRow = !string.IsNullOrWhiteSpace(corresp.Observation)
                ? $@"<tr>
                    <td style='padding:14px 20px;font-size:14px;color:#6b7280;border-bottom:1px solid #f0f0f0;width:130px;font-weight:600;'>ملاحظة</td>
                    <td style='padding:14px 20px;font-size:14px;color:#374151;border-bottom:1px solid #f0f0f0;'>{corresp.Observation}</td>
                  </tr>"
                : "";
            var body = $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background-color:#eef2f7;font-family:Tahoma,Arial,sans-serif;-webkit-font-smoothing:antialiased;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#eef2f7;padding:40px 0;'>
    <tr><td align='center'>
      <table width='620' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.07);'>

        <!-- Header -->
        <tr>
          <td style='background:linear-gradient(135deg,#0f4c81 0%,#1a73e8 50%,#4a90d9 100%);padding:36px 40px 32px;text-align:center;'>
            <table cellpadding='0' cellspacing='0' style='margin:0 auto;'>
              <tr>
                <td style='font-size:36px;padding-left:12px;'>🏛️</td>
                <td>
                  <p style='margin:0 0 4px;color:rgba(255,255,255,0.85);font-size:13px;letter-spacing:1px;'>جامعة فرحات عباس سطيف 1</p>
                  <h1 style='margin:0;color:#ffffff;font-size:22px;font-weight:bold;'>نظام تسيير البريد</h1>
                </td>
              </tr>
            </table>
          </td>
        </tr>

        <!-- Badge -->
        <tr>
          <td style='padding:28px 40px 0;text-align:center;'>
            <table cellpadding='0' cellspacing='0' style='margin:0 auto;'>
              <tr>
                <td style='background-color:#eff6ff;border:1px solid #bfdbfe;border-radius:20px;padding:6px 20px;'>
                  <span style='font-size:13px;color:#1d4ed8;font-weight:bold;'>{typeLabel}</span>
                </td>
              </tr>
            </table>
          </td>
        </tr>

        <!-- Title -->
        <tr>
          <td style='padding:20px 40px 8px;text-align:center;'>
            <h2 style='margin:0;color:#1e293b;font-size:20px;font-weight:bold;'>مراسلة رقم {corresp.Num}</h2>
            {(string.IsNullOrWhiteSpace(corresp.NumInterne) ? "" : $"<p style='margin:6px 0 0;font-size:13px;color:#94a3b8;'>الرقم الداخلي: {corresp.NumInterne}</p>")}
          </td>
        </tr>

        <!-- Divider -->
        <tr>
          <td style='padding:0 40px;'>
            <table width='100%' cellpadding='0' cellspacing='0'><tr><td style='border-bottom:2px solid #e5e7eb;height:1px;font-size:0;line-height:0;'>&nbsp;</td></tr></table>
          </td>
        </tr>

        <!-- Details -->
        <tr>
          <td style='padding:24px 40px;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='border:1px solid #e5e7eb;border-radius:12px;overflow:hidden;'>
              <tr style='background-color:#f8fafc;'>
                <td style='padding:14px 20px;font-size:14px;color:#6b7280;border-bottom:1px solid #f0f0f0;width:130px;font-weight:600;'>الموضوع</td>
                <td style='padding:14px 20px;font-size:15px;color:#111827;border-bottom:1px solid #f0f0f0;font-weight:bold;'>{corresp.Objet}</td>
              </tr>
              <tr>
                <td style='padding:14px 20px;font-size:14px;color:#6b7280;border-bottom:1px solid #f0f0f0;width:130px;font-weight:600;'>{expedLabel}</td>
                <td style='padding:14px 20px;font-size:14px;color:#374151;border-bottom:1px solid #f0f0f0;'>{corresp.Expediteur}</td>
              </tr>
              <tr style='background-color:#f8fafc;'>
                <td style='padding:14px 20px;font-size:14px;color:#6b7280;border-bottom:1px solid #f0f0f0;width:130px;font-weight:600;'>تاريخ المراسلة</td>
                <td style='padding:14px 20px;font-size:14px;color:#374151;border-bottom:1px solid #f0f0f0;'>
                  <span style='background-color:#f0fdf4;color:#166534;padding:3px 10px;border-radius:6px;font-size:13px;'>{dateStr}</span>
                </td>
              </tr>
              <tr>
                <td style='padding:14px 20px;font-size:14px;color:#6b7280;border-bottom:1px solid #f0f0f0;width:130px;font-weight:600;'>تاريخ الوصول/الإرسال</td>
                <td style='padding:14px 20px;font-size:14px;color:#374151;border-bottom:1px solid #f0f0f0;'>
                  <span style='background-color:#fefce8;color:#854d0e;padding:3px 10px;border-radius:6px;font-size:13px;'>{dateArrivStr}</span>
                </td>
              </tr>
              {observationRow}
            </table>
          </td>
        </tr>

        <!-- CTA Button -->
        <tr>
          <td style='padding:8px 40px 36px;text-align:center;'>
            <table cellpadding='0' cellspacing='0' style='margin:0 auto;'>
              <tr>
                <td style='background:linear-gradient(135deg,#1a73e8,#1d4ed8);border-radius:10px;box-shadow:0 4px 14px rgba(26,115,232,0.35);'>
                  <a href='{viewUrl}' style='display:inline-block;color:#ffffff;text-decoration:none;padding:14px 44px;font-size:16px;font-weight:bold;letter-spacing:0.5px;'>
                    عرض المراسلة ←
                  </a>
                </td>
              </tr>
            </table>
          </td>
        </tr>

        <!-- Footer -->
        <tr>
          <td style='background-color:#f8fafc;padding:24px 40px;text-align:center;border-top:1px solid #e5e7eb;'>
            <p style='margin:0 0 4px;font-size:12px;color:#9ca3af;'>
              تم إرسال هذا البريد تلقائيًا من نظام تسيير البريد
            </p>
            <p style='margin:0;font-size:11px;color:#d1d5db;'>
              جامعة فرحات عباس سطيف 1 &bull; جميع الحقوق محفوظة
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

using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Services
{
    public interface IEmailTemplateService
    {
        Task<string> BuildShareEmailAsync(Corresp corresp, string viewUrl);
        string GetDefaultTemplate();
    }

    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly BarideDbContext _context;

        public EmailTemplateService(BarideDbContext context)
        {
            _context = context;
        }

        public async Task<string> BuildShareEmailAsync(Corresp corresp, string viewUrl)
        {
            var param = await _context.Parameters
                .FirstOrDefaultAsync(p => p.Key == "EmailTemplate");

            var template = param?.LongValue ?? GetDefaultTemplate();

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

            var body = template
                .Replace("{{Num}}", corresp.Num)
                .Replace("{{NumInterne}}", corresp.NumInterne)
                .Replace("{{Objet}}", corresp.Objet)
                .Replace("{{Expediteur}}", corresp.Expediteur)
                .Replace("{{DateCorresp}}", corresp.DateCorresp.ToString("yyyy/MM/dd"))
                .Replace("{{DateArrivDepart}}", corresp.DateArrivDepart.ToString("yyyy/MM/dd"))
                .Replace("{{Observation}}", corresp.Observation ?? "")
                .Replace("{{ObservationRow}}", observationRow)
                .Replace("{{TypeLabel}}", typeLabel)
                .Replace("{{ExpedLabel}}", expedLabel)
                .Replace("{{ViewUrl}}", viewUrl)
                .Replace("{{NumInterneRow}}", string.IsNullOrWhiteSpace(corresp.NumInterne)
                    ? ""
                    : $"<p style='margin:6px 0 0;font-size:13px;color:#94a3b8;'>الرقم الداخلي: {corresp.NumInterne}</p>");

            return body;
        }

        public string GetDefaultTemplate()
        {
            return @"<!DOCTYPE html>
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
                  <span style='font-size:13px;color:#1d4ed8;font-weight:bold;'>{{TypeLabel}}</span>
                </td>
              </tr>
            </table>
          </td>
        </tr>

        <!-- Title -->
        <tr>
          <td style='padding:20px 40px 8px;text-align:center;'>
            <h2 style='margin:0;color:#1e293b;font-size:20px;font-weight:bold;'>مراسلة رقم {{Num}}</h2>
            {{NumInterneRow}}
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
                <td style='padding:14px 20px;font-size:15px;color:#111827;border-bottom:1px solid #f0f0f0;font-weight:bold;'>{{Objet}}</td>
              </tr>
              <tr>
                <td style='padding:14px 20px;font-size:14px;color:#6b7280;border-bottom:1px solid #f0f0f0;width:130px;font-weight:600;'>{{ExpedLabel}}</td>
                <td style='padding:14px 20px;font-size:14px;color:#374151;border-bottom:1px solid #f0f0f0;'>{{Expediteur}}</td>
              </tr>
              <tr style='background-color:#f8fafc;'>
                <td style='padding:14px 20px;font-size:14px;color:#6b7280;border-bottom:1px solid #f0f0f0;width:130px;font-weight:600;'>تاريخ المراسلة</td>
                <td style='padding:14px 20px;font-size:14px;color:#374151;border-bottom:1px solid #f0f0f0;'>
                  <span style='background-color:#f0fdf4;color:#166534;padding:3px 10px;border-radius:6px;font-size:13px;'>{{DateCorresp}}</span>
                </td>
              </tr>
              <tr>
                <td style='padding:14px 20px;font-size:14px;color:#6b7280;border-bottom:1px solid #f0f0f0;width:130px;font-weight:600;'>تاريخ الوصول/الإرسال</td>
                <td style='padding:14px 20px;font-size:14px;color:#374151;border-bottom:1px solid #f0f0f0;'>
                  <span style='background-color:#fefce8;color:#854d0e;padding:3px 10px;border-radius:6px;font-size:13px;'>{{DateArrivDepart}}</span>
                </td>
              </tr>
              {{ObservationRow}}
            </table>
          </td>
        </tr>

        <!-- CTA Button -->
        <tr>
          <td style='padding:8px 40px 36px;text-align:center;'>
            <table cellpadding='0' cellspacing='0' style='margin:0 auto;'>
              <tr>
                <td style='background:linear-gradient(135deg,#1a73e8,#1d4ed8);border-radius:10px;box-shadow:0 4px 14px rgba(26,115,232,0.35);'>
                  <a href='{{ViewUrl}}' style='display:inline-block;color:#ffffff;text-decoration:none;padding:14px 44px;font-size:16px;font-weight:bold;letter-spacing:0.5px;'>
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
        }
    }
}

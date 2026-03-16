using System.Net;
using System.Net.Mail;

namespace BarideWeb.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var smtp = _config.GetSection("SmtpSettings");
            var host = smtp["Host"]!;
            var port = int.Parse(smtp["Port"]!);
            var username = smtp["Username"]!;
            var password = smtp["Password"]!;
            var fromEmail = smtp["FromEmail"]!;
            var fromName = smtp["FromName"] ?? "BaridUFAS";

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(message);
        }
    }
}

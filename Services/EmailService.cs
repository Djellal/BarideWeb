using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = htmlBody
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}

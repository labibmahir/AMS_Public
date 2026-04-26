using System.Net;
using System.Net.Mail;
using AMS.Domains.Dto;
using Microsoft.Extensions.Options;

namespace AMS.Services.DBService; // Putting it in DBService or a new namespace

public class EmailService
{
    private readonly SmtpSettings _settings;

    public EmailService(IOptions<SmtpSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Email, _settings.Password),
            EnableSsl = _settings.EnableSsl
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.Email),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage);
    }
}

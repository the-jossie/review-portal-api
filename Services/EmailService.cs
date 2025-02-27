using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpSettings = _configuration.GetSection("Smtp");

        var host = smtpSettings["Host"];
        var portString = smtpSettings["Port"];
        var ssl = smtpSettings["EnableSsl"];
        var password = smtpSettings["Password"];
        var fromEmail = smtpSettings["Username"];

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("SMTP username (sender email) is not configured properly.");
        }

        if (string.IsNullOrWhiteSpace(portString))
        {
            throw new InvalidOperationException("SMTP port is not configured properly.");
        }

        if (string.IsNullOrWhiteSpace(ssl))
        {
            throw new InvalidOperationException("EnableSsl flag is not configured properly.");
        }

        var client = new SmtpClient(host)
        {
            Port = int.Parse(portString),
            Credentials = new NetworkCredential(fromEmail, password),
            EnableSsl = bool.Parse(ssl)
        };

        var message = new MailMessage(fromEmail, to, subject, body);
        await client.SendMailAsync(message);
    }
}

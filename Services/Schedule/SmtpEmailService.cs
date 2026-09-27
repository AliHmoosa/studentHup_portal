using System.Net;
using System.Net.Mail;

namespace StudentHub.Services;

public sealed class SmtpEmailService(
    IConfiguration configuration,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlMessage)
    {
        var host =
            configuration["Email:Smtp:Host"];

        var port =
            configuration.GetValue<int>(
                "Email:Smtp:Port");

        var username =
            configuration["Email:Smtp:Username"];

        var password =
            configuration["Email:Smtp:Password"];

        var fromEmail =
            configuration["Email:FromEmail"];

        var fromName =
            configuration["Email:FromName"]
            ?? "StudentHub";

        var enableSsl =
            configuration.GetValue<bool>(
                "Email:Smtp:EnableSsl");

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                "Email SMTP host is not configured.");
        }

        if (port <= 0)
        {
            throw new InvalidOperationException(
                "Email SMTP port is not configured.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException(
                "Email SMTP username is not configured.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Email SMTP password is not configured.");
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException(
                "Email sender address is not configured.");
        }

        using var message = new MailMessage();

        message.From = new MailAddress(
            fromEmail,
            fromName);

        message.To.Add(
            new MailAddress(recipientEmail));

        message.Subject = subject;
        message.Body = htmlMessage;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(
            host,
            port);

        client.EnableSsl = enableSsl;

        client.Credentials =
            new NetworkCredential(
                username,
                password);

        logger.LogInformation(
            "Sending email to {Email} with subject {Subject}.",
            recipientEmail,
            subject);

        await client.SendMailAsync(message);
    }
}
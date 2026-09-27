namespace StudentHub.Services;

public interface IEmailService
{
    Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlMessage);
}
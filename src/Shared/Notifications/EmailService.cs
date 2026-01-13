using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.EventManagement.Entities;
using Shared.UserManagement;

namespace Shared.Notifications;

/// <summary>
/// Email configuration settings
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server hostname
    /// </summary>
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>
    /// SMTP server port
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Whether to use SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// SMTP username for authentication
    /// </summary>
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// SMTP password for authentication
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Default sender email address
    /// </summary>
    public string FromEmail { get; set; } = "events@company.com";

    /// <summary>
    /// Default sender display name
    /// </summary>
    public string FromName { get; set; } = "Event Management System";

    /// <summary>
    /// Base URL for links in emails
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:4200";

    /// <summary>
    /// Enable email sending (false for development/testing)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Email template directory path
    /// </summary>
    public string? TemplateDirectory { get; set; }
}

/// <summary>
/// Email template data for registration notifications
/// </summary>
public class RegistrationEmailData
{
    public string UserName { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string EventDate { get; set; } = string.Empty;
    public string EventTime { get; set; } = string.Empty;
    public string EventLocation { get; set; } = string.Empty;
    public string? EventVenue { get; set; }
    public string RegistrationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? QueuePosition { get; set; }
    public int? EstimatedWaitMinutes { get; set; }
    public string? ConfirmationUrl { get; set; }
    public string? CancellationUrl { get; set; }
    public Dictionary<string, object>? CustomData { get; set; }
}

/// <summary>
/// Email service interface for sending registration-related notifications
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send registration confirmation email
    /// </summary>
    Task<bool> SendRegistrationConfirmationAsync(User user, Event eventDetails, Shared.Registration.Registration registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send registration queued notification email
    /// </summary>
    Task<bool> SendRegistrationQueuedAsync(User user, Event eventDetails, Shared.Registration.Registration registration, int queuePosition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send registration confirmed from queue email
    /// </summary>
    Task<bool> SendRegistrationConfirmedFromQueueAsync(User user, Event eventDetails, Shared.Registration.Registration registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send registration cancelled email
    /// </summary>
    Task<bool> SendRegistrationCancelledAsync(User user, Event eventDetails, Shared.Registration.Registration registration, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send queue position update email
    /// </summary>
    Task<bool> SendQueuePositionUpdateAsync(User user, Event eventDetails, Shared.Registration.Registration registration, int newPosition, int? estimatedWaitMinutes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send custom email notification
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? plainTextBody = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Email service implementation using SMTP
/// </summary>
public class EmailService : IEmailService, IDisposable
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _settings;
    private readonly SmtpClient? _smtpClient;
    private bool _disposed;

    public EmailService(ILogger<EmailService> logger, IOptions<EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        if (_settings.Enabled)
        {
            try
            {
                _smtpClient = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrEmpty(_settings.SmtpUsername) && !string.IsNullOrEmpty(_settings.SmtpPassword))
                {
                    _smtpClient.Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword);
                }

                _logger.LogInformation("Email service initialized with SMTP host {SmtpHost}:{SmtpPort}",
                    _settings.SmtpHost, _settings.SmtpPort);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SMTP client");
                _smtpClient?.Dispose();
                _smtpClient = null;
            }
        }
        else
        {
            _logger.LogInformation("Email service disabled by configuration");
        }
    }

    public async Task<bool> SendRegistrationConfirmationAsync(User user, Event eventDetails, Shared.Registration.Registration registration, CancellationToken cancellationToken = default)
    {
        RegistrationEmailData emailData = CreateRegistrationEmailData(user, eventDetails, registration);
        var subject = $"Registration Confirmed: {eventDetails.Title}";

        var htmlBody = BuildConfirmationEmailHtml(emailData);
        var plainTextBody = BuildConfirmationEmailText(emailData);

        return await SendEmailAsync(user.Email, subject, htmlBody, plainTextBody, cancellationToken);
    }

    public async Task<bool> SendRegistrationQueuedAsync(User user, Event eventDetails, Shared.Registration.Registration registration, int queuePosition, CancellationToken cancellationToken = default)
    {
        RegistrationEmailData emailData = CreateRegistrationEmailData(user, eventDetails, registration);
        emailData.QueuePosition = queuePosition;

        var subject = $"Registration Queued: {eventDetails.Title}";

        var htmlBody = BuildQueuedEmailHtml(emailData);
        var plainTextBody = BuildQueuedEmailText(emailData);

        return await SendEmailAsync(user.Email, subject, htmlBody, plainTextBody, cancellationToken);
    }

    public async Task<bool> SendRegistrationConfirmedFromQueueAsync(User user, Event eventDetails, Shared.Registration.Registration registration, CancellationToken cancellationToken = default)
    {
        RegistrationEmailData emailData = CreateRegistrationEmailData(user, eventDetails, registration);
        var subject = $"Registration Confirmed: {eventDetails.Title} - You're In!";

        var htmlBody = BuildConfirmedFromQueueEmailHtml(emailData);
        var plainTextBody = BuildConfirmedFromQueueEmailText(emailData);

        return await SendEmailAsync(user.Email, subject, htmlBody, plainTextBody, cancellationToken);
    }

    public async Task<bool> SendRegistrationCancelledAsync(User user, Event eventDetails, Shared.Registration.Registration registration, string reason, CancellationToken cancellationToken = default)
    {
        RegistrationEmailData emailData = CreateRegistrationEmailData(user, eventDetails, registration);
        var subject = $"Registration Cancelled: {eventDetails.Title}";

        var htmlBody = BuildCancelledEmailHtml(emailData, reason);
        var plainTextBody = BuildCancelledEmailText(emailData, reason);

        return await SendEmailAsync(user.Email, subject, htmlBody, plainTextBody, cancellationToken);
    }

    public async Task<bool> SendQueuePositionUpdateAsync(User user, Event eventDetails, Shared.Registration.Registration registration, int newPosition, int? estimatedWaitMinutes = null, CancellationToken cancellationToken = default)
    {
        RegistrationEmailData emailData = CreateRegistrationEmailData(user, eventDetails, registration);
        emailData.QueuePosition = newPosition;
        emailData.EstimatedWaitMinutes = estimatedWaitMinutes;

        var subject = $"Queue Update: {eventDetails.Title} - Position #{newPosition}";

        var htmlBody = BuildQueueUpdateEmailHtml(emailData);
        var plainTextBody = BuildQueueUpdateEmailText(emailData);

        return await SendEmailAsync(user.Email, subject, htmlBody, plainTextBody, cancellationToken);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? plainTextBody = null, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email sending disabled, would send to {Email}: {Subject}", to, subject);
            return true;
        }

        if (_smtpClient == null)
        {
            _logger.LogError("SMTP client not initialized, cannot send email to {Email}", to);
            return false;
        }

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                IsBodyHtml = true,
                Body = htmlBody
            };

            mailMessage.To.Add(to);

            if (!string.IsNullOrEmpty(plainTextBody))
            {
                var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, Encoding.UTF8, "text/plain");
                mailMessage.AlternateViews.Add(plainTextView);
            }

            await _smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email}: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", to, subject);
            return false;
        }
    }

    private RegistrationEmailData CreateRegistrationEmailData(User user, Event eventDetails, Shared.Registration.Registration registration)
    {
        return new RegistrationEmailData
        {
            UserName = user.FullName,
            EventTitle = eventDetails.Title,
            EventDate = eventDetails.StartDate.ToString("dddd, MMMM dd, yyyy"),
            EventTime = eventDetails.StartDate.ToString("h:mm tt"),
            EventLocation = "TBA", // eventDetails.VenueName ?? "TBA",
            EventVenue = "TBA", // eventDetails.VenueAddress,
            RegistrationId = registration.Id.ToString(),
            Status = registration.Status.ToString(),
            ConfirmationUrl = $"{_settings.BaseUrl}/registrations/{registration.Id}",
            CancellationUrl = $"{_settings.BaseUrl}/registrations/{registration.Id}/cancel",
            CustomData = registration.RegistrationData
        };
    }

    private string BuildConfirmationEmailHtml(RegistrationEmailData data)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Registration Confirmed</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #007bff; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none; }}
        .event-details {{ background: white; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .button {{ display: inline-block; padding: 12px 24px; background: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .footer {{ background: #6c757d; color: white; padding: 15px; text-align: center; font-size: 0.9em; border-radius: 0 0 8px 8px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🎉 Registration Confirmed!</h1>
    </div>
    <div class='content'>
        <p>Hi {data.UserName},</p>
        <p>Great news! Your registration for <strong>{data.EventTitle}</strong> has been confirmed.</p>

        <div class='event-details'>
            <h3>📅 Event Details</h3>
            <p><strong>Event:</strong> {data.EventTitle}</p>
            <p><strong>Date:</strong> {data.EventDate}</p>
            <p><strong>Time:</strong> {data.EventTime}</p>
            <p><strong>Location:</strong> {data.EventLocation}</p>
            {(string.IsNullOrEmpty(data.EventVenue) ? "" : $"<p><strong>Venue:</strong> {data.EventVenue}</p>")}
        </div>

        <p>Your registration ID is: <strong>{data.RegistrationId}</strong></p>

        <p>
            <a href='{data.ConfirmationUrl}' class='button'>View Registration Details</a>
        </p>

        <p>We look forward to seeing you at the event!</p>

        <p><small>If you need to cancel your registration, you can do so by clicking <a href='{data.CancellationUrl}'>here</a>.</small></p>
    </div>
    <div class='footer'>
        <p>Event Management System | Do not reply to this email</p>
    </div>
</body>
</html>";
    }

    private string BuildConfirmationEmailText(RegistrationEmailData data)
    {
        return $@"Registration Confirmed!

Hi {data.UserName},

Great news! Your registration for {data.EventTitle} has been confirmed.

Event Details:
- Event: {data.EventTitle}
- Date: {data.EventDate}
- Time: {data.EventTime}
- Location: {data.EventLocation}
{(string.IsNullOrEmpty(data.EventVenue) ? "" : $"- Venue: {data.EventVenue}")}

Your registration ID is: {data.RegistrationId}

View registration details: {data.ConfirmationUrl}

We look forward to seeing you at the event!

If you need to cancel your registration, visit: {data.CancellationUrl}

---
Event Management System
Do not reply to this email";
    }

    private string BuildQueuedEmailHtml(RegistrationEmailData data)
    {
        var waitTimeText = data.EstimatedWaitMinutes.HasValue
            ? $"<p><strong>Estimated wait time:</strong> {data.EstimatedWaitMinutes} minutes</p>"
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Registration Queued</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #ffc107; color: #333; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none; }}
        .event-details {{ background: white; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .queue-info {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .button {{ display: inline-block; padding: 12px 24px; background: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .footer {{ background: #6c757d; color: white; padding: 15px; text-align: center; font-size: 0.9em; border-radius: 0 0 8px 8px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>⏳ Registration Queued</h1>
    </div>
    <div class='content'>
        <p>Hi {data.UserName},</p>
        <p>Thank you for your interest in <strong>{data.EventTitle}</strong>! The event is currently at capacity, but you've been added to the queue.</p>

        <div class='queue-info'>
            <h3>📍 Your Queue Status</h3>
            <p><strong>Queue Position:</strong> #{data.QueuePosition}</p>
            {waitTimeText}
            <p>We'll notify you as soon as a spot becomes available!</p>
        </div>

        <div class='event-details'>
            <h3>📅 Event Details</h3>
            <p><strong>Event:</strong> {data.EventTitle}</p>
            <p><strong>Date:</strong> {data.EventDate}</p>
            <p><strong>Time:</strong> {data.EventTime}</p>
            <p><strong>Location:</strong> {data.EventLocation}</p>
            {(string.IsNullOrEmpty(data.EventVenue) ? "" : $"<p><strong>Venue:</strong> {data.EventVenue}</p>")}
        </div>

        <p>Your registration ID is: <strong>{data.RegistrationId}</strong></p>

        <p>
            <a href='{data.ConfirmationUrl}' class='button'>Check Queue Status</a>
        </p>

        <p><small>If you need to cancel your registration, you can do so by clicking <a href='{data.CancellationUrl}'>here</a>.</small></p>
    </div>
    <div class='footer'>
        <p>Event Management System | Do not reply to this email</p>
    </div>
</body>
</html>";
    }

    private string BuildQueuedEmailText(RegistrationEmailData data)
    {
        var waitTimeText = data.EstimatedWaitMinutes.HasValue
            ? $"Estimated wait time: {data.EstimatedWaitMinutes} minutes"
            : "";

        return $@"Registration Queued

Hi {data.UserName},

Thank you for your interest in {data.EventTitle}! The event is currently at capacity, but you've been added to the queue.

Your Queue Status:
- Queue Position: #{data.QueuePosition}
{waitTimeText}

We'll notify you as soon as a spot becomes available!

Event Details:
- Event: {data.EventTitle}
- Date: {data.EventDate}
- Time: {data.EventTime}
- Location: {data.EventLocation}
{(string.IsNullOrEmpty(data.EventVenue) ? "" : $"- Venue: {data.EventVenue}")}

Your registration ID is: {data.RegistrationId}

Check queue status: {data.ConfirmationUrl}

If you need to cancel your registration, visit: {data.CancellationUrl}

---
Event Management System
Do not reply to this email";
    }

    private string BuildConfirmedFromQueueEmailHtml(RegistrationEmailData data)
    {
        return BuildConfirmationEmailHtml(data).Replace("Registration Confirmed!", "You're In! Registration Confirmed from Queue")
            .Replace("Great news! Your registration", "Fantastic news! Your registration from the queue");
    }

    private string BuildConfirmedFromQueueEmailText(RegistrationEmailData data)
    {
        return BuildConfirmationEmailText(data).Replace("Registration Confirmed!", "You're In! Registration Confirmed from Queue")
            .Replace("Great news! Your registration", "Fantastic news! Your registration from the queue");
    }

    private string BuildCancelledEmailHtml(RegistrationEmailData data, string reason)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Registration Cancelled</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none; }}
        .event-details {{ background: white; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .button {{ display: inline-block; padding: 12px 24px; background: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .footer {{ background: #6c757d; color: white; padding: 15px; text-align: center; font-size: 0.9em; border-radius: 0 0 8px 8px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>❌ Registration Cancelled</h1>
    </div>
    <div class='content'>
        <p>Hi {data.UserName},</p>
        <p>Your registration for <strong>{data.EventTitle}</strong> has been cancelled.</p>

        <p><strong>Reason:</strong> {reason}</p>

        <div class='event-details'>
            <h3>📅 Event Details</h3>
            <p><strong>Event:</strong> {data.EventTitle}</p>
            <p><strong>Date:</strong> {data.EventDate}</p>
            <p><strong>Time:</strong> {data.EventTime}</p>
        </div>

        <p>Your registration ID was: <strong>{data.RegistrationId}</strong></p>

        <p>If you'd like to register again, please visit our events page.</p>

        <p>We hope to see you at future events!</p>
    </div>
    <div class='footer'>
        <p>Event Management System | Do not reply to this email</p>
    </div>
</body>
</html>";
    }

    private string BuildCancelledEmailText(RegistrationEmailData data, string reason)
    {
        return $@"Registration Cancelled

Hi {data.UserName},

Your registration for {data.EventTitle} has been cancelled.

Reason: {reason}

Event Details:
- Event: {data.EventTitle}
- Date: {data.EventDate}
- Time: {data.EventTime}

Your registration ID was: {data.RegistrationId}

If you'd like to register again, please visit our events page.

We hope to see you at future events!

---
Event Management System
Do not reply to this email";
    }

    private string BuildQueueUpdateEmailHtml(RegistrationEmailData data)
    {
        var waitTimeText = data.EstimatedWaitMinutes.HasValue
            ? $"<p><strong>Estimated wait time:</strong> {data.EstimatedWaitMinutes} minutes</p>"
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Queue Position Update</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #17a2b8; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none; }}
        .queue-info {{ background: #d1ecf1; border: 1px solid #bee5eb; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .button {{ display: inline-block; padding: 12px 24px; background: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .footer {{ background: #6c757d; color: white; padding: 15px; text-align: center; font-size: 0.9em; border-radius: 0 0 8px 8px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>📈 Queue Update</h1>
    </div>
    <div class='content'>
        <p>Hi {data.UserName},</p>
        <p>Good news! Your queue position for <strong>{data.EventTitle}</strong> has been updated.</p>

        <div class='queue-info'>
            <h3>📍 Your New Queue Status</h3>
            <p><strong>Queue Position:</strong> #{data.QueuePosition}</p>
            {waitTimeText}
            <p>You're getting closer! We'll notify you as soon as a spot becomes available.</p>
        </div>

        <p>
            <a href='{data.ConfirmationUrl}' class='button'>Check Current Status</a>
        </p>

        <p><small>If you need to cancel your registration, you can do so by clicking <a href='{data.CancellationUrl}'>here</a>.</small></p>
    </div>
    <div class='footer'>
        <p>Event Management System | Do not reply to this email</p>
    </div>
</body>
</html>";
    }

    private string BuildQueueUpdateEmailText(RegistrationEmailData data)
    {
        var waitTimeText = data.EstimatedWaitMinutes.HasValue
            ? $"Estimated wait time: {data.EstimatedWaitMinutes} minutes"
            : "";

        return $@"Queue Update

Hi {data.UserName},

Good news! Your queue position for {data.EventTitle} has been updated.

Your New Queue Status:
- Queue Position: #{data.QueuePosition}
{waitTimeText}

You're getting closer! We'll notify you as soon as a spot becomes available.

Check current status: {data.ConfirmationUrl}

If you need to cancel your registration, visit: {data.CancellationUrl}

---
Event Management System
Do not reply to this email";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _smtpClient?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Extension methods for registering email services
/// </summary>
public static class EmailServiceExtensions
{
    /// <summary>
    /// Register email services with dependency injection
    /// </summary>
    public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(options =>
            configuration.GetSection(EmailSettings.SectionName).Bind(options));
        services.AddTransient<IEmailService, EmailService>();

        return services;
    }
}

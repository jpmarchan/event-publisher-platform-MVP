namespace NotificationService.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string From { get; init; }

    public int TimeoutSeconds { get; init; } = 10;
}

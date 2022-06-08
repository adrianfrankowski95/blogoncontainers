using NodaTime;

namespace Blog.Services.Emailing.API.Events;

public record UserRegisteredEvent(
    string Username,
    string EmailAddress,
    string CallbackUrl,
    Instant UrlValidUntil);

using NodaTime;

namespace Blog.Services.Identity.Domain.AggregatesModel.UserAggregate;

public class LoginService
{
    public LoginResult LogIn(
        User user,
        EmailAddress providedEmailAddress,
        NonEmptyString providedPassword,
        PasswordHasher passwordHasher,
        Instant now)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        if (user.IsLockedOut(now))
            return LoginResult.Fail(LoginErrorCode.AccountLockedOut);

        if (!(user.EmailAddress?.Equals(providedEmailAddress) ?? false))
            return LoginResult.Fail(LoginErrorCode.InvalidEmail);

        if (!passwordHasher.VerifyPasswordHash(providedPassword, user.PasswordHash))
            return LoginResult.Fail(LoginErrorCode.InvalidPassword);

        if (user.IsSuspended(now))
            return LoginResult.Fail(LoginErrorCode.AccountSuspended);

        if (!user.HasConfirmedEmailAddress)
            return LoginResult.Fail(LoginErrorCode.UnconfirmedEmail);

        if (!user.HasActivePassword)
            return LoginResult.Fail(LoginErrorCode.InactivePassword);

        return LoginResult.Success;
    }
}
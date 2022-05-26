using Blog.Services.Identity.API.Core;
using Blog.Services.Identity.API.Models;
using Microsoft.Extensions.Options;

namespace Blog.Services.Identity.API.Services;

public class LoginService<TUser> : ILoginService<TUser> where TUser : User
{
    private readonly UserManager<TUser> _userManager;
    private readonly IOptionsMonitor<SecurityOptions> _options;

    public LoginService(UserManager<TUser> userManager, IOptionsMonitor<SecurityOptions> options)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<IdentityResult> LoginAsync(string email, string password, out TUser? user)
    {
        user = null;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Task.FromResult(IdentityResult.Fail(IdentityError.InvalidCredentials));

        return ProcessLoginAsync(email, password, user);
    }

    private async Task<IdentityResult> ProcessLoginAsync(string email, string password, TUser? user)
    {
        user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);

        if (user is null)
            return IdentityResult.Fail(IdentityError.InvalidCredentials);

        var passwordVerificationResult = _userManager.VerifyPassword(user, password);

        if (passwordVerificationResult is PasswordVerificationResult.Success ||
            passwordVerificationResult is PasswordVerificationResult.SuccessNeedsRehash)
        {
            var userValidationResult = await _userManager.ValidateUserAsync(user).ConfigureAwait(false);
            if (!userValidationResult.Succeeded)
                return userValidationResult;

            var passwordValidationResult = await _userManager.ValidatePasswordAsync(password).ConfigureAwait(false);
            if (!passwordValidationResult.Succeeded)
                return passwordValidationResult;

            if (passwordVerificationResult is PasswordVerificationResult.SuccessNeedsRehash)
            {
                var passwordHashUpdateResult = await _userManager.UpdatePasswordHashAsync(user, password, false).ConfigureAwait(false);
                if (!passwordHashUpdateResult.Succeeded)
                    return passwordHashUpdateResult;
            }

            return await _userManager.SuccessfulLoginAttemptAsync(user).ConfigureAwait(false);
        }
        else if (passwordVerificationResult is PasswordVerificationResult.Fail)
        {
            if (_options.CurrentValue.EnableAccountLockout)
                await _userManager.FailedLoginAttemptAsync(user).ConfigureAwait(false);

            return IdentityResult.Fail(IdentityError.InvalidCredentials);
        }
        else
            throw new NotSupportedException("Unhandled password verification result");
    }
}

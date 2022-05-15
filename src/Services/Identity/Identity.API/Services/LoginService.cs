using Blog.Services.Identity.API.Core;
using Blog.Services.Identity.API.Models;
using NodaTime;

namespace Blog.Services.Identity.API.Services;

public class LoginService<TUser> : ILoginService where TUser : User
{
    private readonly UserManager<TUser> _userManager;

    public LoginService(UserManager<TUser> userManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    public async Task<IdentityResult> LoginAsync(HttpContext context, string email, string password, bool rememberMe)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return IdentityResult.Fail(IdentityError.InvalidCredentials);

        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);

        if (user is null)
            return IdentityResult.Fail(IdentityError.InvalidCredentials);

        var userValidationResult = await _userManager.ValidateUserAsync(user);
        if (!userValidationResult.Succeeded)
            return userValidationResult;

        var passwordVerificationResult = _userManager.VerifyPassword(user, password);

        if (passwordVerificationResult is PasswordVerificationResult.SuccessNeedsRehash)
            await _userManager.UpdatePasswordHashAsync(user, password, false);

        if (passwordVerificationResult is PasswordVerificationResult.SuccessNeedsRehash ||
            passwordVerificationResult is PasswordVerificationResult.Success)
        {
            var passwordValidationResult = await _userManager.ValidatePasswordAsync(password);
            if (!passwordValidationResult.Succeeded)
                return passwordValidationResult;

            await _userManager.UpdateLastLoginAndClearAttemptsAsync(user).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        await _userManager.AddFailedLoginAttemptAsync(user).ConfigureAwait(false);
        return IdentityResult.Fail(IdentityError.InvalidCredentials);
    }
}

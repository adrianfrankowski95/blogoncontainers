// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Blog.Services.Identity.API.Core;
using Blog.Services.Identity.API.Models;
using Blog.Services.Identity.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Blog.Services.Identity.API.Pages.Account.Manage;

public class UpdateEmailModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly IOptionsMonitor<EmailOptions> _emailOptions;
    private readonly ISysTime _sysTime;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UpdateEmailModel> _logger;

    public UpdateEmailModel(
        UserManager<User> userManager,
        IOptionsMonitor<EmailOptions> emailOptions,
        ISysTime sysTime,
        IEmailSender emailSender,
        ILogger<UpdateEmailModel> logger)
    {
        _userManager = userManager;
        _emailOptions = emailOptions;
        _sysTime = sysTime;
        _emailSender = emailSender;
        _logger = logger;
    }

    public string Email { get; set; }
    public string ReturnUrl { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "New email")]
        public string NewEmail { get; set; }
    }

    private void LoadEmail()
    {
        Email = (string)TempData.Peek("Email");
    }

    private void LoadInput()
    {
        LoadEmail();
        Input = new InputModel
        {
            NewEmail = Email
        };
    }

    private void SetNewEmail()
    {
        TempData["Email"] = Input.NewEmail;
    }

    public IActionResult OnGet(string returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        LoadInput();

        if (string.IsNullOrWhiteSpace(Email))
        {
            return NotFound($"Unable to load user email.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        LoadEmail();

        if (string.IsNullOrWhiteSpace(Email))
        {
            return NotFound($"Unable to load user email.");
        }

        var user = await _userManager.FindByEmailAsync(Email);
        if (user is null)
            return NotFound($"Unable to load user with email '{Email}'.");

        if (!ModelState.IsValid)
        {
            LoadInput();
            return Page();
        }

        if (!string.Equals(Input.NewEmail, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var result = await _userManager.UpdateEmailAsync(user, Input.NewEmail);
            if (!result.Succeeded)
            {
                //Reveal details about account state only if provided credentials are valid
                if (!result.Errors.Contains(CredentialsError.InvalidCredentials))
                {
                    if (result.Errors.Contains(UserStateValidationError.AccountSuspended))
                    {
                        _logger.LogWarning("User account suspended.");
                        return RedirectToPage("./Suspension", new { suspendedUntil = user.SuspendedUntil.Value });
                    }
                    else if (result.Errors.Contains(UserStateValidationError.AccountLockedOut))
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }
                    else if (result.Errors.Contains(EmailValidationError.EmailDuplicated))
                    {
                        ModelState.AddModelError(string.Empty, "The Email is already in use.");
                        return Page();
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid email change attempt.");
                return Page();
            }

            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(user.EmailConfirmationCode.ToString()));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, email = Input.NewEmail, code = code },
                protocol: Request.Scheme);
            await _emailSender.SendEmailAsync(
                Input.NewEmail,
                "Confirm your email",
                $"Please confirm your email by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>." +
                $"<br><br>This link will expire at {_sysTime.Now.Plus(Duration.FromTimeSpan(_emailOptions.CurrentValue.EmailConfirmationCodeValidityPeriod)).ToString("dddd, dd mmmm yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo)}.");

            StatusMessage = "Confirmation link to change email sent. Please check your email.";
            SetNewEmail();
            return LocalRedirect(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Your email is unchanged.");
        return RedirectToPage();
    }
}

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using StudentHub.Constants;
using StudentHub.Data;
using StudentHub.Models;
using StudentHub.Services;


namespace StudentHub.Controllers;

public sealed class AccountController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    IEmailService emailService) : Controller
{
    // =========================================================
    // LOGIN
    // =========================================================

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(
                "Index",
                "Dashboard");
        }

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        model.ReturnUrl =
            returnUrl ?? model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user =
            await users.FindByEmailAsync(
                model.Email);

        if (user is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(model);
        }

        var result =
            await signIn.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return RedirectToLocal(
                model.ReturnUrl,
                "Dashboard",
                "Index");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Your account is temporarily locked. Please try again later.");

            return View(model);
        }

        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(
                string.Empty,
                "This account is not currently allowed to sign in.");

            return View(model);
        }

        ModelState.AddModelError(
            string.Empty,
            "Invalid email or password.");

        return View(model);
    }


    // =========================================================
    // GOOGLE LOGIN
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(
        string provider,
        string? returnUrl = null)
    {
        var redirectUrl =
            Url.Action(
                nameof(ExternalLoginCallback),
                "Account",
                new { returnUrl });

        var properties =
            signIn.ConfigureExternalAuthenticationProperties(
                provider,
                redirectUrl);

        return Challenge(
            properties,
            provider);
    }


    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(
        string? returnUrl = null,
        string? remoteError = null)
    {
        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            ModelState.AddModelError(
                string.Empty,
                $"External authentication error: {remoteError}");

            return View(
                "Login",
                new LoginViewModel
                {
                    ReturnUrl = returnUrl
                });
        }

        var info =
            await signIn.GetExternalLoginInfoAsync();

        if (info is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Unable to load information from Google.");

            return View(
                "Login",
                new LoginViewModel
                {
                    ReturnUrl = returnUrl
                });
        }

        var result =
            await signIn.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: true,
                bypassTwoFactor: true);

        if (result.Succeeded)
        {
            return RedirectToLocal(
                returnUrl,
                "Dashboard",
                "Index");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Your account is temporarily locked.");

            return View(
                "Login",
                new LoginViewModel
                {
                    ReturnUrl = returnUrl
                });
        }

        var email =
            info.Principal.FindFirstValue(
                ClaimTypes.Email);

        var displayName =
            info.Principal.FindFirstValue(
                ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(
                string.Empty,
                "Google did not provide an email address.");

            return View(
                "Login",
                new LoginViewModel
                {
                    ReturnUrl = returnUrl
                });
        }

        var existingUser =
            await users.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            var addLoginResult =
                await users.AddLoginAsync(
                    existingUser,
                    info);

            if (!addLoginResult.Succeeded)
            {
                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(
                    "Login",
                    new LoginViewModel
                    {
                        ReturnUrl = returnUrl
                    });
            }

            await signIn.SignInAsync(
                existingUser,
                isPersistent: true);

            return RedirectToLocal(
                returnUrl,
                "Dashboard",
                "Index");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName =
                string.IsNullOrWhiteSpace(displayName)
                    ? email.Split('@')[0]
                    : displayName,

            EmailConfirmed = true,
            IsProfileComplete = false
        };

        var createResult =
            await users.CreateAsync(user);

        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(
                "Login",
                new LoginViewModel
                {
                    ReturnUrl = returnUrl
                });
        }

        var loginResult =
            await users.AddLoginAsync(
                user,
                info);

        if (!loginResult.Succeeded)
        {
            await users.DeleteAsync(user);

            foreach (var error in loginResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(
                "Login",
                new LoginViewModel
                {
                    ReturnUrl = returnUrl
                });
        }

        await users.AddToRoleAsync(
            user,
            StudentHubRoles.Student);

        await signIn.SignInAsync(
            user,
            isPersistent: true);

        return RedirectToLocal(
            returnUrl,
            "Dashboard",
            "Index");
    }


    // =========================================================
    // REGISTER
    // =========================================================
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            EmailConfirmed = false,
            IsProfileComplete = false
        };

        var result = await users.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await users.AddToRoleAsync(user, StudentHubRoles.Student);

        await signIn.SignInAsync(user, isPersistent: false);

        return RedirectToAction("Index", "Dashboard");
    }


    // =========================================================
    // FORGOT PASSWORD
    // =========================================================

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(
                "Index",
                "Dashboard");
        }

        return View(
            new ForgotPasswordViewModel());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user =
            await users.FindByEmailAsync(
                model.Email);

        /*
         * Always show the same confirmation page.
         *
         * This prevents revealing whether an email
         * address belongs to a StudentHub account.
         */
        if (user is null ||
            !(await users.IsEmailConfirmedAsync(user)))
        {
            return RedirectToAction(
                nameof(ForgotPasswordConfirmation));
        }

        var token =
            await users.GeneratePasswordResetTokenAsync(
                user);

        var encodedToken =
            WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(token));

        var resetUrl =
            Url.Action(
                nameof(ResetPassword),
                "Account",
                new
                {
                    email = user.Email,
                    token = encodedToken
                },
                Request.Scheme);

        if (string.IsNullOrWhiteSpace(resetUrl))
        {
            ModelState.AddModelError(
                string.Empty,
                "Unable to generate the password reset link.");

            return View(model);
        }

        var htmlMessage = BuildPasswordResetEmail(
            user.DisplayName,
            resetUrl);

        await emailService.SendAsync(
            user.Email!,
            "Reset your StudentHub password",
            htmlMessage);

        return RedirectToAction(
            nameof(ForgotPasswordConfirmation));
    }


    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }


    // =========================================================
    // RESET PASSWORD
    // =========================================================

    [HttpGet]
    public IActionResult ResetPassword(
        string? email,
        string? token)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(
                nameof(ForgotPassword));
        }

        return View(
            new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user =
            await users.FindByEmailAsync(
                model.Email);

        if (user is null)
        {
            return RedirectToAction(
                nameof(ResetPasswordConfirmation));
        }

        string decodedToken;

        try
        {
            decodedToken =
                Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(
                        model.Token));
        }
        catch
        {
            ModelState.AddModelError(
                string.Empty,
                "The password reset link is invalid or has expired.");

            return View(model);
        }

        var result =
            await users.ResetPasswordAsync(
                user,
                decodedToken,
                model.Password);

        if (result.Succeeded)
        {
            return RedirectToAction(
                nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }

        return View(model);
    }


    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }


    // =========================================================
    // LOGOUT
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();

        return RedirectToAction(
            nameof(Login));
    }


    // =========================================================
    // ACCESS DENIED
    // =========================================================

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private IActionResult RedirectToLocal(
        string? returnUrl,
        string controller,
        string action)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(
            action,
            controller)!;
    }


    private static string BuildPasswordResetEmail(
        string displayName,
        string resetUrl)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <body style="margin:0;padding:0;background:#f5faf8;font-family:Arial,sans-serif;color:#173b3a;">

            <div style="max-width:620px;margin:40px auto;background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e3eeeb;">

                <div style="padding:30px;background:#075c56;color:#ffffff;">
                    <h1 style="margin:0;font-size:28px;">
                        Student<span style="color:#4ee2ae;">Hub</span>
                    </h1>
                </div>

                <div style="padding:38px;">

                    <h2 style="margin-top:0;">
                        Reset your password
                    </h2>

                    <p>
                        Hello {System.Net.WebUtility.HtmlEncode(displayName)},
                    </p>

                    <p>
                        We received a request to reset your StudentHub password.
                    </p>

                    <p>
                        Click the button below to create a new password.
                    </p>

                    <p style="margin:30px 0;">
                        <a href="{System.Net.WebUtility.HtmlEncode(resetUrl)}"
                           style="display:inline-block;padding:14px 28px;background:#078875;color:#ffffff;text-decoration:none;border-radius:8px;font-weight:bold;">
                            Reset Password
                        </a>
                    </p>

                    <p style="font-size:13px;color:#71838a;">
                        If you did not request this, you can safely ignore this email.
                    </p>

                    <p style="font-size:12px;color:#89979c;">
                        For security, this link will expire according to your
                        StudentHub password-reset policy.
                    </p>

                </div>

                <div style="padding:20px 38px;background:#f7faf9;color:#829094;font-size:12px;">
                    © 2026 StudentHub. All rights reserved.
                </div>

            </div>

        </body>
        </html>
        """;
    }
}
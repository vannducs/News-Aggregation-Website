using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models.ViewModels;
using NewsAggregator.Services;

namespace NewsAggregator.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;

    public AccountController(AppDbContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedInput = model.UserNameOrEmail.Trim();
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => !u.IsDeleted && (u.UserName == normalizedInput || u.Email == normalizedInput));

        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Tai khoan khong ton tai hoac da bi khoa.");
            return View(model);
        }

        var verification = _passwordService.VerifyPassword(user.Password, model.Password);
        if (!verification.IsVerified)
        {
            ModelState.AddModelError(string.Empty, "Ten dang nhap hoac mat khau khong dung.");
            return View(model);
        }

        if (verification.NeedsRehash)
        {
            user.Password = _passwordService.HashPassword(model.Password);
        }

        user.LastLoginAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await SignInUserAsync(user);

        return RedirectToLocal(model.ReturnUrl, user.Role);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Profile));
        }

        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        await ValidateRegisterModelAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new Models.AppUser
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            UserName = model.UserName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            Password = _passwordService.HashPassword(model.Password),
            Role = Models.UserRoles.Member,
            IsActive = true,
            CreatedAt = DateTime.Now,
            LastLoginAt = DateTime.Now
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();
        await SignInUserAsync(user);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            Address = user.Address,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            UserName = user.UserName,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (await _context.AppUsers.AnyAsync(u => !u.IsDeleted && u.Email == model.Email && u.AppUserID != user.AppUserID))
        {
            ModelState.AddModelError(nameof(model.Email), "Email da ton tai.");
        }

        if (!ModelState.IsValid)
        {
            model.UserName = user.UserName;
            model.Role = user.Role;
            model.CreatedAt = user.CreatedAt;
            model.LastLoginAt = user.LastLoginAt;
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        user.DateOfBirth = model.DateOfBirth;
        user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        user.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim();

        await _context.SaveChangesAsync();
        await SignInUserAsync(user);
        TempData["ProfileMessage"] = "Da cap nhat ho so.";

        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var verification = _passwordService.VerifyPassword(user.Password, model.CurrentPassword);
        if (!verification.IsVerified)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Mat khau hien tai khong dung.");
            return View(model);
        }

        user.Password = _passwordService.HashPassword(model.NewPassword);
        await _context.SaveChangesAsync();
        await SignInUserAsync(user);
        TempData["PasswordMessage"] = "Da doi mat khau thanh cong.";

        return RedirectToAction(nameof(ChangePassword));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInUserAsync(Models.AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.AppUserID.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("FullName", user.FullName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
    }

    private async Task ValidateRegisterModelAsync(RegisterViewModel model)
    {
        if (await _context.AppUsers.AnyAsync(u => !u.IsDeleted && u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Email da ton tai.");
        }

        if (await _context.AppUsers.AnyAsync(u => !u.IsDeleted && u.UserName == model.UserName))
        {
            ModelState.AddModelError(nameof(model.UserName), "Ten dang nhap da ton tai.");
        }
    }

    private async Task<Models.AppUser?> GetCurrentUserAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _context.AppUsers.FirstOrDefaultAsync(u => u.AppUserID == userId && !u.IsDeleted);
    }

    private IActionResult RedirectToLocal(string? returnUrl, string? role = null)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (string.Equals(role, Models.UserRoles.Admin, StringComparison.Ordinal) ||
            string.Equals(role, Models.UserRoles.Editor, StringComparison.Ordinal) ||
            string.Equals(role, Models.UserRoles.Moderator, StringComparison.Ordinal) ||
            User.IsInRole(Models.UserRoles.Admin) ||
            User.IsInRole(Models.UserRoles.Editor) ||
            User.IsInRole(Models.UserRoles.Moderator))
        {
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        return RedirectToAction("Index", "Home");
    }
}

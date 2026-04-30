using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Models.ViewModels;
using NewsAggregator.Services;

namespace NewsAggregator.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = UserRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordService _passwordService;

        public UsersController(AppDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<IActionResult> Index(string? keyword, string? role, bool? isActive, bool includeDeleted = false, int page = 1, int pageSize = 10)
        {
            var query = _context.AppUsers.AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(u => !u.IsDeleted);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(u =>
                    u.FullName.Contains(normalizedKeyword) ||
                    u.Email.Contains(normalizedKeyword) ||
                    u.UserName.Contains(normalizedKeyword));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            page = Math.Max(1, page);
            pageSize = pageSize is < 5 or > 50 ? 10 : pageSize;

            var totalRecords = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
            var allUsers = await _context.AppUsers.ToListAsync();

            var viewModel = new AdminUserIndexViewModel
            {
                Keyword = keyword,
                Role = role,
                IsActive = isActive,
                IncludeDeleted = includeDeleted,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Users = users,
                TotalUsers = allUsers.Count(u => !u.IsDeleted),
                ActiveUsers = allUsers.Count(u => u.IsActive && !u.IsDeleted),
                InactiveUsers = allUsers.Count(u => !u.IsActive && !u.IsDeleted),
                DeletedUsers = allUsers.Count(u => u.IsDeleted)
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("Form", new AdminUserFormViewModel
            {
                IsActive = true,
                Role = UserRoles.Editor
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminUserFormViewModel model)
        {
            await ValidateUserFormAsync(model);

            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            var user = new AppUser
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                UserName = model.UserName.Trim(),
                Password = _passwordService.HashPassword(model.Password),
                Role = model.Role,
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                DateOfBirth = model.DateOfBirth,
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
                Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim(),
                AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Da tao nguoi dung moi thanh cong.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.AppUserID == id && !u.IsDeleted);
            if (user is null)
            {
                return NotFound();
            }

            return View("Form", new AdminUserFormViewModel
            {
                AppUserID = user.AppUserID,
                FullName = user.FullName,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                IsActive = user.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminUserFormViewModel model)
        {
            if (id != model.AppUserID)
            {
                return NotFound();
            }

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.AppUserID == id && !u.IsDeleted);
            if (user is null)
            {
                return NotFound();
            }

            await ValidateUserFormAsync(model);

            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.UserName = model.UserName.Trim();
            user.Role = model.Role;
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            user.DateOfBirth = model.DateOfBirth;
            user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            user.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
            user.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim();
            user.IsActive = model.IsActive;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.Password = _passwordService.HashPassword(model.Password);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Da cap nhat thong tin nguoi dung.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.AppUserID == id && !u.IsDeleted);
            if (user is not null)
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
                user.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Da an nguoi dung khoi he thong.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.AppUserID == id && u.IsDeleted);
            if (user is not null)
            {
                user.IsDeleted = false;
                user.DeletedAt = null;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Da khoi phuc nguoi dung.";
            }

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.AppUserID == id && !u.IsDeleted);
            if (user is not null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = user.IsActive
                    ? "Da mo khoa tai khoan."
                    : "Da khoa tai khoan.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.AppUserID == id && !u.IsDeleted);
            if (user is not null)
            {
                user.Password = _passwordService.HashPassword("123456");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Da reset mat khau cho {user.UserName} ve 123456.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateUserFormAsync(AdminUserFormViewModel model)
        {
            if (!AdminUserFormViewModel.AvailableRoles.Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Vai tro khong hop le.");
            }

            if (await _context.AppUsers.AnyAsync(u => !u.IsDeleted && u.Email == model.Email && u.AppUserID != model.AppUserID))
            {
                ModelState.AddModelError(nameof(model.Email), "Email da ton tai.");
            }

            if (await _context.AppUsers.AnyAsync(u => !u.IsDeleted && u.UserName == model.UserName && u.AppUserID != model.AppUserID))
            {
                ModelState.AddModelError(nameof(model.UserName), "Ten dang nhap da ton tai.");
            }

            if (!model.AppUserID.HasValue && string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Mat khau la bat buoc khi tao moi.");
            }
        }
    }
}

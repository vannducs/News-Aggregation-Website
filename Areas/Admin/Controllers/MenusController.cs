using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Models.ViewModels;

namespace NewsAggregator.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = NewsAggregator.Models.UserRoles.AdminOrEditor)]
    public class MenusController : Controller
    {
        private readonly AppDbContext _context;

        public MenusController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var menus = await _context.Menus.OrderBy(m => m.Position).ThenBy(m => m.MenuOrder).ToListAsync();
            return View(menus);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View("Form", await BuildFormViewModelAsync(new AdminMenuFormViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminMenuFormViewModel model)
        {
            await ValidateMenuFormAsync(model);

            if (string.IsNullOrWhiteSpace(model.Link) &&
                !string.IsNullOrWhiteSpace(model.ControllerName) &&
                !string.IsNullOrWhiteSpace(model.ActionName))
            {
                model.Link = $"/{model.ControllerName}/{model.ActionName}";
            }

            if (!ModelState.IsValid)
            {
                return View("Form", await BuildFormViewModelAsync(model));
            }

            var menu = new Menu
            {
                MenuName = model.MenuName.Trim(),
                IsActive = model.IsActive,
                ControllerName = string.IsNullOrWhiteSpace(model.ControllerName) ? null : model.ControllerName.Trim(),
                ActionName = string.IsNullOrWhiteSpace(model.ActionName) ? null : model.ActionName.Trim(),
                Levels = model.ParentID == 0 ? 1 : 2,
                ParentID = model.ParentID,
                Link = string.IsNullOrWhiteSpace(model.Link) ? null : model.Link.Trim(),
                MenuOrder = model.MenuOrder,
                Position = model.Position
            };

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu is null)
            {
                return NotFound();
            }

            return View("Form", await BuildFormViewModelAsync(new AdminMenuFormViewModel
            {
                MenuID = menu.MenuID,
                MenuName = menu.MenuName,
                IsActive = menu.IsActive,
                ControllerName = menu.ControllerName,
                ActionName = menu.ActionName,
                Levels = menu.Levels,
                ParentID = menu.ParentID,
                Link = menu.Link,
                MenuOrder = menu.MenuOrder,
                Position = menu.Position
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminMenuFormViewModel model)
        {
            if (id != model.MenuID)
            {
                return NotFound();
            }

            await ValidateMenuFormAsync(model);

            if (string.IsNullOrWhiteSpace(model.Link) &&
                !string.IsNullOrWhiteSpace(model.ControllerName) &&
                !string.IsNullOrWhiteSpace(model.ActionName))
            {
                model.Link = $"/{model.ControllerName}/{model.ActionName}";
            }

            if (!ModelState.IsValid)
            {
                return View("Form", await BuildFormViewModelAsync(model));
            }

            var menu = await _context.Menus.FindAsync(id);
            if (menu is null)
            {
                return NotFound();
            }

            menu.MenuName = model.MenuName.Trim();
            menu.IsActive = model.IsActive;
            menu.ControllerName = string.IsNullOrWhiteSpace(model.ControllerName) ? null : model.ControllerName.Trim();
            menu.ActionName = string.IsNullOrWhiteSpace(model.ActionName) ? null : model.ActionName.Trim();
            menu.ParentID = model.ParentID;
            menu.Levels = model.ParentID == 0 ? 1 : 2;
            menu.Link = string.IsNullOrWhiteSpace(model.Link) ? null : model.Link.Trim();
            menu.MenuOrder = model.MenuOrder;
            menu.Position = model.Position;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu is not null)
            {
                menu.IsActive = !menu.IsActive;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu is not null)
            {
                var childMenus = await _context.Menus.Where(m => m.ParentID == id).ToListAsync();
                foreach (var child in childMenus)
                {
                    child.ParentID = 0;
                    child.Levels = 1;
                }

                _context.Menus.Remove(menu);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveUp(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu is null)
            {
                return RedirectToAction(nameof(Index));
            }

            var previousMenu = await _context.Menus
                .Where(m => m.ParentID == menu.ParentID && m.Position == menu.Position && m.MenuOrder < menu.MenuOrder)
                .OrderByDescending(m => m.MenuOrder)
                .FirstOrDefaultAsync();

            if (previousMenu is not null)
            {
                (menu.MenuOrder, previousMenu.MenuOrder) = (previousMenu.MenuOrder, menu.MenuOrder);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveDown(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu is null)
            {
                return RedirectToAction(nameof(Index));
            }

            var nextMenu = await _context.Menus
                .Where(m => m.ParentID == menu.ParentID && m.Position == menu.Position && m.MenuOrder > menu.MenuOrder)
                .OrderBy(m => m.MenuOrder)
                .FirstOrDefaultAsync();

            if (nextMenu is not null)
            {
                (menu.MenuOrder, nextMenu.MenuOrder) = (nextMenu.MenuOrder, menu.MenuOrder);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<AdminMenuFormViewModel> BuildFormViewModelAsync(AdminMenuFormViewModel model)
        {
            var parentMenus = await _context.Menus
                .Where(m => m.ParentID == 0 && m.MenuID != model.MenuID)
                .OrderBy(m => m.MenuOrder)
                .ToListAsync();

            model.ParentOptions = new List<SelectListItem>
            {
                new("Khong co menu cha", "0")
            }
            .Concat(parentMenus.Select(m => new SelectListItem(m.MenuName, m.MenuID.ToString())))
            .ToList();

            return model;
        }

        private async Task ValidateMenuFormAsync(AdminMenuFormViewModel model)
        {
            if (model.MenuID != 0 && model.ParentID == model.MenuID)
            {
                ModelState.AddModelError(nameof(model.ParentID), "Khong the chon chinh menu nay lam menu cha.");
            }

            if (model.ParentID != 0)
            {
                var parentMenu = await _context.Menus.FirstOrDefaultAsync(m => m.MenuID == model.ParentID);
                if (parentMenu is null)
                {
                    ModelState.AddModelError(nameof(model.ParentID), "Menu cha khong ton tai.");
                }
                else if (parentMenu.ParentID != 0)
                {
                    ModelState.AddModelError(nameof(model.ParentID), "He thong hien chi ho tro toi da 2 tang menu.");
                }
            }

            if (string.IsNullOrWhiteSpace(model.Link) &&
                (string.IsNullOrWhiteSpace(model.ControllerName) || string.IsNullOrWhiteSpace(model.ActionName)))
            {
                ModelState.AddModelError(nameof(model.Link), "Can nhap Link hoac day du Controller/Action.");
            }
        }
    }
}

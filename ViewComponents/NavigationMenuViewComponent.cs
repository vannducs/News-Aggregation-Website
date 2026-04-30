using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Models.ViewModels;

namespace NewsAggregator.ViewComponents;

public class NavigationMenuViewComponent : ViewComponent
{
    private readonly AppDbContext _context;

    public NavigationMenuViewComponent(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync(int position = 1)
    {
        var menus = await _context.Menus
            .Where(m => m.IsActive && m.Position == position)
            .OrderBy(m => m.ParentID)
            .ThenBy(m => m.MenuOrder)
            .ToListAsync();

        var menuTree = BuildTree(menus, 0);
        return View(menuTree);
    }

    private IReadOnlyCollection<MenuNodeViewModel> BuildTree(IReadOnlyCollection<Menu> menus, int parentId)
    {
        return menus
            .Where(m => m.ParentID == parentId)
            .OrderBy(m => m.MenuOrder)
            .Select(m => new MenuNodeViewModel
            {
                MenuID = m.MenuID,
                MenuName = m.MenuName,
                MenuOrder = m.MenuOrder,
                Url = ResolveUrl(m),
                Children = BuildTree(menus, m.MenuID)
            })
            .ToList();
    }

    private string ResolveUrl(Menu menu)
    {
        if (!string.IsNullOrWhiteSpace(menu.Link))
        {
            return menu.Link;
        }

        if (!string.IsNullOrWhiteSpace(menu.ControllerName) && !string.IsNullOrWhiteSpace(menu.ActionName))
        {
            return Url.Action(menu.ActionName, menu.ControllerName) ?? "#";
        }

        return "#";
    }
}

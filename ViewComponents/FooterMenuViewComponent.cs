using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Models.ViewModels;

namespace NewsAggregator.ViewComponents;

public class FooterMenuViewComponent : ViewComponent
{
    private readonly AppDbContext _context;

    public FooterMenuViewComponent(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var menus = await _context.Menus
            .Where(m => m.IsActive && m.Position == 2)
            .OrderBy(m => m.ParentID)
            .ThenBy(m => m.MenuOrder)
            .ToListAsync();

        var rootMenus = menus
            .Where(m => m.ParentID == 0)
            .OrderBy(m => m.MenuOrder)
            .Select(m => new MenuNodeViewModel
            {
                MenuID = m.MenuID,
                MenuName = m.MenuName,
                MenuOrder = m.MenuOrder,
                Url = ResolveUrl(m),
                Children = menus
                    .Where(child => child.ParentID == m.MenuID)
                    .OrderBy(child => child.MenuOrder)
                    .Select(child => new MenuNodeViewModel
                    {
                        MenuID = child.MenuID,
                        MenuName = child.MenuName,
                        MenuOrder = child.MenuOrder,
                        Url = ResolveUrl(child)
                    })
                    .ToList()
            })
            .ToList();

        return View(rootMenus);
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

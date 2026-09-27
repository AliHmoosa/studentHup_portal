using Microsoft.AspNetCore.Mvc;
using StudentHub.Services;
using Microsoft.AspNetCore.Authorization;

namespace StudentHub.Controllers;

[Authorize]
public sealed class DashboardController(IDashboardService dashboardService) : Controller
{
    public IActionResult Index() => View(dashboardService.GetDashboard());
}

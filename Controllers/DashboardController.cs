using Microsoft.AspNetCore.Mvc;
using StudentHub.Services;

namespace StudentHub.Controllers;

public sealed class DashboardController(IDashboardService dashboardService) : Controller
{
    public IActionResult Index() => View(dashboardService.GetDashboard());
}

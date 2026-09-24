using Microsoft.AspNetCore.Mvc;
using StudentHub.Services;

namespace StudentHub.Controllers;

public sealed class AssignmentsController(IDashboardService dashboardService) : Controller
{
    public IActionResult Index() => View(dashboardService.GetAssignments());

    [HttpGet]
    public IActionResult Create() => View();
}

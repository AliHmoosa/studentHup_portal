using Microsoft.AspNetCore.Mvc;
using StudentHub.Services;
using Microsoft.AspNetCore.Authorization;


namespace StudentHub.Controllers;

[Authorize]
public sealed class AssignmentsController(IDashboardService dashboardService) : Controller
{
    public IActionResult Index() => View(dashboardService.GetAssignments());

    [HttpGet]
    public IActionResult Create() => View();
}

using Microsoft.AspNetCore.Mvc;

namespace StudentHub.Controllers;

public sealed class ScheduleController : Controller
{
    public IActionResult Weekly() => View();
    public IActionResult Calendar() => View();
}

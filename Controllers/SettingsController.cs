using Microsoft.AspNetCore.Mvc;

namespace StudentHub.Controllers;

public sealed class SettingsController : Controller
{
    public IActionResult Index() => View("ComingSoon", "Settings");
}

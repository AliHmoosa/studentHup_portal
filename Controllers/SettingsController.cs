using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace StudentHub.Controllers;

[Authorize]
public sealed class SettingsController : Controller
{
    public IActionResult Index() => View("ComingSoon", "Settings");
}

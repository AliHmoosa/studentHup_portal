using Microsoft.AspNetCore.Mvc;

namespace StudentHub.Controllers;

public sealed class LibraryController : Controller
{
    public IActionResult Index() => View("ComingSoon", "StudentHub Library");
    public IActionResult Documents() => View("ComingSoon", "My Documents");
}

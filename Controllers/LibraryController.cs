using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace StudentHub.Controllers;

[Authorize]
public sealed class LibraryController : Controller
{
    public IActionResult Index() => View("ComingSoon", "StudentHub Library");
    public IActionResult Documents() => View("ComingSoon", "My Documents");
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Services.Schedule;
using System.Security.Claims;

namespace StudentHub.Controllers;

[Authorize]
public sealed class ScheduleController : Controller
{
    private readonly IScheduleService _scheduleService;

    public ScheduleController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<IActionResult> Weekly(
        DateTime? date,
        CancellationToken cancellationToken)
    {
        var studentId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Challenge();
        }

        var selectedDate =
            date?.Date ?? DateTime.Today;

        var model =
            await _scheduleService.GetWeeklyScheduleAsync(
                studentId,
                selectedDate,
                cancellationToken);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Calendar(
        CancellationToken cancellationToken)
    {
        return View();
    }
}
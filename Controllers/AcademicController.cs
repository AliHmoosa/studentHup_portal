using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Models;
using StudentHub.Services;

namespace StudentHub.Controllers;

[Authorize]
public sealed class AcademicController(
    ICourseService courseService,
    IAcademicTermService academicTermService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Courses(
        string? search = null,
        int? termId = null,
        string? status = null)
    {
        var ownerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Challenge();
        }

        var model =
            await courseService.GetCoursesAsync(
                ownerId,
                search,
                termId,
                status);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var ownerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Challenge();
        }

        var model = new CreateCourseViewModel();

        ViewBag.Terms =
            await courseService.GetTermsAsync(ownerId);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateCourseViewModel model)
    {
        var ownerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Challenge();
        }

        foreach (var schedule in model.Schedules)
        {
            if (schedule.DaysOfWeek.Count == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please select at least one class day for each schedule.");

                break;
            }

            if (schedule.StartTime.HasValue &&
                schedule.EndTime.HasValue &&
                schedule.EndTime.Value <= schedule.StartTime.Value)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The class end time must be after the start time.");

                break;
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Terms =
                await courseService.GetTermsAsync(ownerId);

            return View(model);
        }

        await courseService.CreateCourseAsync(
            ownerId,
            model);

        return RedirectToAction(
            nameof(Courses));
    }

    [HttpGet]
    public IActionResult Exams()
        => View("ComingSoon", "Exams");

    [HttpGet]
    public IActionResult Gpa()
        => View("ComingSoon", "GPA Calculator");

    [HttpGet]
    public IActionResult DegreeTracker()
        => View("ComingSoon", "Degree Tracker");

    [HttpGet]
    public IActionResult Grades()
        => View("ComingSoon", "Grades & Academic Performance");

    //adding terms 
    [HttpGet]
    public async Task<IActionResult> Terms()
    {
        var ownerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Challenge();
        }

        var terms =
            await academicTermService.GetTermsAsync(
                ownerId);

        return View(
            new AcademicTermsPageViewModel
            {
                Terms = terms
            });
    }

    [HttpGet]
    public IActionResult CreateTerm()
    {
        return View(
            new CreateAcademicTermViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTerm(
        CreateAcademicTermViewModel model)
    {
        var ownerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Challenge();
        }

        if (model.StartsOn.HasValue &&
            model.EndsOn.HasValue &&
            model.EndsOn.Value.Date <
            model.StartsOn.Value.Date)
        {
            ModelState.AddModelError(
                nameof(model.EndsOn),
                "End date must be after the start date.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await academicTermService.CreateTermAsync(
            ownerId,
            model);

        return RedirectToAction(
            nameof(Terms));
    }
}
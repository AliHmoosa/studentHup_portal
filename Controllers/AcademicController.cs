using Microsoft.AspNetCore.Mvc;

namespace StudentHub.Controllers;

public sealed class AcademicController : Controller
{
    public IActionResult Courses() => View("ComingSoon", "My Courses");
    public IActionResult Exams() => View("ComingSoon", "Exams");
    public IActionResult Gpa() => View("ComingSoon", "GPA Calculator");
    public IActionResult DegreeTracker() => View("ComingSoon", "Degree Tracker");
    public IActionResult Grades() => View("ComingSoon", "Grades & Academic Performance");
}

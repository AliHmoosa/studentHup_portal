using StudentHub.Models;

namespace StudentHub.Services;

public interface ICourseService
{
    Task<CoursesPageViewModel> GetCoursesAsync(
        string ownerId,
        string? search = null,
        int? termId = null,
        string? status = null);

    Task<IReadOnlyList<CourseTermOption>> GetTermsAsync(
        string ownerId);

    Task<bool> CreateCourseAsync(
        string ownerId,
        CreateCourseViewModel model);
}
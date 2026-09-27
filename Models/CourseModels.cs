using System.ComponentModel.DataAnnotations;

namespace StudentHub.Models;

public sealed class CourseListItem
{
    public int Id { get; init; }

    public int TermId { get; init; }

    public string TermName { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public decimal CreditHours { get; init; }

    public string Status { get; init; } = string.Empty;

    public int ProgressPercent { get; init; }
}

public sealed class CoursesPageViewModel
{
    public IReadOnlyList<CourseListItem> Courses { get; init; } = [];

    public IReadOnlyList<CourseTermOption> Terms { get; init; } = [];

    public int TotalCourses { get; init; }

    public int InProgressCount { get; init; }

    public int CompletedCount { get; init; }

    public int UpcomingCount { get; init; }

    public string? Search { get; init; }

    public string? TermFilter { get; init; }

    public string? StatusFilter { get; init; }
}

public sealed class CourseTermOption
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class CreateCourseViewModel
{
    [Required]
    [StringLength(180)]
    [Display(Name = "Course Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Display(Name = "Course Code")]
    public string Code { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Range(typeof(decimal), "0.5", "100")]
    [Display(Name = "Credit Hours")]
    public decimal CreditHours { get; set; }

    [StringLength(180)]
    [Display(Name = "Instructor")]
    public string? Instructor { get; set; }

    [StringLength(80)]
    [Display(Name = "Category")]
    public string? Category { get; set; }

    [Required]
    [Display(Name = "Academic Term")]
    public int? TermId { get; set; }

    public List<CourseScheduleInput> Schedules { get; set; } =
        [new CourseScheduleInput()];
}

public sealed class CourseScheduleInput
{
    [Range(0, 6)]
    [Display(Name = "Day")]
    public int DayOfWeek { get; set; }

    [Required]
    [Display(Name = "Start Time")]
    public TimeSpan? StartTime { get; set; }

    [Required]
    [Display(Name = "End Time")]
    public TimeSpan? EndTime { get; set; }

    [StringLength(200)]
    [Display(Name = "Location")]
    public string? Location { get; set; }
}
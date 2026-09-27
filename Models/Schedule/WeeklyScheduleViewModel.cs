namespace StudentHub.Models.Schedule;

public sealed class WeeklyScheduleViewModel
{
    public string SemesterName { get; set; } = string.Empty;

    public DateTime SemesterStartDate { get; set; }

    public DateTime SemesterEndDate { get; set; }

    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public DateTime Today { get; set; }

    public IReadOnlyList<ScheduleEntry> Entries { get; set; }
        = Array.Empty<ScheduleEntry>();

    public IReadOnlyList<ScheduleEntry> TodayEntries { get; set; }
        = Array.Empty<ScheduleEntry>();

    public IReadOnlyList<ScheduleCourseSummary> Courses { get; set; }
        = Array.Empty<ScheduleCourseSummary>();

    public IReadOnlyList<string> Conflicts { get; set; }
        = Array.Empty<string>();

    public double WeeklyHours =>
        Entries
            .GroupBy(x => x.Id)
            .SelectMany(x => x)
            .GroupBy(x => new
            {
                x.CourseCode,
                x.DayOfWeek,
                x.StartTime,
                x.EndTime
            })
            .Sum(x => x.First().DurationHours);

    public int TotalCourses =>
        Courses.Count;

    public bool HasConflicts =>
        Conflicts.Count > 0;

    public string WeekRangeDisplay =>
        $"{WeekStartDate:MMM d} – {WeekEndDate:MMM d, yyyy}";
}


public sealed class ScheduleCourseSummary
{
    public string CourseCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string Color { get; set; } = "blue";

    public IReadOnlyList<string> Days { get; set; }
        = Array.Empty<string>();

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public string? Room { get; set; }

    public double WeeklyHours { get; set; }

    public string DaysDisplay =>
        string.Join(", ", Days);

    public string TimeDisplay =>
        $"{DateTime.Today.Add(StartTime):h:mm tt} - {DateTime.Today.Add(EndTime):h:mm tt}";
}
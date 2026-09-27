namespace StudentHub.Models.Schedule;

public sealed class ScheduleEntry
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public int DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public string? Room { get; set; }

    public string SemesterName { get; set; } = string.Empty;

    public DateTime SemesterStartDate { get; set; }

    public DateTime SemesterEndDate { get; set; }

    public string Color { get; set; } = "blue";

    public string DayName =>
        ((System.DayOfWeek)DayOfWeek).ToString();

    public string StartTimeDisplay =>
        DateTime.Today.Add(StartTime).ToString("h:mm tt");

    public string EndTimeDisplay =>
        DateTime.Today.Add(EndTime).ToString("h:mm tt");

    public string TimeDisplay =>
        $"{StartTimeDisplay} - {EndTimeDisplay}";

    public double DurationHours =>
        (EndTime - StartTime).TotalHours;
}
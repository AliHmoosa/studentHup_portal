namespace StudentHub.Models;

public sealed class DashboardViewModel
{
    public string StudentName { get; init; } = "Sara Ahmed";
    public string Programme { get; init; } = "Doctor of Medicine";
    public decimal CurrentGpa { get; init; } = 3.62m;
    public int CompletedCredits { get; init; } = 96;
    public int RequiredCredits { get; init; } = 132;
    public IReadOnlyList<ClassItem> TodayClasses { get; init; } = [];
    public IReadOnlyList<AssignmentItem> Assignments { get; init; } = [];
    public IReadOnlyList<ExamItem> Exams { get; init; } = [];
    public IReadOnlyList<NotificationItem> Notifications { get; init; } = [];
}

public sealed record ClassItem(string Time, string Name, string Code, string Room, string Color);
public sealed record AssignmentItem(string Name, string Course, string Due, string Priority, string Color, string Status = "Not Started");
public sealed record ExamItem(string Name, string Course, string Date, string Countdown);
public sealed record NotificationItem(string Title, string Message, string Time, string Color);

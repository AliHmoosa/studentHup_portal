using StudentHub.Models;

namespace StudentHub.Services;

/// <summary>Temporary presentation data until the SQL Server data layer is configured.</summary>
public sealed class DemoDashboardService : IDashboardService
{
    private static readonly IReadOnlyList<AssignmentItem> Assignments =
    [
        new("Clinical Case Reflection", "Family Medicine", "Due Sep 18, 11:59 PM", "High", "coral"),
        new("Community Health Report", "Public Health", "Due Sep 22, 11:59 PM", "Medium", "amber"),
        new("Evidence Review", "Medical Research", "Due Sep 28, 11:59 PM", "Low", "blue"),
        new("Anatomy Lab Portfolio", "Anatomy", "Completed Sep 6", "Medium", "green", "Completed")
    ];

    public DashboardViewModel GetDashboard() => new()
    {
        TodayClasses =
        [
            new("08:00", "Internal Medicine", "MED 410", "Building A · Room 101", "green"),
            new("10:00", "Community Health", "PHE 301", "Building B · Room 203", "amber"),
            new("01:00", "Medical Research", "RES 220", "Building A · Room 105", "blue")
        ],
        Assignments = Assignments,
        Exams =
        [
            new("Midterm Exam", "Internal Medicine", "Oct 5, 2026", "In 20 days"),
            new("Clinical Skills OSCE", "Clinical Skills", "Oct 12, 2026", "In 27 days")
        ],
        Notifications =
        [
            new("Assignment due soon", "Clinical Case Reflection is due in 3 days.", "2h ago", "coral"),
            new("Exam reminder", "Internal Medicine Midterm is in 20 days.", "5h ago", "blue"),
            new("Schedule updated", "Community Health room changed to B-203.", "1d ago", "green")
        ]
    };

    public IReadOnlyList<AssignmentItem> GetAssignments() => Assignments;
}

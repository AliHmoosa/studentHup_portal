using StudentHub.Models;

namespace StudentHub.Services;

public interface IDashboardService
{
    DashboardViewModel GetDashboard();
    IReadOnlyList<AssignmentItem> GetAssignments();
}

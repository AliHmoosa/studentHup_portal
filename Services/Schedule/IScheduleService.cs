using StudentHub.Models.Schedule;

namespace StudentHub.Services.Schedule;

public interface IScheduleService
{
    Task<WeeklyScheduleViewModel> GetWeeklyScheduleAsync(
        string studentId,
        DateTime weekDate,
        CancellationToken cancellationToken = default);
}
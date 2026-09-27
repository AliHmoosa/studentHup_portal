using Microsoft.Data.SqlClient;
using StudentHub.Models.Schedule;
using System.Data;

namespace StudentHub.Services.Schedule;

public sealed class SqlScheduleService : IScheduleService
{
    private readonly string _connectionString;

    public SqlScheduleService(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("StudentHubDb")
            ?? throw new InvalidOperationException(
                "The 'StudentHubDb' connection string was not configured.");
    }

    public async Task<WeeklyScheduleViewModel> GetWeeklyScheduleAsync(
        string studentId,
        DateTime weekDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            throw new ArgumentException(
                "A valid student ID is required.",
                nameof(studentId));
        }

        var weekStart = StartOfWeek(weekDate, DayOfWeek.Sunday);
        var weekEnd = weekStart.AddDays(6);

        var entries = new List<ScheduleEntry>();

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                Id,
                StudentId,
                CourseCode,
                CourseName,
                DayOfWeek,
                StartTime,
                EndTime,
                Room,
                SemesterName,
                SemesterStartDate,
                SemesterEndDate,
                Color
            FROM dbo.StudentScheduleEntries
            WHERE StudentId = @StudentId
              AND SemesterStartDate <= @WeekEnd
              AND SemesterEndDate >= @WeekStart
            ORDER BY DayOfWeek, StartTime, CourseCode;
            """;

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter("@StudentId", SqlDbType.NVarChar, 450)
            {
                Value = studentId
            });

        command.Parameters.Add(
            new SqlParameter("@WeekStart", SqlDbType.Date)
            {
                Value = weekStart.Date
            });

        command.Parameters.Add(
            new SqlParameter("@WeekEnd", SqlDbType.Date)
            {
                Value = weekEnd.Date
            });

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new ScheduleEntry
            {
                Id = reader.GetInt32(0),
                StudentId = reader.GetString(1),
                CourseCode = reader.GetString(2),
                CourseName = reader.GetString(3),
                DayOfWeek = reader.GetByte(4),
                StartTime = reader.GetTimeSpan(5),
                EndTime = reader.GetTimeSpan(6),
                Room = reader.IsDBNull(7)
                    ? null
                    : reader.GetString(7),
                SemesterName = reader.GetString(8),
                SemesterStartDate = reader.GetDateTime(9),
                SemesterEndDate = reader.GetDateTime(10),
                Color = reader.GetString(11)
            });
        }

        var semester =
            entries
                .OrderBy(x => x.SemesterStartDate)
                .FirstOrDefault();

        var today = DateTime.Today;

        var todayEntries =
            entries
                .Where(x =>
                    x.DayOfWeek == (int)today.DayOfWeek &&
                    today >= x.SemesterStartDate.Date &&
                    today <= x.SemesterEndDate.Date)
                .OrderBy(x => x.StartTime)
                .ToList();

        var courses =
            entries
                .GroupBy(x => new
                {
                    x.CourseCode,
                    x.CourseName
                })
                .Select(group =>
                {
                    var first = group
                        .OrderBy(x => x.StartTime)
                        .First();

                    return new ScheduleCourseSummary
                    {
                        CourseCode = group.Key.CourseCode,
                        CourseName = group.Key.CourseName,
                        Color = first.Color,
                        Days = group
                            .Select(x =>
                                ((DayOfWeek)x.DayOfWeek).ToString()[..3])
                            .Distinct()
                            .ToList(),
                        StartTime = first.StartTime,
                        EndTime = first.EndTime,
                        Room = first.Room,
                        WeeklyHours = group.Sum(x => x.DurationHours)
                    };
                })
                .OrderBy(x => x.StartTime)
                .ToList();

        var conflicts = FindConflicts(entries);

        return new WeeklyScheduleViewModel
        {
            SemesterName =
                semester?.SemesterName ?? "Fall 2026",

            SemesterStartDate =
                semester?.SemesterStartDate
                ?? new DateTime(2026, 8, 24),

            SemesterEndDate =
                semester?.SemesterEndDate
                ?? new DateTime(2026, 12, 18),

            WeekStartDate = weekStart,

            WeekEndDate = weekEnd,

            Today = today,

            Entries = entries,

            TodayEntries = todayEntries,

            Courses = courses,

            Conflicts = conflicts
        };
    }

    private static DateTime StartOfWeek(
        DateTime date,
        DayOfWeek startOfWeek)
    {
        var difference =
            (7 + (date.DayOfWeek - startOfWeek)) % 7;

        return date.Date.AddDays(-difference);
    }

    private static IReadOnlyList<string> FindConflicts(
        IReadOnlyList<ScheduleEntry> entries)
    {
        var conflicts = new List<string>();

        foreach (var dayGroup in entries.GroupBy(x => x.DayOfWeek))
        {
            var ordered =
                dayGroup
                    .OrderBy(x => x.StartTime)
                    .ThenBy(x => x.EndTime)
                    .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                for (var j = i + 1; j < ordered.Count; j++)
                {
                    var first = ordered[i];
                    var second = ordered[j];

                    if (second.StartTime >= first.EndTime)
                    {
                        break;
                    }

                    conflicts.Add(
                        $"{first.CourseCode} and {second.CourseCode} overlap on " +
                        $"{((DayOfWeek)dayGroup.Key)}.");
                }
            }
        }

        return conflicts
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
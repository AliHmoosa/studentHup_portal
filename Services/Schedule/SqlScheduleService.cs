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

        var weekStart =
            StartOfWeek(
                weekDate.Date,
                DayOfWeek.Sunday);

        var weekEnd =
            weekStart.AddDays(6);

        var today =
            DateTime.Today;

        var entries =
            new List<ScheduleEntry>();

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(
            cancellationToken);


        /*
         * Get the student's actual course schedule.
         *
         * Data source:
         *
         * AcademicTerms
         *      ↓
         * Courses
         *      ↓
         * CourseSchedules
         */

        const string scheduleSql = """
            SELECT
                cs.Id,
                c.Code,
                c.Name,
                cs.DayOfWeek,
                cs.StartTime,
                cs.EndTime,
                cs.Location,
                t.Name,
                t.StartsOn,
                t.EndsOn,
                c.Id
            FROM CourseSchedules cs

            INNER JOIN Courses c
                ON c.Id = cs.CourseId
                AND c.OwnerId = cs.OwnerId

            INNER JOIN AcademicTerms t
                ON t.Id = c.TermId
                AND t.OwnerId = c.OwnerId

            WHERE cs.OwnerId = @OwnerId

              AND
              (
                    t.StartsOn IS NULL
                    OR t.EndsOn IS NULL
                    OR
                    (
                        t.StartsOn <= @WeekEnd
                        AND t.EndsOn >= @WeekStart
                    )
              )

            ORDER BY
                cs.DayOfWeek,
                cs.StartTime,
                c.Code;
            """;

        await using var command =
            new SqlCommand(
                scheduleSql,
                connection);

        command.Parameters.Add(
            new SqlParameter(
                "@OwnerId",
                SqlDbType.NVarChar,
                450)
            {
                Value = studentId
            });

        command.Parameters.Add(
            new SqlParameter(
                "@WeekStart",
                SqlDbType.Date)
            {
                Value = weekStart.Date
            });

        command.Parameters.Add(
            new SqlParameter(
                "@WeekEnd",
                SqlDbType.Date)
            {
                Value = weekEnd.Date
            });

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (await reader.ReadAsync(
            cancellationToken))
        {
            var semesterStartDate =
                reader.IsDBNull(8)
                    ? weekStart.Date
                    : reader.GetDateTime(8).Date;

            var semesterEndDate =
                reader.IsDBNull(9)
                    ? weekEnd.Date
                    : reader.GetDateTime(9).Date;

            var courseId =
                reader.GetInt32(10);

            entries.Add(
                new ScheduleEntry
                {
                    Id =
                        reader.GetInt32(0),

                    StudentId =
                        studentId,

                    CourseCode =
                        reader.GetString(1),

                    CourseName =
                        reader.GetString(2),

                    DayOfWeek =
                        reader.GetInt32(3),

                    StartTime =
                        reader.GetTimeSpan(4),

                    EndTime =
                        reader.GetTimeSpan(5),

                    Room =
                        reader.IsDBNull(6)
                            ? null
                            : reader.GetString(6),

                    SemesterName =
                        reader.GetString(7),

                    SemesterStartDate =
                        semesterStartDate,

                    SemesterEndDate =
                        semesterEndDate,

                    Color =
                        GetCourseColor(courseId)
                });
        }

        await reader.DisposeAsync();


        /*
         * Today's classes.
         */

        var todayEntries =
            entries
                .Where(x =>
                    x.DayOfWeek ==
                    (int)today.DayOfWeek

                    &&

                    today >=
                    x.SemesterStartDate.Date

                    &&

                    today <=
                    x.SemesterEndDate.Date)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.CourseCode)
                .ToList();


        /*
         * Course summaries.
         *
         * Multiple CourseSchedules belonging to
         * the same course are grouped together.
         */

        var courses =
            entries
                .GroupBy(x => new
                {
                    x.CourseCode,
                    x.CourseName
                })
                .Select(group =>
                {
                    var first =
                        group
                            .OrderBy(x => x.StartTime)
                            .First();

                    return new ScheduleCourseSummary
                    {
                        CourseCode =
                            group.Key.CourseCode,

                        CourseName =
                            group.Key.CourseName,

                        Color =
                            first.Color,

                        Days =
                            group
                                .OrderBy(x => x.DayOfWeek)
                                .Select(x =>
                                    ((DayOfWeek)x.DayOfWeek)
                                        .ToString()[..3])
                                .Distinct()
                                .ToList(),

                        StartTime =
                            first.StartTime,

                        EndTime =
                            first.EndTime,

                        Room =
                            first.Room,

                        WeeklyHours =
                            group.Sum(
                                x => x.DurationHours)
                    };
                })
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.CourseCode)
                .ToList();


        /*
         * Detect overlapping classes.
         */

        var conflicts =
            FindConflicts(entries);


        /*
         * Determine the semester represented
         * by the selected week.
         */

        var semesterEntry =
            entries
                .OrderBy(x => x.SemesterStartDate)
                .FirstOrDefault();


        var semesterName =
            semesterEntry?.SemesterName
            ?? "No scheduled term";

        var semesterStart =
            semesterEntry?.SemesterStartDate
            ?? weekStart;

        var semesterEnd =
            semesterEntry?.SemesterEndDate
            ?? weekEnd;


        return new WeeklyScheduleViewModel
        {
            SemesterName =
                semesterName,

            SemesterStartDate =
                semesterStart,

            SemesterEndDate =
                semesterEnd,

            WeekStartDate =
                weekStart,

            WeekEndDate =
                weekEnd,

            Today =
                today,

            Entries =
                entries,

            TodayEntries =
                todayEntries,

            Courses =
                courses,

            Conflicts =
                conflicts
        };
    }


    private static DateTime StartOfWeek(
        DateTime date,
        DayOfWeek startOfWeek)
    {
        var difference =
            (7 +
             (date.DayOfWeek - startOfWeek))
            % 7;

        return date
            .Date
            .AddDays(-difference);
    }


    private static IReadOnlyList<string> FindConflicts(
        IReadOnlyList<ScheduleEntry> entries)
    {
        var conflicts =
            new List<string>();

        foreach (
            var dayGroup
            in entries.GroupBy(x => x.DayOfWeek))
        {
            var ordered =
                dayGroup
                    .OrderBy(x => x.StartTime)
                    .ThenBy(x => x.EndTime)
                    .ToList();

            for (
                var i = 0;
                i < ordered.Count;
                i++)
            {
                for (
                    var j = i + 1;
                    j < ordered.Count;
                    j++)
                {
                    var first =
                        ordered[i];

                    var second =
                        ordered[j];

                    if (
                        second.StartTime
                        >=
                        first.EndTime)
                    {
                        break;
                    }

                    conflicts.Add(
                        $"{first.CourseCode} and " +
                        $"{second.CourseCode} overlap on " +
                        $"{((DayOfWeek)dayGroup.Key)}.");
                }
            }
        }

        return conflicts
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }


    /*
     * The CSS already defines these names:
     *
     * blue
     * green
     * amber
     * purple
     * red
     */

    private static string GetCourseColor(
        int courseId)
    {
        return (courseId % 5) switch
        {
            0 => "blue",
            1 => "green",
            2 => "amber",
            3 => "purple",
            _ => "red"
        };
    }
}
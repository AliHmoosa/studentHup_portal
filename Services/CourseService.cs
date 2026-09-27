using System.Data;
using Microsoft.EntityFrameworkCore;
using StudentHub.Data;
using StudentHub.Models;

namespace StudentHub.Services;

public sealed class CourseService(
    StudentHubDbContext db) : ICourseService
{
    public async Task<CoursesPageViewModel> GetCoursesAsync(
        string ownerId,
        string? search = null,
        int? termId = null,
        string? status = null)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return new CoursesPageViewModel();
        }

        var connection = db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                c.Id,
                c.TermId,
                ISNULL(t.Name, '') AS TermName,
                c.Code,
                c.Name,
                c.CreditHours,
                t.StartsOn,
                t.EndsOn
            FROM Courses c
            LEFT JOIN AcademicTerms t
                ON t.Id = c.TermId
                AND t.OwnerId = c.OwnerId
            WHERE c.OwnerId = @OwnerId
              AND (
                    @Search = ''
                    OR c.Code LIKE '%' + @Search + '%'
                    OR c.Name LIKE '%' + @Search + '%'
                  )
              AND (
                    @TermId IS NULL
                    OR c.TermId = @TermId
                  )
            ORDER BY
                CASE
                    WHEN t.StartsOn IS NULL THEN 1
                    ELSE 0
                END,
                t.StartsOn,
                c.Code;
            """;

        AddParameter(command, "@OwnerId", ownerId);
        AddParameter(command, "@Search", search?.Trim() ?? string.Empty);
        AddParameter(command, "@TermId", termId);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var courses = new List<CourseListItem>();

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var startsOn = reader.IsDBNull(6)
                ? (DateTime?)null
                : reader.GetDateTime(6);

            var endsOn = reader.IsDBNull(7)
                ? (DateTime?)null
                : reader.GetDateTime(7);

            var courseStatus = GetStatus(
                startsOn,
                endsOn);

            if (!string.IsNullOrWhiteSpace(status) &&
                !courseStatus.Equals(
                    status,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            courses.Add(new CourseListItem
            {
                Id = reader.GetInt32(0),
                TermId = reader.GetInt32(1),
                TermName = reader.GetString(2),
                Code = reader.GetString(3),
                Name = reader.GetString(4),
                CreditHours = reader.GetDecimal(5),
                Status = courseStatus,
                ProgressPercent = GetProgress(courseStatus)
            });
        }

        var terms = await GetTermsAsync(ownerId);

        return new CoursesPageViewModel
        {
            Courses = courses,
            Terms = terms,
            TotalCourses = courses.Count,
            InProgressCount = courses.Count(x =>
                x.Status == "In Progress"),
            CompletedCount = courses.Count(x =>
                x.Status == "Completed"),
            UpcomingCount = courses.Count(x =>
                x.Status == "Upcoming"),
            Search = search,
            TermFilter = termId?.ToString(),
            StatusFilter = status
        };
    }

    public async Task<IReadOnlyList<CourseTermOption>> GetTermsAsync(
        string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return [];
        }

        var connection = db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                Name
            FROM AcademicTerms
            WHERE OwnerId = @OwnerId
            ORDER BY
                CASE
                    WHEN StartsOn IS NULL THEN 1
                    ELSE 0
                END,
                StartsOn,
                Name;
            """;

        AddParameter(command, "@OwnerId", ownerId);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var terms = new List<CourseTermOption>();

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            terms.Add(new CourseTermOption
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return terms;
    }

    public async Task<bool> CreateCourseAsync(
        string ownerId,
        CreateCourseViewModel model)
    {
        if (string.IsNullOrWhiteSpace(ownerId) ||
            model.TermId is null)
        {
            return false;
        }

        var connection = db.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            await using var courseCommand =
                connection.CreateCommand();

            courseCommand.Transaction = transaction;

            courseCommand.CommandText = """
                INSERT INTO Courses
                (
                    OwnerId,
                    TermId,
                    Code,
                    Name,
                    CreditHours,
                    Description,
                    Instructor,
                    Category
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @OwnerId,
                    @TermId,
                    @Code,
                    @Name,
                    @CreditHours,
                    @Description,
                    @Instructor,
                    @Category
                );
                """;

            AddParameter(
                courseCommand,
                "@OwnerId",
                ownerId);

            AddParameter(
                courseCommand,
                "@TermId",
                model.TermId.Value);

            AddParameter(
                courseCommand,
                "@Code",
                model.Code.Trim());

            AddParameter(
                courseCommand,
                "@Name",
                model.Name.Trim());

            AddParameter(
                courseCommand,
                "@CreditHours",
                model.CreditHours);

            AddParameter(
                courseCommand,
                "@Description",
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim());

            AddParameter(
                courseCommand,
                "@Instructor",
                string.IsNullOrWhiteSpace(model.Instructor)
                    ? null
                    : model.Instructor.Trim());

            AddParameter(
                courseCommand,
                "@Category",
                string.IsNullOrWhiteSpace(model.Category)
                    ? null
                    : model.Category.Trim());

            var result =
                await courseCommand.ExecuteScalarAsync();

            if (result is null ||
                result == DBNull.Value)
            {
                await transaction.RollbackAsync();
                return false;
            }

            var courseId = Convert.ToInt32(result);

            foreach (var schedule in model.Schedules)
            {
                if (!schedule.StartTime.HasValue ||
                    !schedule.EndTime.HasValue)
                {
                    continue;
                }

                if (schedule.EndTime.Value <=
                    schedule.StartTime.Value)
                {
                    throw new InvalidOperationException(
                        "Course schedule end time must be after the start time.");
                }

                await using var scheduleCommand =
                    connection.CreateCommand();

                scheduleCommand.Transaction = transaction;

                scheduleCommand.CommandText = """
                    INSERT INTO CourseSchedules
                    (
                        OwnerId,
                        CourseId,
                        DayOfWeek,
                        StartTime,
                        EndTime,
                        Location
                    )
                    VALUES
                    (
                        @OwnerId,
                        @CourseId,
                        @DayOfWeek,
                        @StartTime,
                        @EndTime,
                        @Location
                    );
                    """;

                AddParameter(
                    scheduleCommand,
                    "@OwnerId",
                    ownerId);

                AddParameter(
                    scheduleCommand,
                    "@CourseId",
                    courseId);

                AddParameter(
                    scheduleCommand,
                    "@DayOfWeek",
                    schedule.DayOfWeek);

                AddParameter(
                    scheduleCommand,
                    "@StartTime",
                    schedule.StartTime.Value);

                AddParameter(
                    scheduleCommand,
                    "@EndTime",
                    schedule.EndTime.Value);

                AddParameter(
                    scheduleCommand,
                    "@Location",
                    string.IsNullOrWhiteSpace(schedule.Location)
                        ? null
                        : schedule.Location.Trim());

                await scheduleCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static string GetStatus(
        DateTime? startsOn,
        DateTime? endsOn)
    {
        var today = DateTime.UtcNow.Date;

        if (startsOn.HasValue &&
            today < startsOn.Value.Date)
        {
            return "Upcoming";
        }

        if (endsOn.HasValue &&
            today > endsOn.Value.Date)
        {
            return "Completed";
        }

        return "In Progress";
    }

    private static int GetProgress(string status)
    {
        return status switch
        {
            "Completed" => 100,
            _ => 0
        };
    }

    private static void AddParameter(
        IDbCommand command,
        string name,
        object? value)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;

        command.Parameters.Add(parameter);
    }
}
using System.Data;
using Microsoft.EntityFrameworkCore;
using StudentHub.Data;
using StudentHub.Models;

namespace StudentHub.Services;

public sealed class AcademicTermService(
    StudentHubDbContext db) : IAcademicTermService
{
    public async Task<IReadOnlyList<AcademicTermItem>> GetTermsAsync(
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
                Name,
                StartsOn,
                EndsOn
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

        AddParameter(
            command,
            "@OwnerId",
            ownerId);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var terms = new List<AcademicTermItem>();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            terms.Add(new AcademicTermItem
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                StartsOn = reader.IsDBNull(2)
                    ? null
                    : reader.GetDateTime(2),
                EndsOn = reader.IsDBNull(3)
                    ? null
                    : reader.GetDateTime(3)
            });
        }

        return terms;
    }

    public async Task<bool> CreateTermAsync(
        string ownerId,
        CreateAcademicTermViewModel model)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return false;
        }

        if (model.StartsOn.HasValue &&
            model.EndsOn.HasValue &&
            model.EndsOn.Value.Date < model.StartsOn.Value.Date)
        {
            throw new InvalidOperationException(
                "The end date must be after the start date.");
        }

        var connection = db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO AcademicTerms
            (
                OwnerId,
                Name,
                StartsOn,
                EndsOn
            )
            VALUES
            (
                @OwnerId,
                @Name,
                @StartsOn,
                @EndsOn
            );
            """;

        AddParameter(
            command,
            "@OwnerId",
            ownerId);

        AddParameter(
            command,
            "@Name",
            model.Name.Trim());

        AddParameter(
            command,
            "@StartsOn",
            model.StartsOn?.Date);

        AddParameter(
            command,
            "@EndsOn",
            model.EndsOn?.Date);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();

        return true;
    }

    private static void AddParameter(
        IDbCommand command,
        string name,
        object? value)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value =
            value ?? DBNull.Value;

        command.Parameters.Add(parameter);
    }
}
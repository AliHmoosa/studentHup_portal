using StudentHub.Models;

namespace StudentHub.Services;

public interface IAcademicTermService
{
    Task<IReadOnlyList<AcademicTermItem>> GetTermsAsync(
        string ownerId);

    Task<bool> CreateTermAsync(
        string ownerId,
        CreateAcademicTermViewModel model);
}
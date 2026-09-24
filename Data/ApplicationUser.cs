using Microsoft.AspNetCore.Identity;
namespace StudentHub.Data;
public sealed class ApplicationUser : IdentityUser { public string DisplayName { get; set; } = string.Empty; public string? StudentNumber { get; set; } public string? Programme { get; set; } public bool IsProfileComplete { get; set; } }

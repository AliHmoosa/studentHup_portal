using System.ComponentModel.DataAnnotations;

namespace StudentHub.Models;

public sealed class AcademicTermItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime? StartsOn { get; init; }

    public DateTime? EndsOn { get; init; }
}

public sealed class AcademicTermsPageViewModel
{
    public IReadOnlyList<AcademicTermItem> Terms { get; init; } = [];
}

public sealed class CreateAcademicTermViewModel
{
    [Required]
    [StringLength(80)]
    [Display(Name = "Term Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime? StartsOn { get; set; }

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime? EndsOn { get; set; }
}
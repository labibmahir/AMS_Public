using System.ComponentModel.DataAnnotations;

namespace AMS.Domains.Dto;

public sealed class ClassListDto
{
    public int Oid { get; set; }
    public string ClassName { get; set; } = "";
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
}

public sealed class ClassUpsertDto
{
    [Required]
    [StringLength(60)]
    public string ClassName { get; set; } = "";

    [Required]
    public Guid? UserId { get; set; }
}


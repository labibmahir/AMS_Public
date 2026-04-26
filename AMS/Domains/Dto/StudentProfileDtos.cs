using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace AMS.Domains.Dto;

public sealed class StudentProfileListDto
{
    public Guid Oid { get; set; }
    public string FullName { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string ImagePath { get; set; } = "";
}

public sealed class StudentProfileUpsertDto
{
    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = "";

    [Required]
    [StringLength(50)]
    public string StudentId { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = "";

    [StringLength(20)]
    public string Phone { get; set; } = "";

    /// <summary>
    /// For capturing the uploaded file from the UI.
    /// Not mapped to DB directly, used by the Service to save the file.
    /// </summary>
    public IBrowserFile? ImageFile { get; set; }

    /// <summary>
    /// Existing image path from DB, used during edit to retain the old image 
    /// if no new ImageFile is provided.
    /// </summary>
    public string? ExistingImagePath { get; set; }
}

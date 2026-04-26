using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AMS.Domains.Entities;

public class StudentProfile
{
    [Key]
    public Guid Oid { get; set; }
    
    [Required]
    [StringLength(150)]
    public string FullName { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Email { get; set; }
    
    [StringLength(20)]
    public string Phone { get; set; }

    [Required]
    [StringLength(50)]
    public string StudentId { get; set; }

    [Required]
    public string ImagePath { get; set; }
    
    [JsonIgnore]
    public virtual IEnumerable<AssignedStudent> AssignedStudents { get; set; }
}
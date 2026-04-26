using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AMS.Domains.Entities;

public class AssignedStudent
{
    [Key]
    public Guid Oid { get; set; }
    
    [Required]
    public Guid StudentId { get; set; }
    
    [Required]
    public int ClassId { get; set; }
    
    [JsonIgnore]
    [ForeignKey("ClassId")]
    public virtual Class Class { get; set; }
    
    [JsonIgnore]
    [ForeignKey("StudentId")]
    public virtual StudentProfile StudentProfile { get; set; }
}
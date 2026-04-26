using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AMS.Domains.Entities;

public class DailyAttendance
{
    [Key]
    public Guid Oid { get; set; }
    
    [Required]
    public DateTime AttendanceDate { get; set; }
    
    [Required]
    public Guid StudentId { get; set; }
    
    [Required]
    public bool IsPresent { get; set; }
    
    [Required]
    public bool IsMailSent { get; set; }
    
    [JsonIgnore]
    [ForeignKey("StudentId")]
    public virtual StudentProfile StudentProfile { get; set; }
}
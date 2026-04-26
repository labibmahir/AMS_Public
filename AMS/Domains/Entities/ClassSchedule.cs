using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AMS.Domains.Entities;

public class ClassSchedule
{
    [Key]
    public Guid Oid { get; set; }
    
    [Required]
    public int ClassId { get; set; }

    [Required]
    public DateTime ClassDate { get; set; }

    [Required]
    public TimeSpan StartScheduleTime { get; set; }
    
    [Required]
    public TimeSpan EndScheduleTime { get; set; }
    
    [ForeignKey("ClassId")]
    [JsonIgnore]
    public virtual Class Class { get; set; }
    
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AMS.Domains.Entities;

public class Class
{
    [Key]
    public int Oid { get; set; }
    
    [Required]
    [StringLength(60)]
    public string ClassName { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [JsonIgnore]
    [ForeignKey("UserId")]
    public virtual UserAccount UserAccount { get; set; }
    
    [JsonIgnore]
    public virtual IEnumerable<ClassSchedule> ClassSchedules { get; set; }

    [JsonIgnore]
    public virtual IEnumerable<AssignedStudent> AssignedStudents { get; set; }
}
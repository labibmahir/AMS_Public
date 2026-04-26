using System.ComponentModel.DataAnnotations;

namespace AMS.Domains.Entities;

public class Holiday
{
    [Key]
    public Guid Oid { get; set; }

    [Required]
    public int Year { get; set; }
    
    public DayOfWeek? DayOfWeek { get; set; }
    
    public DateTime? VacationDate { get; set; }
    
    public string HolidayName { get; set; }
    
    [Required]
    public bool IsWeekend { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace AMS.Domains.Dto;

public sealed class ClassScheduleListDto
{
    public Guid Oid { get; set; }
    public int ClassId { get; set; }
    public DateTime ClassDate { get; set; }
    public TimeSpan StartScheduleTime { get; set; }
    public TimeSpan EndScheduleTime { get; set; }
}

public sealed class ClassScheduleGenerateDto
{
    public int ClassId { get; set; }
    
    public bool UseDateRange { get; set; }

    public int Year { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [Required]
    public TimeSpan StartScheduleTime { get; set; } = new TimeSpan(9, 0, 0);

    [Required]
    public TimeSpan EndScheduleTime { get; set; } = new TimeSpan(10, 0, 0);

    [Required]
    public List<DayOfWeek> SelectedDays { get; set; } = new();
}

public sealed class ClassScheduleUpdateDto
{
    [Required]
    public DateTime ClassDate { get; set; }

    [Required]
    public TimeSpan StartScheduleTime { get; set; }

    [Required]
    public TimeSpan EndScheduleTime { get; set; }
}

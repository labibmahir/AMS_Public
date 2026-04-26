using System.ComponentModel.DataAnnotations;

namespace AMS.Domains.Dto;

public sealed class HolidayListDto
{
    public Guid Oid { get; set; }
    public int Year { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public DateTime? VacationDate { get; set; }
    public string? HolidayName { get; set; }
    public bool IsWeekend { get; set; }
}

public sealed class HolidayUpsertDto
{
    [Required]
    public int Year { get; set; }

    [Required]
    public bool IsWeekend { get; set; }

    // For Weekend mode
    public List<DayOfWeek> SelectedDays { get; set; } = new();

    // For Non-Weekend mode
    public string? HolidayName { get; set; }
    public DateTime? VacationDate { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

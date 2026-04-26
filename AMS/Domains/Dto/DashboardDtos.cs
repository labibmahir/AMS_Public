namespace AMS.Domains.Dto;

public class DashboardStatsDto
{
    public int TotalClasses { get; set; }
    public double AverageAttendance { get; set; }
    public List<DailyAttendanceChartDto> LastSevenDaysAttendance { get; set; } = new();
    public List<ClassWiseAttendanceDto> ClassWiseAttendance { get; set; } = new();
    public List<TodayScheduleDto> TodaySchedules { get; set; } = new();
}

public class DailyAttendanceChartDto
{
    public DateTime Date { get; set; }
    public double AttendanceRate { get; set; }
}

public class ClassWiseAttendanceDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; }
    public double AverageAttendance { get; set; }
    public int TotalSchedules { get; set; }
}

public enum ScheduleStatus
{
    Upcoming,
    Ongoing,
    Done
}

public class TodayScheduleDto
{
    public Guid ScheduleId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; }
    public bool IsUpcoming { get; set; }
}

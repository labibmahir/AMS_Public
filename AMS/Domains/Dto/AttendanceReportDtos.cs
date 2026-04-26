using System;
using System.Collections.Generic;

namespace AMS.Domains.Dto;

public class ClassAttendanceReportDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; }
    public double AverageAttendance { get; set; }
    public int TotalSchedules { get; set; }
    public int UniqueStudents { get; set; }
}

public class StudentAttendanceReportDto
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; }
    public string StudentIdString { get; set; }
    public double AverageAttendance { get; set; }
    public int TotalSchedules { get; set; }
    public int TotalPresent { get; set; }
}

public class PeriodicAttendanceDto
{
    public string PeriodLabel { get; set; } // e.g. "Week 1", "Jan 2024"
    public double AverageAttendance { get; set; }
    public int TotalSchedules { get; set; }
}

public class ScheduleWiseAttendanceReportDto
{
    public Guid ScheduleId { get; set; }
    public string ClassName { get; set; }
    public DateTime ScheduleDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public int PresentCount { get; set; }
    public int TotalCount { get; set; }
    public string PresentStudentsList { get; set; }
}

public class DateWiseAttendanceReportDto
{
    public string StudentName { get; set; }
    public string StudentId { get; set; }
    public Dictionary<DateTime, string> Attendance { get; set; } = new(); // Date -> "P" or "A"
}

public class DateWiseReportResultDto
{
    public List<DateTime> Dates { get; set; } = new();
    public List<DateWiseAttendanceReportDto> StudentAttendance { get; set; } = new();
}

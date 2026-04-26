using AMS.Domains.Data;
using AMS.Domains.Dto;
using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AMS.Services.DBService;

public sealed class ReportService
{
    private readonly IDbContextFactory<DataContext> contextFactory;

    public ReportService(IDbContextFactory<DataContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<List<ClassAttendanceReportDto>> GetClassWiseReport()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var today = DateTime.UtcNow.Date;

        var classes = await context.Classes
            .AsNoTracking()
            .Include(c => c.AssignedStudents)
            .Include(c => c.ClassSchedules)
            .ToListAsync();

        var report = new List<ClassAttendanceReportDto>();

        foreach (var cls in classes)
        {
            var pastSchedules = cls.ClassSchedules.Where(s => s.ClassDate.Date <= today).ToList();
            var studentIds = cls.AssignedStudents.Select(a => a.StudentId).ToList();

            if (!pastSchedules.Any() || !studentIds.Any())
            {
                report.Add(new ClassAttendanceReportDto
                {
                    ClassId = cls.Oid,
                    ClassName = cls.ClassName,
                    AverageAttendance = 0,
                    TotalSchedules = pastSchedules.Count,
                    UniqueStudents = studentIds.Count
                });
                continue;
            }

            var scheduleDates = pastSchedules.Select(s => s.ClassDate.Date).Distinct().ToList();
            var attendances = await context.DailyAttendances
                .AsNoTracking()
                .Where(at => studentIds.Contains(at.StudentId) && scheduleDates.Contains(at.AttendanceDate.Date))
                .ToListAsync();

            var attendanceLookup = attendances.ToLookup(at => at.AttendanceDate.Date, at => at.StudentId);

            var rates = new List<double>();
            foreach (var schedule in pastSchedules)
            {
                var presentCount = assignedStudentCount(studentIds, attendanceLookup[schedule.ClassDate.Date]);
                rates.Add((double)presentCount / studentIds.Count * 100);
            }

            report.Add(new ClassAttendanceReportDto
            {
                ClassId = cls.Oid,
                ClassName = cls.ClassName,
                AverageAttendance = rates.Average(),
                TotalSchedules = pastSchedules.Count,
                UniqueStudents = studentIds.Count
            });
        }

        return report;
    }

    public async Task<List<StudentAttendanceReportDto>> GetStudentAttendanceReport(int? classId = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var today = DateTime.UtcNow.Date;

        var studentQuery = context.StudentProfiles.AsNoTracking();
        if (classId.HasValue)
        {
            var studentIdsInClass = await context.AssignedStudents
                .Where(a => a.ClassId == classId.Value)
                .Select(a => a.StudentId)
                .ToListAsync();
            studentQuery = studentQuery.Where(s => studentIdsInClass.Contains(s.Oid));
        }

        var students = await studentQuery.ToListAsync();
        var report = new List<StudentAttendanceReportDto>();

        foreach (var student in students)
        {
            var assignments = await context.AssignedStudents
                .Where(a => a.StudentId == student.Oid)
                .Select(a => a.ClassId)
                .ToListAsync();

            var schedules = await context.ClassSchedules
                .Where(s => assignments.Contains(s.ClassId) && s.ClassDate.Date <= today)
                .ToListAsync();

            if (!schedules.Any())
            {
                report.Add(new StudentAttendanceReportDto
                {
                    StudentId = student.Oid,
                    FullName = student.FullName,
                    StudentIdString = student.StudentId,
                    AverageAttendance = 0,
                    TotalSchedules = 0,
                    TotalPresent = 0
                });
                continue;
            }

            var dates = schedules.Select(s => s.ClassDate.Date).Distinct().ToList();
            var presentCount = await context.DailyAttendances
                .CountAsync(at => at.StudentId == student.Oid && dates.Contains(at.AttendanceDate.Date));

            report.Add(new StudentAttendanceReportDto
            {
                StudentId = student.Oid,
                FullName = student.FullName,
                StudentIdString = student.StudentId,
                AverageAttendance = (double)presentCount / schedules.Count * 100,
                TotalSchedules = schedules.Count,
                TotalPresent = presentCount
            });
        }

        return report;
    }

    public async Task<List<PeriodicAttendanceDto>> GetWeeklyClassReport(int classId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var today = DateTime.UtcNow.Date;

        var schedules = await context.ClassSchedules
            .AsNoTracking()
            .Where(s => s.ClassId == classId && s.ClassDate.Date <= today)
            .OrderBy(s => s.ClassDate)
            .ToListAsync();

        var assignedStudents = await context.AssignedStudents
            .AsNoTracking()
            .Where(a => a.ClassId == classId)
            .Select(a => a.StudentId)
            .ToListAsync();

        if (!schedules.Any() || !assignedStudents.Any()) return new List<PeriodicAttendanceDto>();

        var dates = schedules.Select(s => s.ClassDate.Date).Distinct().ToList();
        var attendances = await context.DailyAttendances
            .AsNoTracking()
            .Where(at => assignedStudents.Contains(at.StudentId) && dates.Contains(at.AttendanceDate.Date))
            .ToListAsync();
        
        var attendanceLookup = attendances.ToLookup(at => at.AttendanceDate.Date, at => at.StudentId);

        var report = schedules.GroupBy(s => GetIsoWeek(s.ClassDate))
            .Select(g => {
                var weekRates = g.Select(s => (double)assignedStudentCount(assignedStudents, attendanceLookup[s.ClassDate.Date]) / assignedStudents.Count * 100).ToList();
                return new PeriodicAttendanceDto
                {
                    PeriodLabel = $"Week {g.Key.Week}, {g.Key.Year}",
                    AverageAttendance = weekRates.Average(),
                    TotalSchedules = g.Count()
                };
            })
            .ToList();

        return report;
    }

    public async Task<List<PeriodicAttendanceDto>> GetMonthlyClassReport(int classId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var today = DateTime.UtcNow.Date;

        var schedules = await context.ClassSchedules
            .AsNoTracking()
            .Where(s => s.ClassId == classId && s.ClassDate.Date <= today)
            .OrderBy(s => s.ClassDate)
            .ToListAsync();

        var assignedStudents = await context.AssignedStudents
            .AsNoTracking()
            .Where(a => a.ClassId == classId)
            .Select(a => a.StudentId)
            .ToListAsync();

        if (!schedules.Any() || !assignedStudents.Any()) return new List<PeriodicAttendanceDto>();

        var dates = schedules.Select(s => s.ClassDate.Date).Distinct().ToList();
        var attendances = await context.DailyAttendances
            .AsNoTracking()
            .Where(at => assignedStudents.Contains(at.StudentId) && dates.Contains(at.AttendanceDate.Date))
            .ToListAsync();
        
        var attendanceLookup = attendances.ToLookup(at => at.AttendanceDate.Date, at => at.StudentId);

        var report = schedules.GroupBy(s => new { s.ClassDate.Year, s.ClassDate.Month })
            .Select(g => {
                var monthRates = g.Select(s => (double)assignedStudentCount(assignedStudents, attendanceLookup[s.ClassDate.Date]) / assignedStudents.Count * 100).ToList();
                return new PeriodicAttendanceDto
                {
                    PeriodLabel = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month)} {g.Key.Year}",
                    AverageAttendance = monthRates.Average(),
                    TotalSchedules = g.Count()
                };
            })
            .ToList();

        return report;
    }

    public async Task<List<ScheduleWiseAttendanceReportDto>> GetScheduleWiseReport(int? classId = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var today = DateTime.UtcNow.Date;

        var query = context.ClassSchedules
            .AsNoTracking()
            .Include(s => s.Class)
            .ThenInclude(c => c.AssignedStudents)
            .ThenInclude(a => a.StudentProfile)
            .Where(s => s.ClassDate.Date <= today);

        if (classId.HasValue)
        {
            query = query.Where(s => s.ClassId == classId.Value);
        }

        var schedules = await query.OrderByDescending(s => s.ClassDate).ToListAsync();
        var report = new List<ScheduleWiseAttendanceReportDto>();

        foreach (var schedule in schedules)
        {
            var studentIds = schedule.Class.AssignedStudents.Select(a => a.StudentId).ToList();
            var attendances = await context.DailyAttendances
                .AsNoTracking()
                .Include(at => at.StudentProfile)
                .Where(at => studentIds.Contains(at.StudentId) && at.AttendanceDate.Date == schedule.ClassDate.Date)
                .ToListAsync();

            report.Add(new ScheduleWiseAttendanceReportDto
            {
                ScheduleId = schedule.Oid,
                ClassName = schedule.Class.ClassName,
                ScheduleDate = schedule.ClassDate,
                StartTime = schedule.StartScheduleTime,
                PresentCount = attendances.Count,
                TotalCount = studentIds.Count,
                PresentStudentsList = string.Join(", ", attendances.Select(at => at.StudentProfile?.FullName ?? "Unknown"))
            });
        }

        return report;
    }

    public async Task<List<PeriodicAttendanceDto>> GetWeeklyStudentReport(Guid studentId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var today = DateTime.UtcNow.Date;

        var assignments = await context.AssignedStudents
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .Select(a => a.ClassId)
            .ToListAsync();

        var schedules = await context.ClassSchedules
            .AsNoTracking()
            .Where(s => assignments.Contains(s.ClassId) && s.ClassDate.Date <= today)
            .OrderBy(s => s.ClassDate)
            .ToListAsync();

        if (!schedules.Any()) return new List<PeriodicAttendanceDto>();

        var dates = schedules.Select(s => s.ClassDate.Date).Distinct().ToList();
        var attendances = await context.DailyAttendances
            .AsNoTracking()
            .Where(at => at.StudentId == studentId && dates.Contains(at.AttendanceDate.Date))
            .Select(at => at.AttendanceDate.Date)
            .ToListAsync();
        
        var attendanceSet = new HashSet<DateTime>(attendances);

        var report = schedules.GroupBy(s => GetIsoWeek(s.ClassDate))
            .Select(g => {
                var presentInWeek = g.Count(s => attendanceSet.Contains(s.ClassDate.Date));
                return new PeriodicAttendanceDto
                {
                    PeriodLabel = $"Week {g.Key.Week}, {g.Key.Year}",
                    AverageAttendance = (double)presentInWeek / g.Count() * 100,
                    TotalSchedules = g.Count()
                };
            })
            .ToList();

        return report;
    }

    public async Task<List<PeriodicAttendanceDto>> GetMonthlyStudentReport(Guid studentId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var today = DateTime.UtcNow.Date;

        var assignments = await context.AssignedStudents
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .Select(a => a.ClassId)
            .ToListAsync();

        var schedules = await context.ClassSchedules
            .AsNoTracking()
            .Where(s => assignments.Contains(s.ClassId) && s.ClassDate.Date <= today)
            .OrderBy(s => s.ClassDate)
            .ToListAsync();

        if (!schedules.Any()) return new List<PeriodicAttendanceDto>();

        var dates = schedules.Select(s => s.ClassDate.Date).Distinct().ToList();
        var attendances = await context.DailyAttendances
            .AsNoTracking()
            .Where(at => at.StudentId == studentId && dates.Contains(at.AttendanceDate.Date))
            .Select(at => at.AttendanceDate.Date)
            .ToListAsync();
        
        var attendanceSet = new HashSet<DateTime>(attendances);

        var report = schedules.GroupBy(s => new { s.ClassDate.Year, s.ClassDate.Month })
            .Select(g => {
                var presentInMonth = g.Count(s => attendanceSet.Contains(s.ClassDate.Date));
                return new PeriodicAttendanceDto
                {
                    PeriodLabel = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month)} {g.Key.Year}",
                    AverageAttendance = (double)presentInMonth / g.Count() * 100,
                    TotalSchedules = g.Count()
                };
            })
            .ToList();

        return report;
    }

    public async Task<DateWiseReportResultDto> GetDateWiseAttendanceReport(int classId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var today = DateTime.UtcNow.Date;

        var schedules = await context.ClassSchedules
            .AsNoTracking()
            .Where(s => s.ClassId == classId && s.ClassDate.Date <= today)
            .OrderBy(s => s.ClassDate)
            .ToListAsync();

        var assignedStudents = await context.AssignedStudents
            .AsNoTracking()
            .Include(a => a.StudentProfile)
            .Where(a => a.ClassId == classId)
            .OrderBy(a => a.StudentProfile.FullName)
            .ToListAsync();

        if (!schedules.Any() || !assignedStudents.Any())
            return new DateWiseReportResultDto();

        var dates = schedules.Select(s => s.ClassDate.Date).Distinct().ToList();
        var studentIds = assignedStudents.Select(a => a.StudentId).ToList();

        var attendances = await context.DailyAttendances
            .AsNoTracking()
            .Where(at => studentIds.Contains(at.StudentId) && dates.Contains(at.AttendanceDate.Date))
            .ToListAsync();

        var attendanceLookup = attendances.ToLookup(at => (at.StudentId, at.AttendanceDate.Date));

        var result = new DateWiseReportResultDto { Dates = dates };

        foreach (var assignment in assignedStudents)
        {
            var studentReport = new DateWiseAttendanceReportDto
            {
                StudentName = assignment.StudentProfile.FullName,
                StudentId = assignment.StudentProfile.StudentId,
            };

            foreach (var date in dates)
            {
                studentReport.Attendance[date] = attendanceLookup.Contains((assignment.StudentId, date)) ? "P" : "A";
            }

            result.StudentAttendance.Add(studentReport);
        }

        return result;
    }

    private int assignedStudentCount(List<Guid> assigned, IEnumerable<Guid> present)
    {
        return assigned.Count(id => present.Contains(id));
    }

    private (int Year, int Week) GetIsoWeek(DateTime date)
    {
        var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }
        return (date.Year, CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday));
    }
}

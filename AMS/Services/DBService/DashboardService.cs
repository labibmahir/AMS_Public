using AMS.Domains.Data;
using AMS.Domains.Dto;
using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMS.Services.DBService;

public sealed class DashboardService
{
    private readonly IDbContextFactory<DataContext> contextFactory;

    public DashboardService(IDbContextFactory<DataContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<DashboardStatsDto> GetDashboardStats(Guid? userId = null, Enums.UserType? userType = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var stats = new DashboardStatsDto();

        // 1. Base query for classes
        var classesQuery = context.Classes.AsNoTracking();
        if (userType == Enums.UserType.Teacher && userId.HasValue)
        {
            classesQuery = classesQuery.Where(c => c.UserId == userId.Value);
        }

        // 2. Performance-optimized data fetching
        var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
        var nowTime = DateTime.Now.TimeOfDay;

        // Fetch all relevant classes with student counts
        var classes = await classesQuery
            .Include(c => c.AssignedStudents)
            .ToListAsync();

        stats.TotalClasses = classes.Count;

        var classIds = classes.Select(c => c.Oid).ToList();

        // Fetch past and today's schedules for these classes
        var allSchedules = await context.ClassSchedules
            .AsNoTracking()
            .Where(s => classIds.Contains(s.ClassId))
            .Include(s => s.Class)
            .ToListAsync();

        var pastSchedules = allSchedules.Where(s => s.ClassDate <= today).ToList();
        var todaySchedules = allSchedules.Where(s => s.ClassDate == today).ToList();

        // Populate TodaySchedules for the dashboard
        stats.TodaySchedules = todaySchedules
            .Select(s => 
            {
                var isUpcoming = s.StartScheduleTime > nowTime;
                var isOngoing = s.StartScheduleTime <= nowTime && s.EndScheduleTime >= nowTime;
                var status = isOngoing ? "Ongoing" : (isUpcoming ? "Upcoming" : "Done");

                return new TodayScheduleDto
                {
                    ScheduleId = s.Oid,
                    ClassId = s.ClassId,
                    ClassName = s.Class.ClassName,
                    StartTime = s.StartScheduleTime,
                    EndTime = s.EndScheduleTime,
                    Status = status,
                    IsUpcoming = status == "Upcoming"
                };
            })
            .OrderBy(s => s.Status == "Ongoing" ? 0 : (s.Status == "Upcoming" ? 1 : 2))
            .ThenBy(s => s.StartTime)
            .ToList();

        if (pastSchedules.Any())
        {
            // Get all unique student IDs involved in past schedules
            var allAssignedStudentIds = classes
                .SelectMany(s => s.AssignedStudents)
                .Select(a => a.StudentId)
                .Distinct()
                .ToList();

            // Get all relevant attendance records in one go
            var minDate = pastSchedules.Min(s => s.ClassDate.Date);
            var maxDate = pastSchedules.Max(s => s.ClassDate.Date).AddDays(1);
            
            var allAttendances = await context.DailyAttendances
                .AsNoTracking()
                .Where(at => allAssignedStudentIds.Contains(at.StudentId) && at.AttendanceDate >= minDate && at.AttendanceDate < maxDate)
                .Select(at => new { at.StudentId, Date = at.AttendanceDate.Date })
                .ToListAsync();

            // Use a lookup for faster access
            var attendanceLookup = allAttendances.ToLookup(a => a.Date, a => a.StudentId);

            var scheduleRates = new List<double>();

            foreach (var schedule in pastSchedules)
            {
                var cls = classes.FirstOrDefault(c => c.Oid == schedule.ClassId);
                if (cls == null) continue;

                var assignedStudentIds = cls.AssignedStudents.Select(a => a.StudentId).ToList();
                if (!assignedStudentIds.Any()) continue;

                var presentOnDate = attendanceLookup[schedule.ClassDate.Date];
                var presentCount = assignedStudentIds.Count(id => presentOnDate.Contains(id));

                double rate = (double)presentCount / assignedStudentIds.Count * 100;
                scheduleRates.Add(rate);
            }

            if (scheduleRates.Any())
            {
                stats.AverageAttendance = scheduleRates.Average();
            }

            // 3. Last Seven Days Attendance Average
            var lastSevenDays = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            foreach (var date in lastSevenDays)
            {
                var schedulesOnDate = pastSchedules.Where(s => s.ClassDate.Date == date.Date).ToList();
                if (schedulesOnDate.Any())
                {
                    var dayRates = new List<double>();
                    foreach (var schedule in schedulesOnDate)
                    {
                        var cls = classes.FirstOrDefault(c => c.Oid == schedule.ClassId);
                        if (cls == null) continue;

                        var assignedStudentIds = cls.AssignedStudents.Select(a => a.StudentId).ToList();
                        if (!assignedStudentIds.Any()) continue;

                        var presentOnDate = attendanceLookup[date.Date];
                        var presentCount = assignedStudentIds.Count(id => presentOnDate.Contains(id));
                        
                        dayRates.Add((double)presentCount / assignedStudentIds.Count * 100);
                    }
                    
                    stats.LastSevenDaysAttendance.Add(new DailyAttendanceChartDto
                    {
                        Date = date,
                        AttendanceRate = dayRates.Any() ? dayRates.Average() : 0
                    });
                }
                else
                {
                    stats.LastSevenDaysAttendance.Add(new DailyAttendanceChartDto
                    {
                        Date = date,
                        AttendanceRate = 0
                    });
                }
            }

            // 4. Class Wise Attendance Summary
            foreach (var cls in classes)
            {
                var clsSchedules = pastSchedules.Where(s => s.ClassId == cls.Oid).ToList();
                if (clsSchedules.Any())
                {
                    var clsRates = new List<double>();
                    var assignedStudentIds = cls.AssignedStudents.Select(a => a.StudentId).ToList();

                    if (assignedStudentIds.Any())
                    {
                        foreach (var schedule in clsSchedules)
                        {
                            var presentOnDate = attendanceLookup[schedule.ClassDate.Date];
                            var presentCount = assignedStudentIds.Count(id => presentOnDate.Contains(id));

                            clsRates.Add((double)presentCount / assignedStudentIds.Count * 100);
                        }
                    }

                    stats.ClassWiseAttendance.Add(new ClassWiseAttendanceDto
                    {
                        ClassId = cls.Oid,
                        ClassName = cls.ClassName,
                        AverageAttendance = clsRates.Any() ? clsRates.Average() : 0,
                        TotalSchedules = clsSchedules.Count
                    });
                }
                else
                {
                    stats.ClassWiseAttendance.Add(new ClassWiseAttendanceDto
                    {
                        ClassId = cls.Oid,
                        ClassName = cls.ClassName,
                        AverageAttendance = 0,
                        TotalSchedules = 0
                    });
                }
            }
        }

        return stats;
    }
}

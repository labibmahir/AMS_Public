using AMS.Domains.Data;
using AMS.Domains.Dto;
using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMS.Services.DBService;

public sealed class DailyAttendanceService
{
    private readonly AttendanceNotificationService notificationService;
    private readonly IDbContextFactory<DataContext> contextFactory;

    public DailyAttendanceService(IDbContextFactory<DataContext> contextFactory, AttendanceNotificationService notificationService)
    {
        this.contextFactory = contextFactory;
        this.notificationService = notificationService;
    }

    public async Task<List<AttendanceStudentListDto>> GetAttendanceForClassSchedule(int classId, DateTime scheduleDate)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // Get all students assigned to this class
        var assignedStudents = await context.AssignedStudents
            .AsNoTracking()
            .Include(a => a.StudentProfile)
            .Where(a => a.ClassId == classId)
            .ToListAsync();

        // Get attendance records for these students on this specific date
        var targetDate = DateTime.SpecifyKind(scheduleDate.Date, DateTimeKind.Utc);
        var nextDay = targetDate.AddDays(1);
        var studentIds = assignedStudents.Select(a => a.StudentId).ToList();

        var attendances = await context.DailyAttendances
            .AsNoTracking()
            .Where(at => studentIds.Contains(at.StudentId) && at.AttendanceDate >= targetDate && at.AttendanceDate < nextDay)
            .ToListAsync();

        var attendanceDict = attendances
            .GroupBy(at => at.StudentId)
            .ToDictionary(g => g.Key, g => g.First());

        var result = assignedStudents.Select(a => 
        {
            attendanceDict.TryGetValue(a.StudentId, out var attendance);
            return new AttendanceStudentListDto
            {
                StudentId = a.StudentId,
                FullName = a.StudentProfile?.FullName ?? "Unknown",
                StudentIdString = a.StudentProfile?.StudentId ?? "",
                ImagePath = a.StudentProfile?.ImagePath ?? "",
                IsPresent = attendance != null && attendance.IsPresent
            };
        })
        .OrderBy(x => x.FullName)
        .ToList();

        return result;
    }

    public async Task ToggleAttendance(ToggleAttendanceDto dto)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var targetDate = DateTime.SpecifyKind(dto.AttendanceDate.Date, DateTimeKind.Utc);
        var nextDay = targetDate.AddDays(1);

        var existingRecord = await context.DailyAttendances
            .FirstOrDefaultAsync(a => a.StudentId == dto.StudentId && a.AttendanceDate >= targetDate && a.AttendanceDate < nextDay);

        if (existingRecord == null)
        {
            existingRecord = new DailyAttendance
            {
                Oid = Guid.NewGuid(),
                StudentId = dto.StudentId,
                AttendanceDate = DateTime.SpecifyKind(targetDate, DateTimeKind.Utc),
                IsPresent = dto.IsPresent,
                IsMailSent = false
            };
            context.DailyAttendances.Add(existingRecord);
        }
        else
        {
            if (existingRecord.IsPresent != dto.IsPresent)
            {
                existingRecord.IsMailSent = false;
            }
            existingRecord.IsPresent = dto.IsPresent;
            context.DailyAttendances.Update(existingRecord);
        }
        await context.SaveChangesAsync();

        // Send immediate email if present
        if (dto.IsPresent)
        {
            await notificationService.SendImmediatePresentNotificationAsync(dto.StudentId, dto.AttendanceDate, dto.ClassId);
        }
    }
}

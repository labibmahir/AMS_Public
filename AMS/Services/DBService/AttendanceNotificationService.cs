using AMS.Domains.Data;
using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMS.Services.DBService;

public class AttendanceNotificationService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly EmailService _emailService;
    private readonly ILogger<AttendanceNotificationService> _logger;

    public AttendanceNotificationService(
        IDbContextFactory<DataContext> contextFactory,
        EmailService emailService,
        ILogger<AttendanceNotificationService> logger)
    {
        _contextFactory = contextFactory;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendImmediatePresentNotificationAsync(Guid studentId, DateTime attendanceDate, int classId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var student = await context.StudentProfiles.FindAsync(studentId);
        if (student == null || string.IsNullOrWhiteSpace(student.Email)) return;

        var cls = await context.Classes.FindAsync(classId);
        if (cls == null) return;

        var attendanceDateUtc = DateTime.SpecifyKind(attendanceDate.Date, DateTimeKind.Utc);
        var schedule = await context.ClassSchedules
            .FirstOrDefaultAsync(s => s.ClassId == classId && s.ClassDate.Date == attendanceDateUtc);
        
        if (schedule == null) return;

        var attendanceRecord = await context.DailyAttendances
            .FirstOrDefaultAsync(da => da.StudentId == studentId && da.AttendanceDate.Date == attendanceDateUtc);

        if (attendanceRecord == null || !attendanceRecord.IsPresent || attendanceRecord.IsMailSent) return;

        try
        {
            await _emailService.SendEmailAsync(
                student.Email,
                $"Attendance Notification: {cls.ClassName}",
                $@"
                <h3>Attendance Summary</h3>
                <p>Hello {student.FullName},</p>
                <p>You have been marked <b>Present</b> for your class today.</p>
                <p><b>Class:</b> {cls.ClassName}</p>
                <p><b>Date:</b> {attendanceDate:D}</p>
                <p><b>Time:</b> {schedule.StartScheduleTime:hh\:mm} - {schedule.EndScheduleTime:hh\:mm}</p>
                <p>Thank you for using the Attendance Management System.</p>"
            );

            attendanceRecord.IsMailSent = true;
            context.DailyAttendances.Update(attendanceRecord);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send immediate present email to {Email}", student.Email);
        }
    }

    public async Task ProcessDailyAttendanceNotificationsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;
        var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);

        // 1. Get all schedules for today that ended at least 10 minutes ago
        var schedules = await context.ClassSchedules
            .Include(s => s.Class)
            .Where(s => s.ClassDate.Date == today)
            .ToListAsync();

        foreach (var schedule in schedules)
        {
            // Calculate end time
            var scheduleEndTime = today.Add(schedule.EndScheduleTime);
            if (now < scheduleEndTime.AddMinutes(10)) 
            {
                // Not yet time to send absent notifications for this schedule
                continue;
            }

            // 2. Get all assigned students for this class
            var assignedStudents = await context.AssignedStudents
                .Include(as_ => as_.StudentProfile)
                .Where(as_ => as_.ClassId == schedule.ClassId)
                .ToListAsync();

            // 3. Get attendance records for today for these students
            var attendances = await context.DailyAttendances
                .Where(da => da.AttendanceDate.Date == today)
                .ToListAsync();

            var attendanceDict = attendances.ToDictionary(a => a.StudentId);

            foreach (var mapping in assignedStudents)
            {
                var student = mapping.StudentProfile;
                if (student == null || string.IsNullOrWhiteSpace(student.Email)) continue;

                attendanceDict.TryGetValue(student.Oid, out var attendanceRecord);
                
                // If mail already sent (or student was marked present and mail sent immediately), skip
                if (attendanceRecord != null && attendanceRecord.IsMailSent) continue;
                
                // If student is present, they should have received an immediate notification. 
                // But if for some reason they didn't, we can send it here or skip. 
                // User logic: "if absent it will check... if not found as present then it will send that students assigned mail absent notification"
                
                bool isPresent = attendanceRecord != null && attendanceRecord.IsPresent;
                if (isPresent) continue; // Should have been handled immediately or by a manual trigger

                try
                {
                    await _emailService.SendEmailAsync(
                        student.Email,
                        $"Absence Notification: {schedule.Class.ClassName}",
                        $@"
                        <h3>Attendance Summary: Absent</h3>
                        <p>Hello {student.FullName},</p>
                        <p>You were marked <b>Absent</b> or no attendance was recorded for your class today.</p>
                        <p><b>Class:</b> {schedule.Class.ClassName}</p>
                        <p><b>Date:</b> {schedule.ClassDate:D}</p>
                        <p><b>Time:</b> {schedule.StartScheduleTime:hh\:mm} - {schedule.EndScheduleTime:hh\:mm}</p>
                        <p>Status: <span style='color: red'>Absent</span></p>
                        <p>Please contact your teacher if you believe this is an error.</p>"
                    );

                    if (attendanceRecord != null)
                    {
                        attendanceRecord.IsMailSent = true;
                        context.DailyAttendances.Update(attendanceRecord);
                    }
                    else
                    {
                        context.DailyAttendances.Add(new DailyAttendance
                        {
                            Oid = Guid.NewGuid(),
                            StudentId = student.Oid,
                            AttendanceDate = DateTime.SpecifyKind(today, DateTimeKind.Utc),
                            IsPresent = false,
                            IsMailSent = true
                        });
                    }
                    
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send absent email to {Email}", student.Email);
                }
            }
        }
    }
}

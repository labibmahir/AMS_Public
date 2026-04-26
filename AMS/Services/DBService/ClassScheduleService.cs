using AMS.Domains.Data;
using AMS.Domains.Dto;
using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMS.Services.DBService;

public sealed class ClassScheduleService
{
    private readonly IDbContextFactory<DataContext> contextFactory;

    public ClassScheduleService(IDbContextFactory<DataContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<ClassScheduleListDto?> GetById(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var entity = await context.ClassSchedules.AsNoTracking().FirstOrDefaultAsync(x => x.Oid == id);
        if (entity == null) return null;

        return new ClassScheduleListDto
        {
            Oid = entity.Oid,
            ClassId = entity.ClassId,
            ClassDate = entity.ClassDate,
            StartScheduleTime = entity.StartScheduleTime,
            EndScheduleTime = entity.EndScheduleTime
        };
    }

    public async Task<(List<ClassScheduleListDto> Items, int TotalCount)> GetPaged(int classId, string search, int pageNumber, int pageSize)
    {
        search ??= "";
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        await using var context = await contextFactory.CreateDbContextAsync();

        IQueryable<ClassSchedule> query = context.ClassSchedules.AsNoTracking().Where(x => x.ClassId == classId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (DateTime.TryParse(search, out var date))
            {
                var searchDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
                var nextDay = searchDate.AddDays(1);
                query = query.Where(x => x.ClassDate >= searchDate && x.ClassDate < nextDay);
            }
        }
        else
        {
            var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
            query = query.Where(x => x.ClassDate >= today);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.ClassDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ClassScheduleListDto
            {
                Oid = x.Oid,
                ClassId = x.ClassId,
                ClassDate = x.ClassDate,
                StartScheduleTime = x.StartScheduleTime,
                EndScheduleTime = x.EndScheduleTime
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task GenerateSchedules(ClassScheduleGenerateDto dto)
    {
        if (dto == null) throw new InvalidOperationException("Invalid generation data.");
        if (dto.EndScheduleTime <= dto.StartScheduleTime) throw new InvalidOperationException("End time must be after start time.");
        if (dto.SelectedDays == null || !dto.SelectedDays.Any()) throw new InvalidOperationException("Please select at least one day of the week.");

        DateTime start, end;

        if (dto.UseDateRange)
        {
            if (dto.StartDate == null || dto.EndDate == null) throw new InvalidOperationException("Start and End dates are required for date range generation.");
            start = DateTime.SpecifyKind(dto.StartDate.Value.Date, DateTimeKind.Utc);
            end = DateTime.SpecifyKind(dto.EndDate.Value.Date, DateTimeKind.Utc);
            
            if (end < start) throw new InvalidOperationException("End date cannot be before start date.");
            if (start < DateTime.Today) throw new InvalidOperationException("Start date cannot be in the past.");
        }
        else
        {
            start = new DateTime(dto.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            end = new DateTime(dto.Year, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        }

        await using var context = await contextFactory.CreateDbContextAsync();

        var cls = await context.Classes.FindAsync(dto.ClassId);
        if (cls == null) throw new InvalidOperationException("Class not found.");

        var schedules = new List<ClassSchedule>();

        // Fetch existing dates in this range for this class to prevent duplicates
        var existingDates = await context.ClassSchedules
            .Where(x => x.ClassId == dto.ClassId && x.ClassDate >= start && x.ClassDate <= end)
            .Select(x => x.ClassDate.Date)
            .ToListAsync();
        
        var existingDatesSet = new HashSet<DateTime>(existingDates);

        for (var dt = start; dt <= end; dt = dt.AddDays(1))
        {
            if (dto.SelectedDays.Contains(dt.DayOfWeek) && !existingDatesSet.Contains(dt.Date))
            {
                schedules.Add(new ClassSchedule
                {
                    Oid = Guid.NewGuid(),
                    ClassId = dto.ClassId,
                    ClassDate = dt,
                    StartScheduleTime = dto.StartScheduleTime,
                    EndScheduleTime = dto.EndScheduleTime
                });
            }
        }

        if (schedules.Any())
        {
            context.ClassSchedules.AddRange(schedules);
            await context.SaveChangesAsync();
        }
    }

    public async Task Update(Guid id, ClassScheduleUpdateDto dto)
    {
        if (dto == null) throw new InvalidOperationException("Invalid data.");
        if (dto.EndScheduleTime <= dto.StartScheduleTime) throw new InvalidOperationException("End time must be after start time.");

        await using var context = await contextFactory.CreateDbContextAsync();
        var entity = await context.ClassSchedules.FindAsync(id);
        if (entity == null) throw new InvalidOperationException("Schedule not found.");

        entity.ClassDate = DateTime.SpecifyKind(dto.ClassDate, DateTimeKind.Utc);
        entity.StartScheduleTime = dto.StartScheduleTime;
        entity.EndScheduleTime = dto.EndScheduleTime;

        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var entity = await context.ClassSchedules.FindAsync(id);
        if (entity != null)
        {
            context.ClassSchedules.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}

using AMS.Domains.Data;
using AMS.Domains.Dto;
using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMS.Services.DBService;

public sealed class HolidayService
{
    private readonly IDbContextFactory<DataContext> contextFactory;

    public HolidayService(IDbContextFactory<DataContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<(List<HolidayListDto> Items, int TotalCount)> GetPaged(string search, int pageNumber, int pageSize)
    {
        search ??= "";
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        await using var context = await contextFactory.CreateDbContextAsync();

        IQueryable<Holiday> query = context.Holidays.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (DateTime.TryParse(search, out var date))
            {
                var searchDateUtc = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
                query = query.Where(x => x.VacationDate != null && x.VacationDate.Value.Date == searchDateUtc);
            }
            else
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.HolidayName != null && x.HolidayName.ToLower().Contains(s));
            }
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.VacationDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new HolidayListDto
            {
                Oid = x.Oid,
                Year = x.Year,
                DayOfWeek = x.DayOfWeek,
                VacationDate = x.VacationDate,
                HolidayName = x.HolidayName,
                IsWeekend = x.IsWeekend
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task Add(HolidayUpsertDto dto)
    {
        if (dto is null) throw new InvalidOperationException("Holiday info is required.");

        await using var context = await contextFactory.CreateDbContextAsync();

        if (dto.IsWeekend)
        {
            if (dto.SelectedDays == null || !dto.SelectedDays.Any())
            {
                throw new InvalidOperationException("Please select at least one day for the weekend.");
            }

            var holidays = new List<Holiday>();
            var start = new DateTime(dto.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(dto.Year, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            for (var dt = start; dt <= end; dt = dt.AddDays(1))
            {
                if (dto.SelectedDays.Contains(dt.DayOfWeek))
                {
                    holidays.Add(new Holiday
                    {
                        Oid = Guid.NewGuid(),
                        Year = dto.Year,
                        DayOfWeek = dt.DayOfWeek,
                        VacationDate = dt,
                        HolidayName = $"{dt.DayOfWeek} Weekend",
                        IsWeekend = true
                    });
                }
            }

            context.Holidays.AddRange(holidays);
            await context.SaveChangesAsync();
        }
        else
        {
            dto.HolidayName = (dto.HolidayName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(dto.HolidayName))
            {
                throw new InvalidOperationException("Holiday name is required for non-weekend holidays.");
            }
            if (!dto.VacationDate.HasValue)
            {
                throw new InvalidOperationException("Vacation date is required.");
            }

            var entity = new Holiday
            {
                Oid = Guid.NewGuid(),
                Year = dto.Year,
                VacationDate = DateTime.SpecifyKind(dto.VacationDate.Value, DateTimeKind.Utc),
                HolidayName = dto.HolidayName,
                IsWeekend = false
            };

            context.Holidays.Add(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task Update(Guid id, HolidayUpsertDto dto)
    {
        if (dto is null) throw new InvalidOperationException("Holiday info is required.");

        await using var context = await contextFactory.CreateDbContextAsync();
        var entity = await context.Holidays.FindAsync(id);
        if (entity is null)
        {
            throw new InvalidOperationException("Holiday not found.");
        }

        // For weekend, we might not update in bulk through Update method,
        // it applies to the single record.
        if (dto.IsWeekend)
        {
            // If they modify an explicitly generated weekend record, we update it as a single row.
            entity.Year = dto.Year;
            entity.VacationDate = dto.VacationDate.HasValue ? DateTime.SpecifyKind(dto.VacationDate.Value, DateTimeKind.Utc) : null;
            entity.HolidayName = dto.HolidayName ?? entity.HolidayName;
            entity.IsWeekend = true;
        }
        else
        {
            dto.HolidayName = (dto.HolidayName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(dto.HolidayName))
            {
                throw new InvalidOperationException("Holiday name is required.");
            }
            if (!dto.VacationDate.HasValue)
            {
                throw new InvalidOperationException("Vacation date is required.");
            }

            entity.Year = dto.Year;
            entity.VacationDate = DateTime.SpecifyKind(dto.VacationDate.Value, DateTimeKind.Utc);
            entity.HolidayName = dto.HolidayName;
            entity.IsWeekend = false;
            // Optionally clear DayOfWeek if switching from weekend to non-weekend
            entity.DayOfWeek = null;
        }

        await context.SaveChangesAsync();
    }
}

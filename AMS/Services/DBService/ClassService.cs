using AMS.Domains.Data;
using AMS.Domains.Dto;
using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMS.Services.DBService;

public sealed class ClassService
{
    private readonly IDbContextFactory<DataContext> contextFactory;

    public ClassService(IDbContextFactory<DataContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<(List<ClassListDto> Items, int TotalCount)> GetPaged(string search, int pageNumber, int pageSize, Guid? userId = null, Enums.UserType? userType = null)
    {
        search ??= "";
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        await using var context = await contextFactory.CreateDbContextAsync();

        IQueryable<Class> classQuery = context.Classes.AsNoTracking();

        if (userType == Enums.UserType.Teacher && userId.HasValue)
        {
            classQuery = classQuery.Where(x => x.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            classQuery = classQuery.Where(x => x.ClassName.ToLower().Contains(s));
        }

        var total = await classQuery.CountAsync();

        var items = await classQuery
            .OrderBy(c => c.ClassName)
            .Include(c => c.UserAccount)
            .Select(c => new ClassListDto
            {
                Oid = c.Oid,
                ClassName = c.ClassName,
                UserId = c.UserId,
                FullName = c.UserAccount != null ? c.UserAccount.FullName : "N/A"
            })
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Class> GetById(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Classes.AsNoTracking().FirstOrDefaultAsync(x => x.Oid == id);
    }

    public async Task<Class> Add(ClassUpsertDto dto)
    {
        if (dto is null)
        {
            throw new InvalidOperationException("Class is required.");
        }

        dto.ClassName = (dto.ClassName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dto.ClassName))
        {
            throw new InvalidOperationException("Class name is required.");
        }

        await using var context = await contextFactory.CreateDbContextAsync();
        var exists = await context.Classes.AnyAsync(x => x.ClassName == dto.ClassName);
        if (exists)
        {
            throw new InvalidOperationException("Class name already exists.");
        }

        var entity = new Class
        {
            ClassName = dto.ClassName,
            UserId = dto.UserId ?? Guid.Empty
        };

        context.Classes.Add(entity);
        await context.SaveChangesAsync();

        return entity;
    }

    public async Task<Class> Update(int id, ClassUpsertDto dto)
    {
        if (dto is null)
        {
            throw new InvalidOperationException("Class is required.");
        }

        dto.ClassName = (dto.ClassName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dto.ClassName))
        {
            throw new InvalidOperationException("Class name is required.");
        }

        await using var context = await contextFactory.CreateDbContextAsync();
        var entity = await context.Classes.FindAsync(id);
        if (entity is null)
        {
            throw new InvalidOperationException("Class not found.");
        }

        var exists = await context.Classes.AnyAsync(x => x.Oid != id && x.ClassName == dto.ClassName);
        if (exists)
        {
            throw new InvalidOperationException("Class name already exists.");
        }

        entity.ClassName = dto.ClassName;
        entity.UserId = dto.UserId ?? Guid.Empty;

        await context.SaveChangesAsync();
        return entity;
    }
}

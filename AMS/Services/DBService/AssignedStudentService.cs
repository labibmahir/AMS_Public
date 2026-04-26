using AMS.Domains.Data;
using AMS.Domains.Dto;
using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMS.Services.DBService;

public class AssignedStudentService
{
    private readonly IDbContextFactory<DataContext> contextFactory;

    public AssignedStudentService(IDbContextFactory<DataContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<List<AssignedStudentListDto>> GetByClassId(int classId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var items = await (
                from a in context.AssignedStudents.AsNoTracking()
                join s in context.StudentProfiles.AsNoTracking() on a.StudentId equals s.Oid
                where a.ClassId == classId
                orderby s.FullName
                select new AssignedStudentListDto
                {
                    Oid = a.Oid,
                    StudentId = a.StudentId,
                    ClassId = a.ClassId,
                    StudentName = s.FullName,
                    StudentIdString = s.StudentId
                })
            .ToListAsync();

        return items;
    }

    public async Task<List<AssignedStudentListDto>> GetByStudentId(Guid studentId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var items = await (
                from a in context.AssignedStudents.AsNoTracking()
                join c in context.Classes.AsNoTracking() on a.ClassId equals c.Oid
                where a.StudentId == studentId
                orderby c.ClassName
                select new AssignedStudentListDto
                {
                    Oid = a.Oid,
                    StudentId = a.StudentId,
                    ClassId = a.ClassId,
                    ClassName = c.ClassName
                })
            .ToListAsync();

        return items;
    }

    public async Task Add(AssignedStudentCreateDto dto)
    {
        if (dto == null) throw new InvalidOperationException("Invalid assignment data.");

        await using var context = await contextFactory.CreateDbContextAsync();

        // Check if student exists
        var studentExists = await context.StudentProfiles.AnyAsync(s => s.Oid == dto.StudentId);
        if (!studentExists) throw new InvalidOperationException("Student not found.");

        // Check if class exists
        var classExists = await context.Classes.AnyAsync(c => c.Oid == dto.ClassId);
        if (!classExists) throw new InvalidOperationException("Class not found.");

        // Check if already assigned
        var alreadyAssigned = await context.AssignedStudents.AnyAsync(a => a.StudentId == dto.StudentId && a.ClassId == dto.ClassId);
        if (alreadyAssigned)
        {
            throw new InvalidOperationException("This student is already assigned to this class.");
        }

        var assignment = new AssignedStudent
        {
            Oid = Guid.NewGuid(),
            StudentId = dto.StudentId,
            ClassId = dto.ClassId
        };

        context.AssignedStudents.Add(assignment);
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var entity = await context.AssignedStudents.FindAsync(id);
        if (entity != null)
        {
            context.AssignedStudents.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task<StudentProfile?> GetStudentById(Guid studentId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.Oid == studentId);
    }
}

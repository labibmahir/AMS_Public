using AMS.Domains.Data;
using AMS.Domains.Dto;
using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Forms;

namespace AMS.Services.DBService;

public sealed class StudentProfileService
{
    private readonly IDbContextFactory<DataContext> contextFactory;
    private readonly IWebHostEnvironment env;

    public StudentProfileService(IDbContextFactory<DataContext> contextFactory, IWebHostEnvironment env)
    {
        this.contextFactory = contextFactory;
        this.env = env;
    }

    public async Task<(List<StudentProfileListDto> Items, int TotalCount)> GetPaged(string search, int pageNumber, int pageSize)
    {
        search ??= "";
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        await using var context = await contextFactory.CreateDbContextAsync();

        IQueryable<StudentProfile> query = context.StudentProfiles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x => x.FullName.ToLower().Contains(s) || x.StudentId.ToLower().Contains(s) || x.Email.ToLower().Contains(s) || x.Phone.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StudentProfileListDto
            {
                Oid = x.Oid,
                FullName = x.FullName,
                StudentId = x.StudentId,
                Email = x.Email,
                Phone = x.Phone,
                ImagePath = x.ImagePath
            })
            .ToListAsync();

        return (items, total);
    }

    private async Task<string> SaveImageAsync(IBrowserFile file)
    {
        var folderInfo = new DirectoryInfo(Path.Combine(env.WebRootPath, "uploads", "student_profiles"));
        if (!folderInfo.Exists)
        {
            folderInfo.Create();
        }

        var ext = Path.GetExtension(file.Name);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folderInfo.FullName, fileName);

        // Save file (allow up to ~10MB)
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(fileStream);

        return $"/uploads/student_profiles/{fileName}";
    }

    public async Task Add(StudentProfileUpsertDto dto)
    {
        if (dto is null) throw new InvalidOperationException("Student profile is required.");
        if (dto.ImageFile is null) throw new InvalidOperationException("Profile image is required.");

        var imagePath = await SaveImageAsync(dto.ImageFile);

        await using var context = await contextFactory.CreateDbContextAsync();

        var profile = new StudentProfile
        {
            Oid = Guid.NewGuid(),
            FullName = dto.FullName,
            StudentId = dto.StudentId,
            Email = dto.Email,
            Phone = dto.Phone,
            ImagePath = imagePath
        };

        context.StudentProfiles.Add(profile);
        await context.SaveChangesAsync();
    }

    public async Task Update(Guid id, StudentProfileUpsertDto dto)
    {
        if (dto is null) throw new InvalidOperationException("Student profile is required.");

        await using var context = await contextFactory.CreateDbContextAsync();
        var existing = await context.StudentProfiles.FindAsync(id);
        if (existing is null)
        {
            throw new InvalidOperationException("Student profile not found.");
        }

        existing.FullName = dto.FullName;
        existing.StudentId = dto.StudentId;
        existing.Email = dto.Email;
        existing.Phone = dto.Phone;

        if (dto.ImageFile is not null)
        {
            // If there's an old image, we could theoretically delete it from disk here, 
            // but for simplicity/safety we just drop the reference and add the new one.
            existing.ImagePath = await SaveImageAsync(dto.ImageFile);
        }

        await context.SaveChangesAsync();
    }
}


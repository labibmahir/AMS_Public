using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMS.Domains.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        
    }
    
    public DbSet<AssignedStudent> AssignedStudents { get; set; }
    
    public DbSet<ClassSchedule> ClassSchedules { get; set; }
    
    public DbSet<Class> Classes { get; set; }
    
    public DbSet<DailyAttendance> DailyAttendances { get; set; }
    
    public DbSet<Holiday> Holidays { get; set; }
    
    public DbSet<StudentProfile> StudentProfiles { get; set; }
    
    public DbSet<UserAccount> UserAccounts { get; set; }
}
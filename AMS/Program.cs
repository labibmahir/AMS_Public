using AMS.Components;
using AMS.Domains.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<DataContext>(x =>
    x.UseNpgsql(builder.Configuration.GetConnectionString("DBLocation")));

builder.Services.AddScoped<AMS.Services.DBService.UserAccountService>();
builder.Services.AddScoped<AMS.Services.DBService.ClassService>();
builder.Services.AddScoped<AMS.Services.DBService.StudentProfileService>();
builder.Services.AddScoped<AMS.Services.DBService.ClassScheduleService>();
builder.Services.AddScoped<AMS.Services.DBService.HolidayService>();
builder.Services.AddScoped<AMS.Services.DBService.AssignedStudentService>();
builder.Services.AddScoped<AMS.Services.DBService.DailyAttendanceService>();
builder.Services.AddScoped<AMS.Services.DBService.DashboardService>();
builder.Services.AddScoped<AMS.Services.DBService.ReportService>();
builder.Services.AddScoped<AMS.Services.DBService.ExportService>();
builder.Services.Configure<AMS.Domains.Dto.SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<AMS.Services.DBService.EmailService>();
builder.Services.AddScoped<AMS.Services.DBService.AttendanceNotificationService>();
builder.Services.AddScoped<AMS.Services.SessionService.AuthSessionService>();
builder.Services.AddHostedService<AMS.Services.AttendanceBackgroundWorker>();

builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

using AMS.Services.DBService;

namespace AMS.Services;

public class AttendanceBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AttendanceBackgroundWorker> _logger;

    public AttendanceBackgroundWorker(
        IServiceProvider serviceProvider,
        ILogger<AttendanceBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Attendance Background Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Attendance Background Worker is running a task.");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<AttendanceNotificationService>();
                    await notificationService.ProcessDailyAttendanceNotificationsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing attendance background task.");
            }

            // Run every 5 minutes to check for finished classes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Attendance Background Worker is stopping.");
    }
}

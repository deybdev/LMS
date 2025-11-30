using System;
using System.Web.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace LMS.Helpers
{
    /// <summary>
    /// Background service to handle scheduled email notifications
    /// This can be called from Global.asax or integrated with a task scheduler
    /// </summary>
    public class EmailNotificationScheduler : IRegisteredObject
    {
        private readonly object _lock = new object();
        private bool _shuttingDown;
        private Timer _timer;

        public EmailNotificationScheduler()
        {
            // Register this object with the hosting environment
            HostingEnvironment.RegisterObject(this);
        }

        /// <summary>
        /// Start the background email notification service
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_shuttingDown)
                    return;

                // Run every 30 minutes
                _timer = new Timer(ExecuteNotificationTasks, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
            }
        }

        /// <summary>
        /// Stop the background service
        /// </summary>
        public void Stop(bool immediate)
        {
            lock (_lock)
            {
                _shuttingDown = true;

                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }

                HostingEnvironment.UnregisterObject(this);
            }
        }

        /// <summary>
        /// Execute notification tasks
        /// </summary>
        private void ExecuteNotificationTasks(object sender)
        {
            if (_shuttingDown)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"EmailNotificationScheduler: Running notification tasks at {DateTime.Now}");

                // Check for scheduled classwork that should now be published
                StudentNotificationService.CheckScheduledClassworkNotifications();

                // Send due date reminders for classwork due within 24 hours
                StudentNotificationService.SendDueDateReminders();

                System.Diagnostics.Debug.WriteLine($"EmailNotificationScheduler: Completed notification tasks at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmailNotificationScheduler Error: {ex.Message}");
                // In production, you might want to log this to a file or database
            }
        }

        /// <summary>
        /// Manually trigger notification tasks (useful for testing or manual execution)
        /// </summary>
        public static void RunNotificationTasks()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Manual EmailNotificationScheduler: Running notification tasks at {DateTime.Now}");

                // Check for scheduled classwork that should now be published
                StudentNotificationService.CheckScheduledClassworkNotifications();

                // Send due date reminders for classwork due within 24 hours
                StudentNotificationService.SendDueDateReminders();

                System.Diagnostics.Debug.WriteLine($"Manual EmailNotificationScheduler: Completed notification tasks at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Manual EmailNotificationScheduler Error: {ex.Message}");
                throw; // Re-throw for manual execution so caller knows about the error
            }
        }
    }

    /// <summary>
    /// Static helper class to manage the email notification scheduler
    /// </summary>
    public static class EmailNotificationManager
    {
        private static EmailNotificationScheduler _scheduler;
        private static readonly object _lock = new object();

        /// <summary>
        /// Start the email notification background service
        /// </summary>
        public static void Start()
        {
            lock (_lock)
            {
                if (_scheduler == null)
                {
                    _scheduler = new EmailNotificationScheduler();
                    _scheduler.Start();
                    System.Diagnostics.Debug.WriteLine("EmailNotificationManager: Background service started");
                }
            }
        }

        /// <summary>
        /// Stop the email notification background service
        /// </summary>
        public static void Stop()
        {
            lock (_lock)
            {
                if (_scheduler != null)
                {
                    _scheduler.Stop(true);
                    _scheduler = null;
                    System.Diagnostics.Debug.WriteLine("EmailNotificationManager: Background service stopped");
                }
            }
        }

        /// <summary>
        /// Manually run notification tasks
        /// </summary>
        public static void RunNow()
        {
            EmailNotificationScheduler.RunNotificationTasks();
        }
    }
}
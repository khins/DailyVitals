using System;
using System.Windows;
using DailyVitals.Data.Configuration;

namespace DailyVitalsApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // QUICK DB CONNECTIVITY TEST
                using var conn = DbConnectionFactory.Create();
                conn.Open();

                MessageBox.Show(
                    "Database connection successful.",
                    "DailyVitals",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Database connection failed:\n{ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown();
            }
        }
    }
}

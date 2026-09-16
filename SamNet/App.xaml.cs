// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using SharedToolbox;
using System.IO;
using System.Windows;

namespace SamNet
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Catch non-UI / background exceptions
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                try
                {
                    var ex = args.ExceptionObject as Exception;
                    File.WriteAllText(
                        Path.Combine(AppContext.BaseDirectory, "startup-error.txt"),
                        ex?.ToString() ?? args.ExceptionObject?.ToString() ?? "Unknown error");
                }
                catch { /* ignore logging failures */ }
            };

            // Catch UI-thread exceptions
            DispatcherUnhandledException += (s, args) =>
            {
                try
                {
                    File.WriteAllText(
                        Path.Combine(AppContext.BaseDirectory, "startup-error.txt"),
                        args.Exception.ToString());
                }
                catch { /* ignore */ }

                args.Handled = true; // prevents the default crash dialog
            };

            base.OnStartup(e);

            Application.Current.Resources["EventBus"] = new EdwardsMessageBus();

            Application.Current.MainWindow = new MainWindow();
            MainWindow? app = Application.Current.MainWindow as MainWindow;
            app!.DataContext = new MainWindowVM(app);
            app.Show();
        }
    }

}

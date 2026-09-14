// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using SharedToolbox;
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
            base.OnStartup(e);

            Application.Current.Resources["EventBus"] = new EdwardsMessageBus();

            Application.Current.MainWindow = new MainWindow();
            MainWindow? app = Application.Current.MainWindow as MainWindow;
            app!.DataContext = new MainWindowVM(app);
            app.Show();

        }

    }

}

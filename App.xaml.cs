using System.Windows;
using NLog;
using TelegramAutomation.Views;

namespace TelegramAutomation
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            LogManager.Setup().LoadConfigurationFromFile("nlog.config");
            
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}

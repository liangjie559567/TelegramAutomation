using System.Windows;
using NLog;

namespace TelegramAutomation
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 更新 NLog 配置方法
            LogManager.Setup().LoadConfigurationFromFile("nlog.config");
            
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}

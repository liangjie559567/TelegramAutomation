using System.Windows;

namespace TelegramAutomation
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化日志配置
            NLog.LogManager.LoadConfiguration("nlog.config");
            
            // 创建并显示主窗口
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}

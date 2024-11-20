using System.Windows;
using TelegramAutomation.Views;

namespace TelegramAutomation
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化主窗口
            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}

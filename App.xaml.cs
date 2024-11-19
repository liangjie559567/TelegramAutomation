using System.Windows;
using TelegramAutomation.Views;

namespace TelegramAutomation
{
    public partial class App : System.Windows.Application
    {
        public App()
        {
            InitializeComponent();
            
            // 初始化主窗口
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // 其他启动代码
        }
    }
}

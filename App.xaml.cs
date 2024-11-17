using System.Windows;
using NLog;

namespace TelegramAutomation
{
    public partial class App : System.Windows.Application
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                _logger.Info("应用程序启动");
                LogManager.Setup().LoadConfigurationFromFile("nlog.config");
                
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "应用程序启动失败");
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logger.Info("应用程序退出");
            LogManager.Shutdown();
            base.OnExit(e);
        }
    }
}

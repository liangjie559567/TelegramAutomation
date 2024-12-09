using System.Windows;

namespace TelegramAutomation
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 设置未捕获的异常处理器
            Current.DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"发生未处理的异常：{args.Exception.Message}", 
                    "错误", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
} 
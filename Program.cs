using NLog;
using System;
using System.Windows;
using System.Threading.Tasks;
using System.Diagnostics;
using MessageBox = System.Windows.MessageBox;

namespace TelegramAutomation
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // 初始化日志系统
                var logger = LogManager.GetCurrentClassLogger();
                logger.Info("应用程序启动");

                // 确保必要的配置文件存在
                if (!System.IO.File.Exists("appsettings.json"))
                {
                    MessageBox.Show(
                        "配置文件 appsettings.json 不存在", 
                        "错误", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error
                    );
                    return;
                }

                if (!System.IO.File.Exists("nlog.config"))
                {
                    MessageBox.Show(
                        "日志配置文件 nlog.config 不存在", 
                        "错误", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error
                    );
                    return;
                }

                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Error(ex, "应用程序启动失败");
                MessageBox.Show(
                    $"应用程序启动失败: {ex.Message}", 
                    "错误", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
            }
        }
    }
} 
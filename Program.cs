using NLog;
using System;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using System.Diagnostics;
using MessageBox = System.Windows.MessageBox;

namespace TelegramAutomation
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                ValidateConfigurations();
                var app = new App();
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

        private static void ValidateConfigurations()
        {
            if (!File.Exists("appsettings.json"))
            {
                throw new FileNotFoundException("配置文件 appsettings.json 不存在");
            }

            if (!File.Exists("nlog.config"))
            {
                throw new FileNotFoundException("日志配置文件 nlog.config 不存在");
            }
        }
    }
} 
using NLog;
using System;

namespace TelegramAutomation
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                LogManager.LoadConfiguration("nlog.config");
                var logger = LogManager.GetCurrentClassLogger();
                logger.Info("应用程序启动");

                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex, "应用程序启动失败");
                throw;
            }
        }
    }
} 
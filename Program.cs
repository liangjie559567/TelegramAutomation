using NLog;
using System;
using System.Windows;

namespace TelegramAutomation
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
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
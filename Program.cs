using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Threading;
using System.Threading.Tasks;
using TelegramAutomation.Models;
using TelegramAutomation.Services;

namespace TelegramAutomation
{
    public class Program
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public static async Task Main(string[] args)
        {
            try
            {
                // 确保日志目录存在
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TelegramAutomation",
                    "logs"
                );
                Directory.CreateDirectory(logPath);

                // 使用新的 NLog 配置方式
                var config = new LoggingConfiguration();

                // 添加控制台目标
                var consoleTarget = new ConsoleTarget("console")
                {
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}"
                };
                config.AddTarget(consoleTarget);
                config.AddRuleForAllLevels(consoleTarget);

                // 添加文件目标
                var logfile = new FileTarget("logfile")
                {
                    FileName = Path.Combine(logPath, "${shortdate}.log"),
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}"
                };
                config.AddTarget(logfile);
                config.AddRuleForAllLevels(logfile);

                // 添加调试文件目标
                var debugfile = new FileTarget("debugfile")
                {
                    FileName = Path.Combine(logPath, "debug_${shortdate}.log"),
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}"
                };
                config.AddTarget(debugfile);
                config.AddRuleForAllLevels(debugfile);

                // 应用配置
                LogManager.Configuration = config;

                _logger.Info("=== 程序启动 ===");
                _logger.Info($"日志目录: {logPath}");
                _logger.Info($"当前时间: {DateTime.Now}");
                _logger.Info($"操作系统: {Environment.OSVersion}");
                _logger.Info($"程序目录: {AppDomain.CurrentDomain.BaseDirectory}");

                // 加载配置
                var settings = AppSettings.Load();
                _logger.Info("配置加载成功");

                // 初始化 Chrome 服务
                using var chromeService = new ChromeService(settings);
                
                try
                {
                    // 初始化浏览器
                    _logger.Info("正在初始化浏览器...");
                    await chromeService.InitializeAsync();
                    _logger.Info("浏览器初始化成功");

                    // 等待用户操作
                    _logger.Info("程序运行中，按 Ctrl+C 退出...");
                    Console.WriteLine("程序运行中，按 Ctrl+C 退出...");
                    
                    using var cts = new CancellationTokenSource();
                    Console.CancelKeyPress += (s, e) => {
                        e.Cancel = true;
                        cts.Cancel();
                        _logger.Info("收到退出信号");
                    };

                    try
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            await Task.Delay(1000, cts.Token);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.Info("程序正常退出");
                    }
                }
                catch (ChromeException ex)
                {
                    _logger.Error(ex, $"Chrome 错误 ({ex.ErrorCode}): {ex.Message}");
                    Console.WriteLine($"Chrome 错误: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.Error(ex.InnerException, "详细错误信息");
                    }
                }

                _logger.Info("=== 程序结束 ===");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "程序运行出错");
                Console.WriteLine($"错误: {ex.Message}");
            }
        }
    }
} 
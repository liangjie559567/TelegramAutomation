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
                _logger.Info($"操��系统: {Environment.OSVersion}");
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

                    while (true)
                    {
                        try 
                        {
                            // 清空控制台缓冲区
                            while (Console.KeyAvailable)
                            {
                                Console.ReadKey(true);
                            }
                            
                            // 使用不同颜色突出显示输入提示
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n请输入要访问的频道名称 (无需加@符号，输入 'exit' 退出): ");
                            Console.ResetColor();
                            
                            // 确保输入流正常
                            var channelName = Console.ReadLine()?.Trim();
                            // 移除用户可能输入的 @ 符号
                            channelName = channelName?.TrimStart('@');
                            
                            if (string.IsNullOrEmpty(channelName) || channelName.ToLower() == "exit")
                            {
                                break;
                            }

                            // 开始下载频道内容
                            _logger.Info($"开始下载频道 {channelName} 的内容...");

                            var progress = new Progress<string>(message => {
                                Console.WriteLine(message);
                                _logger.Info(message);
                            });

                            await chromeService.NavigateToChannel(channelName);
                            await chromeService.StartDownloadingAsync(channelName);
                            
                            _logger.Info("下载完成");
                            Console.WriteLine("下载完成");

                            Console.WriteLine("是否继续下载其他频道? (y/n):");
                            var continueDownload = Console.ReadLine()?.ToLower();
                            if (continueDownload != "y")
                            {
                                break;
                            }
                        }
                        catch (ChromeException ex) when (ex.ErrorCode == ErrorCodes.CHANNEL_NOT_FOUND)
                        {
                            _logger.Warn($"未找到频道，请检查频道名称是否正确");
                            Console.WriteLine($"未找到频道，请检查频道名称是否正确");
                            // 继续循环，让用户重新输入
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "下载过程中出错");
                            Console.WriteLine($"错误: {ex.Message}");
                            Console.WriteLine("是否重试? (y/n):");
                            var retry = Console.ReadLine()?.ToLower();
                            if (retry != "y")
                            {
                                break;
                            }
                        }
                    }

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
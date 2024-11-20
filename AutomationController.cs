using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using NLog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using InputSimulatorStandard;
using InputSimulatorStandard.Native;
using TelegramAutomation.Models;
using TelegramAutomation.Services;
using TelegramAutomation.Exceptions;

namespace TelegramAutomation
{
    public class AutomationController
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IInputSimulator _inputSimulator;
        private readonly AppSettings _settings;
        private readonly ChromeService _chromeService;
        private IWebDriver _driver;
        private readonly string _downloadPath;
        private bool _isInitialized;
        private CancellationTokenSource? _cancellationTokenSource;

        public AutomationController(AppSettings settings, ChromeService chromeService)
        {
            _settings = settings;
            _chromeService = chromeService;
            _downloadPath = settings.DefaultSavePath;
            _inputSimulator = new InputSimulator();
            Directory.CreateDirectory(_downloadPath);
        }

        private async Task SimulateInput(string text)
        {
            foreach (var c in text)
            {
                _inputSimulator.Keyboard.TextEntry(c.ToString());
                await Task.Delay(50);
            }
        }

        private async Task PressEnter()
        {
            try
            {
                await Task.Run(() => {
                    _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(50);
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "按回车键失败");
                throw;
            }
        }

        // ... 其他代码保持不变 ...
    }
}

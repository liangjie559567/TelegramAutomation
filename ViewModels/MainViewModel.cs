using System;
using System.Windows.Input;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using TelegramAutomation.Commands;
using System.Windows.Forms;
using OpenQA.Selenium;
using NLog;
using TelegramAutomation.Models;
using TelegramAutomation.Services;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows.Media;
using System.Net.Http;
using System.ComponentModel;

namespace TelegramAutomation.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly AppSettings _settings;
        private readonly ChromeService _chromeService;
        private readonly AutomationController _controller;

        public MainViewModel()
        {
            _settings = AppSettings.Load();
            _chromeService = new ChromeService();
            _controller = new AutomationController(_settings, _chromeService);
            InitializeCommands();
        }

        public async Task Initialize()
        {
            try
            {
                await _controller.InitializeAsync();
                await CheckLoginStatus();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "初始化失败");
                throw;
            }
        }

        private async Task ExecuteCommand()
        {
            // 实现命令执行逻辑
        }
    }
}

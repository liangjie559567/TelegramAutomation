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
        private readonly ChromeService _chromeService;
        private readonly AppSettings _settings;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public MainViewModel()
        {
            _settings = AppSettings.Load();
            _chromeService = new ChromeService(_settings);
            InitializeCommands();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _chromeService.InitializeAsync();
                await CheckLoginStatus();
                UpdateNetworkStatus();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "初始化失败");
                Status = "初始化失败: " + ex.Message;
                StatusColor = Brushes.Red;
                throw;
            }
        }

        private void InitializeCommands()
        {
            // 实现命令初始化
        }

        private async Task CheckLoginStatus()
        {
            try
            {
                var isLoggedIn = await _chromeService.CheckLoginStatusAsync();
                UpdateLoginStatus(isLoggedIn);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检查登录状态失败");
                LoginStatusMessage = "登录状态检查失败";
                LoginStatusColor = Brushes.Red;
                throw;
            }
        }
    }
}

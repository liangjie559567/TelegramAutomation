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

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _networkStatusText = "检查网络...";
        public string NetworkStatusText
        {
            get => _networkStatusText;
            set => SetProperty(ref _networkStatusText, value);
        }

        private System.Windows.Media.Brush _networkStatusColor = System.Windows.Media.Brushes.Gray;
        public System.Windows.Media.Brush NetworkStatusColor
        {
            get => _networkStatusColor;
            set => SetProperty(ref _networkStatusColor, value);
        }

        private string _loginStatusMessage = string.Empty;
        public string LoginStatusMessage
        {
            get => _loginStatusMessage;
            set => SetProperty(ref _loginStatusMessage, value);
        }

        private System.Windows.Media.Brush _loginStatusColor = System.Windows.Media.Brushes.Gray;
        public System.Windows.Media.Brush LoginStatusColor
        {
            get => _loginStatusColor;
            set => SetProperty(ref _loginStatusColor, value);
        }

        private System.Windows.Media.Brush _statusColor = System.Windows.Media.Brushes.Gray;
        public System.Windows.Media.Brush StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

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
                StatusColor = System.Windows.Media.Brushes.Red;
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

        private void UpdateNetworkStatus()
        {
            try
            {
                var isConnected = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
                NetworkStatusText = isConnected ? "网络正常" : "网络异常";
                NetworkStatusColor = isConnected ? 
                    System.Windows.Media.Brushes.Green : 
                    System.Windows.Media.Brushes.Red;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "更新网络状态失败");
                NetworkStatusText = "网络状态未知";
                NetworkStatusColor = System.Windows.Media.Brushes.Gray;
            }
        }

        private void UpdateLoginStatus(bool isLoggedIn)
        {
            try
            {
                LoginStatusMessage = isLoggedIn ? "已登录" : "未登录";
                LoginStatusColor = isLoggedIn ? 
                    System.Windows.Media.Brushes.Green : 
                    System.Windows.Media.Brushes.Red;
                
                // 更新整体状态
                Status = isLoggedIn ? "登录成功" : "未登录";
                StatusColor = isLoggedIn ? 
                    System.Windows.Media.Brushes.Green : 
                    System.Windows.Media.Brushes.Red;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "更新登录状态失败");
                LoginStatusMessage = "状态更新失败";
                LoginStatusColor = System.Windows.Media.Brushes.Gray;
            }
        }
    }
}

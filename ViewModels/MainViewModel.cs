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

        private ICommand? _startCommand;
        public ICommand StartCommand => _startCommand ??= new RelayCommand(
            async param => await StartAutomation(),
            param => !IsRunning
        );

        private ICommand? _stopCommand;
        public ICommand StopCommand => _stopCommand ??= new RelayCommand(
            async param => await StopAutomation(),
            param => IsRunning
        );

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        public MainViewModel()
        {
            try 
            {
                // 先加载配置
                _settings = AppSettings.Load();
                if (_settings == null)
                {
                    throw new InvalidOperationException("无法加载应用程序配置");
                }

                // 初始化服务
                _chromeService = new ChromeService(_settings);
                
                // 初始化命令
                InitializeCommands();

                // 设置初始状态
                Status = "正在初始化...";
                StatusColor = System.Windows.Media.Brushes.Gray;
                NetworkStatusText = "检查网络...";
                NetworkStatusColor = System.Windows.Media.Brushes.Gray;
                LoginStatusMessage = "未登录";
                LoginStatusColor = System.Windows.Media.Brushes.Gray;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "MainViewModel 初始化失败");
                Status = "初始化失败: " + ex.Message;
                StatusColor = System.Windows.Media.Brushes.Red;
                throw;
            }
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
                LoginStatusColor = System.Windows.Media.Brushes.Red;
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

        private async Task StartAutomation()
        {
            try
            {
                IsRunning = true;
                Status = "正在运行...";
                StatusColor = System.Windows.Media.Brushes.Green;
                
                // TODO: 实现自动化逻辑
                await _chromeService.InitializeAsync();
                
                await Task.Delay(100); // 防止界面卡顿
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "自动化执行失败");
                Status = "执行失败: " + ex.Message;
                StatusColor = System.Windows.Media.Brushes.Red;
            }
        }

        private async Task StopAutomation()
        {
            try
            {
                IsRunning = false;
                Status = "已停止";
                StatusColor = System.Windows.Media.Brushes.Gray;
                
                // TODO: 实现停止逻辑
                await Task.Delay(100); // 防止界面卡顿
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "停止自动化失败");
                Status = "停止失败: " + ex.Message;
                StatusColor = System.Windows.Media.Brushes.Red;
            }
        }
    }
}

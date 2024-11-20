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
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly AutomationController _controller;
        private readonly StringBuilder _logBuilder;
        private CancellationTokenSource? _cancellationTokenSource;

        private string _channelUrl = string.Empty;
        private string _savePath = string.Empty;
        private string _logContent = string.Empty;
        private string _status = string.Empty;
        private bool _isRunning;
        private DateTime _lastLogUpdate;
        private string _phoneNumber = string.Empty;
        private string _verificationCode = string.Empty;
        private bool _isLoggedIn;
        private bool _isRequestingCode;
        private bool _isLoggingIn;
        private string _statusMessage = "请登录";
        private string _loginStatus = string.Empty;
        private string _loginStatusMessage = string.Empty;
        private bool _isInitializing;

        private readonly Dictionary<string, string> _errorMessages = new()
        {
            { "PHONE_NUMBER_INVALID", "无效的手机号码格式" },
            { "PHONE_NUMBER_BANNED", "该手机号已被封禁" },
            { "PHONE_CODE_INVALID", "验证码错误" },
            { "PHONE_CODE_EXPIRED", "验证码已过期" },
            { "FLOOD_WAIT", "请求过于频繁，请稍后再试" },
            { "NETWORK_ERROR", "网络连接错误，请检查网络" },
            { "SESSION_EXPIRED", "登录会话已过期，请重新登录" },
            { "CHROME_NOT_FOUND", "未找到Chrome浏览器，请先安装" },
            { "CHROME_VERSION_MISMATCH", "Chrome版本不兼容，请更新" }
        };

        private SolidColorBrush _loginStatusColor = new(Colors.Black);
        private int _reconnectAttempts = 0;
        private readonly int MAX_RECONNECT_ATTEMPTS = 3;
        private readonly int[] RECONNECT_DELAYS = { 2000, 5000, 10000 };
        private SolidColorBrush _statusColor = new(Colors.Gray);
        private SolidColorBrush _networkStatusColor = new(Colors.Gray);

        public SolidColorBrush LoginStatusColor
        {
            get => _loginStatusColor;
            set => SetProperty(ref _loginStatusColor, value);
        }

        public SolidColorBrush StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public SolidColorBrush NetworkStatusColor
        {
            get => _networkStatusColor;
            set => SetProperty(ref _networkStatusColor, value);
        }

        public MainViewModel()
        {
            _controller = new AutomationController();
            _logBuilder = new StringBuilder();
            
            StartCommand = new RelayCommand(StartAutomation, CanStartAutomation);
            StopCommand = new RelayCommand(StopAutomation, () => IsRunning);
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            RequestCodeCommand = new RelayCommand(async () => await RequestVerificationCode(), 
                () => !string.IsNullOrWhiteSpace(PhoneNumber) && !IsRequestingCode);
            LoginCommand = new RelayCommand(async () => await Login(), 
                () => !string.IsNullOrWhiteSpace(VerificationCode) && !IsLoggingIn);

            // 设置默认保存路径
            SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TelegramDownloads");
            _lastLogUpdate = DateTime.Now;

            // 添加初始化任务
            _ = InitializeAsync();

            // 启动网络监控
            _ = CheckNetworkStatus();
        }

        public string ChannelUrl
        {
            get => _channelUrl;
            set
            {
                if (SetProperty(ref _channelUrl, value))
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string SavePath
        {
            get => _savePath;
            set
            {
                if (SetProperty(ref _savePath, value))
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string LogContent
        {
            get => _logContent;
            private set => SetProperty(ref _logContent, value);
        }

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                {
                    RequestCodeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string VerificationCode
        {
            get => _verificationCode;
            set
            {
                if (SetProperty(ref _verificationCode, value))
                {
                    LoginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            private set
            {
                if (SetProperty(ref _isLoggedIn, value))
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool IsRequestingCode
        {
            get => _isRequestingCode;
            set
            {
                if (SetProperty(ref _isRequestingCode, value))
                {
                    RequestCodeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set
            {
                if (SetProperty(ref _isLoggingIn, value))
                {
                    LoginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string LoginStatus
        {
            get => _loginStatus;
            set => SetProperty(ref _loginStatus, value);
        }

        public string LoginStatusMessage
        {
            get => _loginStatusMessage;
            set => SetProperty(ref _loginStatusMessage, value);
        }

        public bool IsInitializing
        {
            get => _isInitializing;
            set
            {
                if (SetProperty(ref _isInitializing, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand RequestCodeCommand { get; }
        public ICommand LoginCommand { get; }

        private bool CanStartAutomation()
        {
            return !IsRunning && IsLoggedIn && !string.IsNullOrWhiteSpace(ChannelUrl) && !string.IsNullOrWhiteSpace(SavePath);
        }

        private bool ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(ChannelUrl))
            {
                AddLog("错误: 请输入Telegram频道URL");
                return false;
            }

            if (!Uri.TryCreate(ChannelUrl, UriKind.Absolute, out _))
            {
                AddLog("错误: 请输入有效的URL");
                return false;
            }

            if (string.IsNullOrWhiteSpace(SavePath))
            {
                AddLog("错误: 请选择保存路径");
                return false;
            }

            if (!Directory.Exists(SavePath))
            {
                try
                {
                    Directory.CreateDirectory(SavePath);
                }
                catch (Exception ex)
                {
                    AddLog($"错误: 无法创建保存目录: {ex.Message}");
                    return false;
                }
            }

            return true;
        }

        private async void StartAutomation()
        {
            if (!ValidateConfiguration())
                return;

            try
            {
                IsRunning = true;
                Status = "正在初始化...";
                AddLog("正在启动自动化任务...");
                
                _cancellationTokenSource = new CancellationTokenSource();

                var progress = new Progress<string>(message =>
                {
                    if (_lastLogUpdate.AddMilliseconds(100) < DateTime.Now)
                    {
                        AddLog(message);
                        Status = message;
                        _lastLogUpdate = DateTime.Now;
                    }
                });

                await Task.Run(() => _controller.StartAutomation(
                    ChannelUrl, 
                    SavePath, 
                    progress, 
                    _cancellationTokenSource.Token
                ));

                Status = "任务完成";
                AddLog("自动化任务完成");
            }
            catch (OperationCanceledException)
            {
                Status = "已停止";
                AddLog("任务已停止");
            }
            catch (WebDriverException ex)
            {
                AddLog($"浏览器操作失败: {ex.Message}");
                AddLog("请确保已安装最新版本的Chrome浏览器");
                Status = "任务失败";
                _logger.Error(ex, "浏览器操作失败");
            }
            catch (Exception ex)
            {
                AddLog($"任务失败: {ex.Message}");
                Status = "任务失败";
                _logger.Error(ex, "自动化任务失败");
            }
            finally
            {
                IsRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void StopAutomation()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _controller.Stop();
                Status = "正在停止...";
                AddLog("正在停止任务...");
            }
            catch (Exception ex)
            {
                AddLog($"停止任务时出错: {ex.Message}");
                _logger.Error(ex, "停止任务失败");
            }
        }

        private void BrowseFolder()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择下载文件保存位置",
                UseDescriptionForTitle = true,
                SelectedPath = SavePath
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SavePath = dialog.SelectedPath;
            }
        }

        private void AddLog(string message)
        {
            _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            LogContent = _logBuilder.ToString();
        }

        private bool ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // 添加国际手机号格式验证
            var phoneRegex = new Regex(@"^\+[1-9]\d{1,14}$");
            return phoneRegex.IsMatch(phoneNumber);
        }

        private async Task RequestVerificationCode()
        {
            if (!ValidatePhoneNumber(PhoneNumber))
            {
                LoginStatusMessage = "请输入正确的手机号码格式（如：+8613800138000）";
                return;
            }

            try
            {
                IsRequestingCode = true;
                LoginStatusMessage = "正在发送验证码...";

                await _controller.RequestVerificationCode(PhoneNumber);
                LoginStatusMessage = "验证码已发送，请查收";
                _logger.Info($"验证码已发送到 {PhoneNumber}");
            }
            catch (Exception ex)
            {
                HandleError(ex, "发送验证码失败");
            }
            finally
            {
                IsRequestingCode = false;
            }
        }

        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(VerificationCode))
            {
                LoginStatusMessage = "请输入验证码";
                LoginStatusColor = new SolidColorBrush(Colors.Red);
                return;
            }

            try
            {
                IsLoggingIn = true;
                LoginStatusMessage = "正在登录...";
                LoginStatusColor = new SolidColorBrush(Colors.Black);

                await _controller.LoginWithRetry(PhoneNumber, VerificationCode);
                
                IsLoggedIn = true;
                LoginStatusMessage = "登录成功";
                LoginStatusColor = new SolidColorBrush(Colors.Green);
                _logger.Info("登录成功");

                // 清空验证码
                VerificationCode = string.Empty;
            }
            catch (Exception ex)
            {
                HandleError(ex, "登录失败");
                IsLoggedIn = false;
                
                // 尝试自动重连
                if (ex.Message.Contains("SESSION_EXPIRED") || 
                    ex.Message.Contains("NETWORK_ERROR"))
                {
                    await AutoReconnect();
                }
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                IsInitializing = true;
                LoginStatusMessage = "正在初始化...";

                // 尝试恢复会话
                var sessionRestored = await _controller.InitializeAsync();
                if (sessionRestored)
                {
                    IsLoggedIn = true;
                    LoginStatusMessage = "已恢复登录状态";
                    _logger.Info("成功恢复登录会话");
                }
                else
                {
                    LoginStatusMessage = "请登录";
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "初始化失败");
            }
            finally
            {
                IsInitializing = false;
            }
        }

        private void HandleError(Exception ex, string defaultMessage)
        {
            string errorMessage = defaultMessage;
            LoginStatusColor = new SolidColorBrush(Colors.Red);

            // 检查是否是已知错误
            foreach (var pair in _errorMessages)
            {
                if (ex.Message.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = pair.Value;
                    break;
                }
            }

            // 记录错误
            _logger.Error(ex, errorMessage);
            LoginStatusMessage = $"{errorMessage}: {ex.Message}";

            // 特殊错误处理
            if (ex.Message.Contains("SESSION_EXPIRED"))
            {
                IsLoggedIn = false;
                // 清理会话数据
                _ = _controller.ClearSession();
            }
            else if (ex.Message.Contains("NETWORK_ERROR"))
            {
                // 网络错误时尝试自动重连
                _ = AutoReconnect();
            }
        }

        private void UpdateCommandStates()
        {
            (RequestCodeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource?.Dispose();
                    _controller?.Dispose();
                    // 清理其他托管资源
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task AutoReconnect()
        {
            while (_reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
            {
                try
                {
                    LoginStatusMessage = $"正在尝试重新连接... ({_reconnectAttempts + 1}/{MAX_RECONNECT_ATTEMPTS})";
                    LoginStatusColor = new SolidColorBrush(Colors.Orange);

                    await Task.Delay(RECONNECT_DELAYS[_reconnectAttempts]);
                    await _controller.InitializeAsync();

                    if (await _controller.VerifyLoginStatusComprehensive())
                    {
                        IsLoggedIn = true;
                        LoginStatusMessage = "重新连接成功";
                        LoginStatusColor = new SolidColorBrush(Colors.Green);
                        _reconnectAttempts = 0;
                        return;
                    }

                    _reconnectAttempts++;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"重新连接失败 (尝试 {_reconnectAttempts + 1}/{MAX_RECONNECT_ATTEMPTS})");
                    _reconnectAttempts++;
                }
            }

            LoginStatusMessage = "重新连接失败，请手动重新登录";
            LoginStatusColor = new SolidColorBrush(Colors.Red);
            IsLoggedIn = false;
            _reconnectAttempts = 0;
        }

        // 添加网络状态监控
        private bool _isNetworkAvailable = true;
        public bool IsNetworkAvailable
        {
            get => _isNetworkAvailable;
            private set
            {
                if (SetProperty(ref _isNetworkAvailable, value))
                {
                    if (!value && IsLoggedIn)
                    {
                        LoginStatusMessage = "网络连接已断开，等待重连...";
                        LoginStatusColor = new SolidColorBrush(Colors.Orange);
                        _ = AutoReconnect();
                    }
                }
            }
        }

        // 添加网络状态检查
        private async Task CheckNetworkStatus()
        {
            while (!_disposed)
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    await client.GetAsync("https://www.google.com");
                    IsNetworkAvailable = true;
                }
                catch
                {
                    IsNetworkAvailable = false;
                }

                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }
}

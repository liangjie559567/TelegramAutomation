using System;
using System.Windows.Input;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
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
using Microsoft.Extensions.Configuration;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace TelegramAutomation.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly ChromeService? _chromeService;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;
        
        private string _phoneNumber = string.Empty;
        public string PhoneNumber 
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                {
                    UpdateCanRequestCode();
                }
            }
        }

        private string _verificationCode = string.Empty;
        public string VerificationCode
        {
            get => _verificationCode;
            set
            {
                if (SetProperty(ref _verificationCode, value))
                {
                    UpdateCanLogin();
                }
            }
        }

        private bool _canRequestCode;
        public bool CanRequestCode
        {
            get => _canRequestCode;
            private set => SetProperty(ref _canRequestCode, value);
        }

        private bool _canLogin;
        public bool CanLogin
        {
            get => _canLogin;
            private set => SetProperty(ref _canLogin, value);
        }

        // 添加验证码请求频率限制
        private DateTime _lastRequestTime = DateTime.MinValue;
        private const int REQUEST_COOLDOWN_SECONDS = 60;
        
        public MainViewModel()
        {
            try
            {
                var settings = LoadAppSettingsFromJson();
                if (settings != null)
                {
                    _chromeService = new ChromeService(settings);
                    
                    // 初始化所有命令
                    LoginCommand = new RelayCommand(async _ => await ExecuteLoginCommandAsync());
                    StopCommand = new RelayCommand(_ => ExecuteStopCommand());
                    RetryCommand = new RelayCommand(async _ => await ExecuteRetryCommandAsync());
                    RequestCodeCommand = new RelayCommand(async _ => await ExecuteRequestCodeCommandAsync());
                }
                else
                {
                    throw new TelegramAutomationException(
                        "无法加载应用程序配置",
                        ErrorCodes.CONFIG_ERROR
                    );
                }
            }
            catch (Exception ex)
            {
                HandleInitializationError(ex);
            }
        }

        private AppSettings? LoadAppSettingsFromJson()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var settings = config.Get<AppSettings>();
                if (settings == null)
                {
                    throw new TelegramAutomationException(
                        "无法加载应用程序配置",
                        ErrorCodes.CONFIG_ERROR
                    );
                }

                return settings;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载配置文件失败");
                throw new TelegramAutomationException(
                    "加载配置文件失败",
                    ErrorCodes.CONFIG_ERROR,
                    ex
                );
            }
        }

        private async Task CheckNetworkStatusAsync()
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync("https://www.google.com");
                if (!response.IsSuccessStatusCode)
                {
                    throw new TelegramAutomationException(
                        "网络连接异常",
                        ErrorCodes.NETWORK_ERROR
                    );
                }
            }
            catch (Exception ex)
            {
                throw new TelegramAutomationException(
                    "网络连接失败，请检查网络设置",
                    ErrorCodes.NETWORK_ERROR,
                    ex
                );
            }
        }

        private void ExecuteStopCommand()
        {
            try
            {
                _cts.Cancel();
                Status = "操作已取消";
                StatusColor = System.Windows.Media.Brushes.Orange;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "停止操作失败");
            }
        }

        private async Task ExecuteRetryCommandAsync()
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                cts.CancelAfter(TimeSpan.FromMinutes(1));
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                HandleInitializationError(ex);
            }
        }

        private async Task ExecuteLoginCommandAsync()
        {
            if (_disposed)
            {
                _logger.Warn("尝试在已释放的实例上执行登录");
                return;
            }

            try
            {
                if (_chromeService == null)
                {
                    throw new TelegramAutomationException(
                        "Chrome服务未初始化",
                        ErrorCodes.INITIALIZATION_ERROR
                    );
                }

                IsLoading = true;
                Status = "正在初始化Chrome...";

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                cts.CancelAfter(TimeSpan.FromMinutes(2));

                await ValidateAndInitializeChromeAsync(cts.Token);
                
                Status = "登录成功";
                StatusColor = System.Windows.Media.Brushes.Green;
                IsLoggedIn = true;
            }
            catch (OperationCanceledException)
            {
                Status = "操作已取消";
                StatusColor = System.Windows.Media.Brushes.Orange;
            }
            catch (Exception ex)
            {
                HandleLoginError(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ValidateAndInitializeChromeAsync(CancellationToken token)
        {
            if (_chromeService == null) return;

            if (!_chromeService.ValidateChromeEnvironment())
            {
                throw new ChromeException(
                    "Chrome环境验证失败",
                    ErrorCodes.CHROME_ENVIRONMENT_ERROR
                );
            }

            await _chromeService.InitializeAsync();
        }

        private void HandleInitializationError(Exception ex)
        {
            _logger.LogException(ex, "初始化失败");
            Status = "初始化失败: " + ex.Message;
            StatusColor = System.Windows.Media.Brushes.Red;
            MessageBox.Show(
                ex.Message,
                "初始化错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void HandleLoginError(Exception ex)
        {
            string errorMessage;
            string errorCode = ErrorCodes.UNKNOWN_ERROR;

            if (ex is ChromeDriverException chromeEx)
            {
                errorCode = chromeEx.ErrorCode;
                errorMessage = "Chrome驱动初始化失败: " + chromeEx.Message;
            }
            else if (ex is LoginException loginEx)
            {
                errorCode = loginEx.ErrorCode;
                errorMessage = "登录失败: " + loginEx.Message;
            }
            else
            {
                errorMessage = "发生未知错误: " + ex.Message;
            }

            _logger.Error(ex, $"错误代码: {errorCode}, 位置: HandleLoginError at {ex.StackTrace}, 消息: {errorMessage}");
            Status = errorMessage;
            StatusColor = System.Windows.Media.Brushes.Red;
            IsLoading = false;
        }

        public async Task InitializeAsync()
        {
            try
            {
                Status = "正在初始化...";
                StatusColor = System.Windows.Media.Brushes.Gray;

                // 检查网络状态
                await CheckNetworkStatusAsync();

                Status = "初始化完成";
                StatusColor = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "初始化失败");
                Status = "初始化失败: " + ex.Message;
                StatusColor = System.Windows.Media.Brushes.Red;
                MessageBox.Show(
                    ex.Message,
                    "初始化错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _cts.Cancel();
            _cts.Dispose();
            _chromeService?.Dispose();
            
            _disposed = true;
        }

        // 属性定义
        public ICommand? LoginCommand { get; private set; }
        public ICommand? StopCommand { get; private set; }
        public ICommand? RetryCommand { get; private set; }
        public ICommand? RequestCodeCommand { get; private set; }
        public ICommand? StartCommand { get; private set; }
        public ICommand? PauseCommand { get; private set; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        private string _status = "就绪";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private System.Windows.Media.Brush _statusColor = System.Windows.Media.Brushes.Gray;
        public System.Windows.Media.Brush StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => SetProperty(ref _isLoggedIn, value);
        }

        // 添加手机号格式化
        private string FormatPhoneNumber(string phone)
        {
            // 移除所有非数字字符
            var numbers = new string(phone.Where(c => char.IsDigit(c)).ToArray());
            
            // 如果没有国际区号，默认添加+86
            if (!phone.StartsWith("+"))
            {
                return numbers.Length <= 11 ? $"+86{numbers}" : $"+{numbers}";
            }
            return $"+{numbers}";
        }

        private async Task ExecuteRequestCodeCommandAsync()
        {
            try
            {
                // 验证手机号格式
                if (!Regex.IsMatch(PhoneNumber, @"^\+?[1-9]\d{1,14}$"))
                {
                    MessageBox.Show("请输入正确的手机号码格式\n例如: +8613800138000", 
                        "格式错误", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                    return;
                }

                // 检查请求频率
                var timeSinceLastRequest = DateTime.Now - _lastRequestTime;
                if (timeSinceLastRequest.TotalSeconds < REQUEST_COOLDOWN_SECONDS)
                {
                    var remainingSeconds = REQUEST_COOLDOWN_SECONDS - (int)timeSinceLastRequest.TotalSeconds;
                    MessageBox.Show($"请求过于频繁，请等待 {remainingSeconds} 秒后重试", 
                        "提示", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                    return;
                }

                IsLoading = true;
                Status = "正在初始化浏览器...";
                StatusColor = System.Windows.Media.Brushes.Gray;

                if (_chromeService == null) throw new InvalidOperationException("Chrome服务未初始化");

                // 格式化手机号
                var formattedPhone = FormatPhoneNumber(PhoneNumber);
                
                Status = "正在打开 Telegram Web...";
                await _chromeService.InitializeAsync();
                
                Status = "正在请求验证码...";
                await _chromeService.RequestVerificationCode(formattedPhone);
                
                _lastRequestTime = DateTime.Now;
                Status = "验证码已发送，请注意查收";
                StatusColor = System.Windows.Media.Brushes.Green;

                // 自动聚焦到验证码输入框
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        var verificationCodeBox = mainWindow.FindName("VerificationCodeBox") as System.Windows.Controls.TextBox;
                        verificationCodeBox?.Focus();
                    }
                });
            }
            catch (ChromeException ex)
            {
                HandleError(ex, "Chrome 浏览器错误");
            }
            catch (LoginException ex)
            {
                HandleError(ex, "登录错误");
            }
            catch (Exception ex)
            {
                HandleError(ex, "请求验证码失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void HandleError(Exception ex, string context)
        {
            var errorCode = (ex as TelegramAutomationException)?.ErrorCode ?? ErrorCodes.UNKNOWN_ERROR;
            var errorMessage = $"{context}: {ex.Message}";
            
            _logger.Error(ex, $"错误代码: {errorCode}, 上下文: {context}, 消息: {ex.Message}");
            Status = errorMessage;
            StatusColor = System.Windows.Media.Brushes.Red;
            
            MessageBox.Show(
                errorMessage,
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void UpdateCanRequestCode()
        {
            CanRequestCode = !IsLoading && 
                            !string.IsNullOrWhiteSpace(PhoneNumber) && 
                            Regex.IsMatch(PhoneNumber, @"^\+\d{11,15}$");
        }

        private void UpdateCanLogin()
        {
            CanLogin = !IsLoading && 
                      !string.IsNullOrWhiteSpace(PhoneNumber) && 
                      !string.IsNullOrWhiteSpace(VerificationCode) &&
                      VerificationCode.Length >= 5;
        }

        // 添加命令状态更新方法
        private void UpdateCommandStates()
        {
            UpdateCanRequestCode();
            UpdateCanLogin();
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanPause));
            OnPropertyChanged(nameof(CanStop));
        }

        // 添加命令状态属性
        public bool CanStart => IsLoggedIn && !IsLoading;
        public bool CanPause => IsLoggedIn && IsLoading;
        public bool CanStop => IsLoggedIn;
    }
}

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
                    (RequestCodeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string VerificationCode
        {
            get => _verificationCode;
            set => SetProperty(ref _verificationCode, value);
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
                AddLog("正在启动Chrome浏览器...");
                
                _cancellationTokenSource = new CancellationTokenSource();

                var progress = new Progress<string>(message =>
                {
                    if (_lastLogUpdate.AddMilliseconds(100) < DateTime.Now)
                    {
                        AddLog(message);
                        _lastLogUpdate = DateTime.Now;
                    }
                });

                await Task.Run(() => _controller.StartAutomation(
                    ChannelUrl, 
                    SavePath, 
                    progress, 
                    _cancellationTokenSource.Token
                ));
            }
            catch (WebDriverException ex)
            {
                AddLog($"浏览器启动失败: {ex.Message}");
                AddLog("请确保已安装最新版本的Chrome浏览器");
                Status = "启动失败";
            }
            catch (Exception ex)
            {
                AddLog($"发生错误: {ex.Message}");
                Status = "出错";
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
            _cancellationTokenSource?.Cancel();
            _controller.Stop();
            Status = "正在停止...";
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

        private async Task RequestVerificationCode()
        {
            try
            {
                IsRequestingCode = true;
                AddLog($"正在发送验证码到 {PhoneNumber}...");
                
                await _controller.InitializeBrowser();
                await _controller.NavigateToTelegram();
                await _controller.RequestVerificationCode(PhoneNumber);
                
                AddLog("验证码已发送，请查收");
            }
            catch (Exception ex)
            {
                AddLog($"发送验证码失败: {ex.Message}");
                _logger.Error(ex, "发送验证码失败");
            }
            finally
            {
                IsRequestingCode = false;
            }
        }

        private async Task Login()
        {
            try
            {
                IsLoggingIn = true;
                AddLog("正在登录...");
                
                await _controller.Login(PhoneNumber, VerificationCode);
                IsLoggedIn = true;
                
                AddLog("登录成功");
            }
            catch (Exception ex)
            {
                AddLog($"登录失败: {ex.Message}");
                _logger.Error(ex, "登录失败");
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _controller?.Dispose();
        }
    }
}

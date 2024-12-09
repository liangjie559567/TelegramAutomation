using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TelegramAutomation.Models;
using TelegramAutomation.Services;
using TelegramAutomation.Views;
using NLog;

namespace TelegramAutomation.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private ChromeService _chromeService;
        private AppSettings _settings;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private string _channelName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ChannelInfo> _channelList = new();

        [ObservableProperty]
        private ChannelInfo? _selectedChannel;

        [ObservableProperty]
        private int _messageCount = 10;

        [ObservableProperty]
        private string _savePath;

        [ObservableProperty]
        private ObservableCollection<DownloadItemViewModel> _downloadItems = new();

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private string _version = "v2.3.1";

        [ObservableProperty]
        private bool _isLoggedIn = false;

        public MainViewModel()
        {
            _settings = AppSettings.Load();
            _savePath = _settings.DefaultSavePath;
            _chromeService = new ChromeService(_settings);
        }

        [RelayCommand]
        private async Task Login()
        {
            try
            {
                StatusMessage = "正在登录...";
                await _chromeService.InitializeAsync();
                IsLoggedIn = true;
                StatusMessage = "登录成功";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "登录失败");
                StatusMessage = $"登录失败: {ex.Message}";
                MessageBox.Show($"登录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = Application.Current.MainWindow
            };

            if (settingsWindow.ShowDialog() == true)
            {
                // 重新加载设置
                _settings = AppSettings.Load();
                SavePath = _settings.DefaultSavePath;
                
                // 更新下载管理器配置
                if (_chromeService != null)
                {
                    // TODO: 更新下载服务配置
                }
            }
        }

        [RelayCommand]
        private async Task StartDownload()
        {
            if (!IsLoggedIn)
            {
                MessageBox.Show("请先登录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(ChannelName))
            {
                MessageBox.Show("请输入频道名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                StatusMessage = "正在下载...";
                DownloadItems.Clear();

                if (_chromeService.Driver == null)
                {
                    throw new InvalidOperationException("浏览器未初始化");
                }

                // 创建下载配置
                var downloadConfig = new DownloadConfiguration
                {
                    MaxConcurrentDownloads = _settings.DownloadConfig.MaxConcurrentDownloads,
                    SaveMessageText = true,
                    SaveLinks = true,
                    SupportedFileExtensions = _settings.DownloadConfig.SupportedFileExtensions
                };

                // 创建下载服务
                var downloadService = new DownloadService(_chromeService.Driver, SavePath, downloadConfig);

                // 创建进度报告处理器
                var progress = new Progress<(string FileName, string FileSize, double Progress, string Status, string Message)>(
                    update =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var item = DownloadItems.FirstOrDefault(x => x.FileName == update.FileName);
                            if (item == null)
                            {
                                item = new DownloadItemViewModel
                                {
                                    FileName = update.FileName,
                                    FileSize = update.FileSize
                                };
                                DownloadItems.Add(item);
                            }

                            item.Progress = update.Progress;
                            item.Status = update.Status;
                            StatusMessage = update.Message;
                        });
                    });

                // 开始下载
                await downloadService.ProcessChannelMessages(
                    ChannelName,
                    new Progress<string>(message => StatusMessage = message),
                    _cancellationTokenSource.Token,
                    progress
                );

                StatusMessage = "下载完成";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "下载已取消";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "下载失败");
                StatusMessage = $"下载失败: {ex.Message}";
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private void StopDownload()
        {
            _cancellationTokenSource?.Cancel();
            StatusMessage = "正在取消下载...";
        }
    }

    public partial class ChannelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public partial class DownloadItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _fileSize = string.Empty;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private string _status = string.Empty;
    }
} 
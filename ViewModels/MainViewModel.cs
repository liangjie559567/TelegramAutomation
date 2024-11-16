using System.Windows.Input;
using Microsoft.Win32;
using System.Text;
using System.Threading;
using System.IO;

namespace TelegramAutomation.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly AutomationController _controller;
        private readonly StringBuilder _logBuilder;
        private CancellationTokenSource _cancellationTokenSource;

        private string _channelUrl;
        private string _savePath;
        private string _logContent;
        private string _status;
        private bool _isRunning;

        public MainViewModel()
        {
            _controller = new AutomationController();
            _logBuilder = new StringBuilder();
            
            StartCommand = new RelayCommand(StartAutomation, CanStartAutomation);
            StopCommand = new RelayCommand(StopAutomation, () => IsRunning);
            BrowseFolderCommand = new RelayCommand(BrowseFolder);

            // 设置默认保存路径
            SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TelegramDownloads");
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

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand BrowseFolderCommand { get; }

        private bool CanStartAutomation()
        {
            return !IsRunning && !string.IsNullOrWhiteSpace(ChannelUrl) && !string.IsNullOrWhiteSpace(SavePath);
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
                Status = "正在运行...";
                _cancellationTokenSource = new CancellationTokenSource();

                var progress = new Progress<string>(message =>
                {
                    if (_lastLogUpdate.AddMilliseconds(100) < DateTime.Now)
                    {
                        _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                        LogContent = _logBuilder.ToString();
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
            catch (OperationCanceledException)
            {
                AddLog("操作已取消");
            }
            catch (Exception ex)
            {
                AddLog($"错误: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                Status = "已停止";
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
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择下载文件保存位置",
                UseDescriptionForTitle = true,
                SelectedPath = SavePath
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SavePath = dialog.SelectedPath;
            }
        }

        private void AddLog(string message)
        {
            _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            LogContent = _logBuilder.ToString();
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _controller?.Dispose();
        }
    }
} 
using System;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using System.Threading;
using System.Collections.ObjectModel;
using TelegramAutomation.Models;
using TelegramAutomation.ViewModels;

namespace TelegramAutomation.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            InitializeEventHandlers();
        }

        private void InitializeEventHandlers()
        {
            // 手机号码输入框
            PhoneNumberTextBox.TextChanged += (s, e) =>
            {
                // 可以在这里添加输入验证逻辑
                GetCodeButton.IsEnabled = !string.IsNullOrWhiteSpace(PhoneNumberTextBox.Text);
            };

            // 验证码输入框
            VerificationCodeTextBox.TextChanged += (s, e) =>
            {
                // 可以在这里添加输入验证逻辑
                LoginButton.IsEnabled = !string.IsNullOrWhiteSpace(VerificationCodeTextBox.Text);
            };

            // 保存路径输入框
            SavePathTextBox.TextChanged += (s, e) =>
            {
                // 可以在这里添加路径验证逻辑
                StartButton.IsEnabled = !string.IsNullOrWhiteSpace(SavePathTextBox.Text) 
                    && !string.IsNullOrWhiteSpace(ChannelLinkTextBox.Text);
            };

            // 获取验证码按钮
            GetCodeButton.Click += async (s, e) =>
            {
                try
                {
                    GetCodeButton.IsEnabled = false;
                    StatusText.Text = "正在发送验证码...";
                    
                    string phoneNumber = PhoneNumberTextBox.Text.Trim();
                    if (string.IsNullOrEmpty(phoneNumber))
                    {
                        MessageBox.Show("请输入手机号码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await _controller.InitializeBrowser();
                    await _controller.NavigateToTelegram();
                    await _controller.RequestVerificationCode(phoneNumber);
                    
                    StatusText.Text = "验证码已发送";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"发送验证码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "发送验证码失败";
                }
                finally
                {
                    GetCodeButton.IsEnabled = true;
                }
            };

            // 登录按钮
            LoginButton.Click += async (s, e) =>
            {
                try
                {
                    LoginButton.IsEnabled = false;
                    StatusText.Text = "正在登录...";
                    
                    string phoneNumber = PhoneNumberTextBox.Text.Trim();
                    string code = VerificationCodeTextBox.Text.Trim();
                    
                    if (string.IsNullOrEmpty(code))
                    {
                        MessageBox.Show("请输入验证码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await _controller.Login(phoneNumber, code);
                    StatusText.Text = "登录成功";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"登录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "登录失败";
                }
                finally
                {
                    LoginButton.IsEnabled = true;
                }
            };

            // 浏览按钮
            BrowseButton.Click += (s, e) =>
            {
                var dialog = new FolderBrowserDialog
                {
                    Description = "选择下载文件保存位置",
                    UseDescriptionForTitle = true
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SavePathTextBox.Text = dialog.SelectedPath;
                }
            };

            // 开始下载按钮
            StartButton.Click += async (s, e) =>
            {
                try
                {
                    StartButton.IsEnabled = false;
                    StatusText.Text = "正在下载...";
                    
                    string channelLink = ChannelLinkTextBox.Text.Trim();
                    string savePath = SavePathTextBox.Text.Trim();
                    
                    if (string.IsNullOrEmpty(channelLink) || string.IsNullOrEmpty(savePath))
                    {
                        MessageBox.Show("请输入频道链接和保存路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 创建下载项
                    var downloadItem = new DownloadItem
                    {
                        FileName = $"文件_{DateTime.Now:yyyyMMddHHmmss}",
                        FileSize = 1024 * 1024, // 1MB 示例
                        Status = "准备中"
                    };
                    
                    DownloadItems.Add(downloadItem);

                    var progress = new Progress<int>(value =>
                    {
                        downloadItem.Progress = value;
                    });

                    _cancellationTokenSource = new CancellationTokenSource();
                    await _controller.StartDownload(downloadItem, progress, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "下载失败";
                }
                finally
                {
                    StartButton.IsEnabled = true;
                }
            };

            // 暂停按钮
            PauseButton.Click += (s, e) =>
            {
                var selectedItem = DownloadListView.SelectedItem as DownloadItem;
                if (selectedItem != null)
                {
                    if (selectedItem.IsPaused)
                    {
                        _controller.ResumeDownload(selectedItem.FileName);
                        selectedItem.IsPaused = false;
                        selectedItem.Status = "下载中";
                    }
                    else
                    {
                        _controller.PauseDownload(selectedItem.FileName);
                        selectedItem.IsPaused = true;
                        selectedItem.Status = "已暂停";
                    }
                }
            };

            // 停止按钮
            StopButton.Click += (s, e) =>
            {
                var selectedItem = DownloadListView.SelectedItem as DownloadItem;
                if (selectedItem != null)
                {
                    _controller.CancelDownload(selectedItem.FileName);
                    selectedItem.Status = "已取消";
                }
            };

            // 窗口关闭时清理资源
            this.Closed += (s, e) =>
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _controller.Dispose();
            };
        }
    }
} 
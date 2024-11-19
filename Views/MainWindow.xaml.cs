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
        private readonly ObservableCollection<DownloadItem> DownloadItems;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            DownloadItems = new ObservableCollection<DownloadItem>();
            DownloadListView.ItemsSource = DownloadItems;
            InitializeEventHandlers();
        }

        private void InitializeEventHandlers()
        {
            // 手机号码输入框
            PhoneNumberTextBox.TextChanged += (s, e) =>
            {
                if (_viewModel.ValidatePhoneNumber(PhoneNumberTextBox.Text.Trim()))
                {
                    PhoneNumberTextBox.BorderBrush = System.Windows.Media.Brushes.Green;
                    GetCodeButton.IsEnabled = true;
                }
                else
                {
                    PhoneNumberTextBox.BorderBrush = System.Windows.Media.Brushes.Red;
                    GetCodeButton.IsEnabled = false;
                }
            };

            // 验证码输入框
            VerificationCodeTextBox.TextChanged += (s, e) =>
            {
                var code = VerificationCodeTextBox.Text.Trim();
                LoginButton.IsEnabled = !string.IsNullOrWhiteSpace(code) && code.Length >= 5;
            };

            // 频道链接输入框
            ChannelLinkTextBox.TextChanged += (s, e) =>
            {
                UpdateStartButtonState();
            };

            // 保存路径输入框
            SavePathTextBox.TextChanged += (s, e) =>
            {
                UpdateStartButtonState();
            };

            // 获取验证码按钮
            GetCodeButton.Click += async (s, e) =>
            {
                try
                {
                    GetCodeButton.IsEnabled = false;
                    StatusText.Text = "正在发送验证码...";
                    
                    await _viewModel.RequestVerificationCode();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"发送验证码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    
                    await _viewModel.Login();
                    
                    if (_viewModel.IsLoggedIn)
                    {
                        EnableDownloadControls(true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"登录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    LoginButton.IsEnabled = true;
                }
            };

            // 浏览按钮
            BrowseButton.Click += (s, e) =>
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = "选择下载文件保存位置",
                    UseDescriptionForTitle = true,
                    SelectedPath = _viewModel.SavePath
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _viewModel.SavePath = dialog.SelectedPath;
                }
            };

            // 开始下载按钮
            StartButton.Click += async (s, e) =>
            {
                try
                {
                    if (!_viewModel.IsLoggedIn)
                    {
                        MessageBox.Show("请先登录", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    StartButton.IsEnabled = false;
                    PauseButton.IsEnabled = true;
                    StopButton.IsEnabled = true;
                    StatusText.Text = "正在下载...";

                    await _viewModel.StartAutomation();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    StartButton.IsEnabled = true;
                    PauseButton.IsEnabled = false;
                    StopButton.IsEnabled = false;
                }
            };

            // 暂停按钮
            PauseButton.Click += (s, e) =>
            {
                var selectedItem = DownloadListView.SelectedItem as DownloadItem;
                if (selectedItem != null)
                {
                    _viewModel.PauseDownload(selectedItem);
                }
            };

            // 停止按钮
            StopButton.Click += (s, e) =>
            {
                var selectedItem = DownloadListView.SelectedItem as DownloadItem;
                if (selectedItem != null)
                {
                    _viewModel.StopDownload(selectedItem);
                }
            };

            // 窗口关闭时清理资源
            this.Closed += (s, e) =>
            {
                _viewModel.Dispose();
            };
        }

        private void UpdateStartButtonState()
        {
            StartButton.IsEnabled = !string.IsNullOrWhiteSpace(ChannelLinkTextBox.Text) 
                && !string.IsNullOrWhiteSpace(SavePathTextBox.Text)
                && _viewModel.IsLoggedIn;
        }

        private void EnableDownloadControls(bool enable)
        {
            ChannelLinkTextBox.IsEnabled = enable;
            SavePathTextBox.IsEnabled = enable;
            BrowseButton.IsEnabled = enable;
            UpdateStartButtonState();
        }
    }
} 
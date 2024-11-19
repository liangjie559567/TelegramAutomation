using System;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using System.Threading;

namespace TelegramAutomation.Views
{
    public partial class MainWindow : Window
    {
        private readonly AutomationController _controller;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly Progress<string> _progress;

        public MainWindow()
        {
            InitializeComponent();
            _controller = new AutomationController();
            _progress = new Progress<string>(message => StatusText.Text = message);
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
                    PauseButton.IsEnabled = true;
                    StopButton.IsEnabled = true;
                    StatusText.Text = "正在下载...";
                    
                    string channelLink = ChannelLinkTextBox.Text.Trim();
                    string savePath = SavePathTextBox.Text.Trim();
                    
                    if (string.IsNullOrEmpty(channelLink) || string.IsNullOrEmpty(savePath))
                    {
                        MessageBox.Show("请输入频道链接和保存路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _cancellationTokenSource = new CancellationTokenSource();
                    await _controller.StartAutomation(channelLink, savePath, _progress, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "下载失败";
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
                try
                {
                    // TODO: 实现暂停功能
                    PauseButton.Content = PauseButton.Content.ToString() == "暂停" ? "继续" : "暂停";
                    StatusText.Text = PauseButton.Content.ToString() == "暂停" ? "已继续" : "已暂停";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            // 停止按钮
            StopButton.Click += (s, e) =>
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    _controller.Stop();
                    StatusText.Text = "已停止";
                    
                    StartButton.IsEnabled = true;
                    PauseButton.IsEnabled = false;
                    StopButton.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"停止失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
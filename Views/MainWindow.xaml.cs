using System;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace TelegramAutomation.Views
{
    public partial class MainWindow : Window
    {
        private readonly AutomationController _controller;

        public MainWindow()
        {
            InitializeComponent();
            _controller = new AutomationController();
            InitializeEventHandlers();
        }

        private void InitializeEventHandlers()
        {
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

                    // TODO: 调用发送验证码的方法
                    await _controller.InitializeBrowser();
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
                    
                    string code = VerificationCodeTextBox.Text.Trim();
                    if (string.IsNullOrEmpty(code))
                    {
                        MessageBox.Show("请输入验证码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // TODO: 调用登录方法
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
                var dialog = new FolderBrowserDialog();
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

                    // TODO: 调用下载方法
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
                // TODO: 实现暂停功能
                StatusText.Text = "已暂停";
            };

            // 停止按钮
            StopButton.Click += (s, e) =>
            {
                // TODO: 实现停止功能
                StatusText.Text = "已停止";
            };
        }
    }
} 
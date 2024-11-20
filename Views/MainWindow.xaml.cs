using System;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using System.Threading;
using System.Collections.ObjectModel;
using TelegramAutomation.Models;
using TelegramAutomation.ViewModels;
using NLog;

namespace TelegramAutomation.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = (MainViewModel)DataContext;
            
            // 移除直接的控件引用，改用 XAML 绑定
            DataContext = new MainViewModel();
        }

        // 使用命令绑定替代直接的事件处理
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.Initialize();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "窗口加载失败");
                MessageBox.Show("初始化失败: " + ex.Message);
            }
        }
    }
} 
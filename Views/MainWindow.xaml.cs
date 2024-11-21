using System;
using System.Windows;
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
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "窗口加载失败");
                MessageBox.Show(
                    "初始化失败: " + ex.Message,
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
} 